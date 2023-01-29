using Dashboard_Server.Filters;
using Dashboard_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Dashboard_Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthController> logger;
        private readonly IMongoCollection<User> _usersCollection;

        public AuthController(IConfiguration configuration, IOptions<StoreDatabaseSettings> database, ILogger<AuthController> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            var mongoClient = new MongoClient(database.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(database.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<User>("users");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(Credentials credentials)
        {
            try
            {
                logger.LogInformation("(AuthController, Login) Init Sesion");
                var pass = Security.GetSHA256(credentials.Password);

                var user = _usersCollection.Find(x => x.Username == credentials.Username && x.Password == pass).FirstOrDefault();

                if (user == null) return Unauthorized(new ErrorResponse() { Message = "username or password was incorrect.", Status = false });

                var issuer = configuration.GetValue<string>("Jwt:Issuer");
                var audience = configuration.GetValue<string>("Jwt:Audience");
                var key = Encoding.ASCII.GetBytes(configuration.GetValue<string>("Jwt:Key") ?? "");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id?.ToString() ?? ""),
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim(ClaimTypes.Role, user.role ?? ""),
                        new Claim(ClaimTypes.Hash, "yarzad")
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(60),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = new SigningCredentials
                    (new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512Signature)
                };
                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);
                var stringToken = tokenHandler.WriteToken(token);

                return Ok(new
                {
                    Token = stringToken,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogCritical($"(AuthController, Login) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }
    }

    public class Security
    {
        public static string GetSHA256(string str)
        {
            SHA256 sha256 = SHA256Managed.Create();
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] stream = null;
            StringBuilder sb = new StringBuilder();
            stream = sha256.ComputeHash(encoding.GetBytes(str));
            for (int i = 0; i < stream.Length; i++) sb.AppendFormat("{0:x2}", stream[i]);
            return sb.ToString();
        }
    }
}
