using Dashboard_Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Dashboard_Server.Filters
{
    public class TokenAttribute : ActionFilterAttribute, IAsyncActionFilter
    {
        readonly IConfiguration configuration = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
       .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
       .Build();

        readonly IMongoDatabase database;

        public TokenAttribute()
        {
            string connString = configuration.GetValue<string>("StoreDatabase:ConnectionString") ?? "";
            string dbname = configuration.GetValue<string>("StoreDatabase:DatabaseName") ?? "";
            var client = new MongoClient(connString);
            database = client.GetDatabase(dbname);
        }

        public bool LifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters) => (expires.HasValue && DateTime.UtcNow < expires) && (notBefore.HasValue && DateTime.UtcNow > notBefore);


        public ClaimsPrincipal? GetPrincipal(string token)
        {
            try
            {
                string key = configuration.GetValue<string>("Jwt:Key") ?? "";
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken == null) return null;

                var symmetricKey = Encoding.UTF8.GetBytes(key);

                var validationParameters = new TokenValidationParameters()
                {
                    RequireExpirationTime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(symmetricKey),
                    ValidateLifetime = true,
                    LifetimeValidator = this.LifetimeValidator
                };

                SecurityToken securityToken;
                var principal = tokenHandler.ValidateToken(token, validationParameters, out securityToken);

                return principal;
            }
            catch (Exception ex)
            {
                //should write log
                return null;
            }
        }

        public bool ValidateToken(string token)
        {
            var simplePrinciple = GetPrincipal(token);

            if (simplePrinciple == null) return false;

            var identity = new ClaimsIdentity(simplePrinciple.Identity);

            if (identity == null) return false;

            if (!identity.IsAuthenticated) return false;

            if (identity.FindFirst(ClaimTypes.Hash) == null || !identity.FindFirst(ClaimTypes.Hash).Value.Equals("yarzad"))
                return false;

            return true;
        }

        public Dictionary<string, object> GetUser(string token)
        {
            var response = new Dictionary<string, object>();

            var simplePrinciple = GetPrincipal(token);

            if (simplePrinciple == null)
            {
                response.Add("id", "");
                return response;
            }

            var identity = new ClaimsIdentity(simplePrinciple.Identity);

            if (identity == null || !identity.IsAuthenticated)
            {
                response.Add("id", "");
                return response;
            }

            if (identity.FindFirst(ClaimTypes.Role) != null)
            {
                response.Add("id", identity.FindFirst(ClaimTypes.NameIdentifier).Value);
                return response;
            }

            response.Add("id", "");
            return response;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if ((!context.HttpContext.Request.Headers.ContainsKey("xtoken")
                || string.IsNullOrEmpty(context.HttpContext.Request.Headers["xtoken"])
                || !ValidateToken(context.HttpContext.Request.Headers["xtoken"].ToString())))
            {
                context.Result = new UnauthorizedObjectResult(new ErrorResponse
                {
                    Message = "Token validation failed!",
                    Status = false
                });
                return;
            }

            string idUser = GetUser(context.HttpContext.Request.Headers["xtoken"].ToString())["id"].ToString() ?? "";

            if (string.IsNullOrEmpty(idUser))
            {
                context.Result = new UnauthorizedObjectResult(new ErrorResponse
                {
                    Message = "Token is unauthorized!",
                    Status = false
                });
                return;
            }

            var user = database.GetCollection<User>("users").Find(x => x.Id == idUser).FirstOrDefault();

            context.HttpContext.Session.SetString("user:id", idUser);
            context.HttpContext.Session.SetString("user:firstName", user.FirstName ?? "");
            context.HttpContext.Session.SetString("user:lastName", user.LastName ?? "");
            context.HttpContext.Session.SetString("user:lastName2", user.LastName2 ?? "");
            context.HttpContext.Session.SetString("user:role", user.role ?? "");

            await next();
        }
    }
}
