using Dashboard_Server.Filters;
using Dashboard_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Dashboard_Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<UsersController> logger;
        private readonly IMongoCollection<User> usersCollection;
        private readonly IMongoDatabase db;

        public UsersController(IConfiguration configuration, IOptions<StoreDatabaseSettings> database, ILogger<UsersController> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            var client = new MongoClient(database.Value.ConnectionString);
            db = client.GetDatabase(database.Value.DatabaseName);

            usersCollection = db.GetCollection<User>("users");
        }

        // GET: api/<UsersController>
        [HttpGet]
        [Token]
        [Permissions(Permission = "readPermissions", Route = "users")]
        public IActionResult GetAll()
        {
            try
            {
                logger.LogInformation($"(UsersController, GetAll) Get all users");

                var users = usersCollection.Find(_ => true).ToList();

                return Ok(new DataResponse()
                {
                    Data = users,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(UsersController, GetAll) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        [Token]
        [Permissions(Permission = "readPermissions", Route = "users")]
        public IActionResult Get(string id)
        {
            try
            {
                logger.LogInformation($"(UsersController, GetAll) Get user by id");

                var roles = db.GetCollection<Role>("roles");

                var user = usersCollection.AsQueryable()
                    .Where(x => x.Id== id)
                    .Join(roles.AsQueryable(), user => user.role, role => role.Id,  (x, y) => new { oUser = x, oRole = y })
                    .Select((x) => new
                    {
                        x.oUser.Id,
                        x.oUser.FirstName,
                        x.oUser.LastName,
                        x.oUser.LastName2,
                        x.oUser.Email,
                        x.oUser.Avatar,
                        x.oUser.CreatedAt,
                        x.oUser.Status,
                        x.oUser.Username,
                        Role = x.oRole
                    })
                    .FirstOrDefault();

                if (user == null) return Unauthorized(new ErrorResponse() { Message = "username or password was incorrect.", Status = false });

                return Ok(new DataResponse()
                {
                    Data = user,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(UsersController, Get) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // POST api/<UsersController>
        [HttpPost]
        [Token]
        [Permissions(Permission = "createPermissions", Route = "users")]
        public IActionResult Post([FromBody] User value)
        {
            try
            {
                logger.LogInformation($"(UsersController, Post) Add user");

                value.Password = Security.GetSHA256(value.Password ?? "");

                usersCollection.InsertOne(value);

                return Ok(new DataResponse()
                {
                    Data = configuration.GetValue<string>("Messages:EntityCreate") ?? "",
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(UsersController, Post) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        [Token]
        [Permissions(Permission = "updatePermissions", Route = "users")]
        public IActionResult Put(string id, [FromBody] User value)
        {
            try
            {
                logger.LogInformation($"(UsersController, Put) Update user by id");

                var user = usersCollection.Find(x => x.Id == id).FirstOrDefault();

                if (user == null) return Unauthorized(new ErrorResponse() { Message = "username or password was incorrect.", Status = false });

                user.FirstName = string.IsNullOrEmpty(value.FirstName) ? user.FirstName : value.FirstName;
                user.LastName = string.IsNullOrEmpty(value.LastName) ? user.LastName : value.LastName;
                user.LastName2 = string.IsNullOrEmpty(value.LastName2) ? user.LastName2 : value.LastName2;
                user.Email = string.IsNullOrEmpty(value.Email) ? user.Email : value.Email;
                user.Username = string.IsNullOrEmpty(value.Username) ? user.Username : value.Username;

                usersCollection.ReplaceOne(x => x.Id == id, user, new ReplaceOptions { IsUpsert = true });

                return Ok(new DataResponse()
                {
                    Data = configuration.GetValue<string>("Messages:EntityUpdate") ?? "",
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(UsersController, Put) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // PUT api/<UsersController>/5
        [HttpPut("{id}/changePassword")]
        [Token]
        [Permissions(Permission = "updatePermissions", Route = "users")]
        public IActionResult ChangePassword(string id, [FromBody] Credentials password)
        {
            try
            {
                logger.LogInformation($"(UsersController, ChangePassword) Update user password by id");

                var user = usersCollection.Find(x => x.Id == id && x.Status == true).FirstOrDefault();

                if (user == null) return Unauthorized(new ErrorResponse() { Message = "username or password was incorrect.", Status = false });

                var pass = Security.GetSHA256(password.Password ?? "");

                UpdateDefinition<User> update = Builders<User>.Update.Set("password", pass);

                usersCollection.FindOneAndUpdate(x => x.Id == id, update);

                return Ok(new DataResponse()
                {
                    Data = configuration.GetValue<string>("Messages:EntityUpdate") ?? "",
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(UsersController, ChangePassword) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        [Token]
        [Permissions(Permission = "deletePermissions", Route = "users")]
        public IActionResult Delete(string id)
        {
            try
            {
                logger.LogInformation($"(UsersController, Delete) Delete user by id");

                var user = usersCollection.Find(x => x.Id == id && x.Status == true).FirstOrDefault();

                if (user == null) return Unauthorized(new ErrorResponse() { Message = "username or password was incorrect.", Status = false });

                UpdateDefinition<User> update = Builders<User>.Update.Set("status", false);

                usersCollection.FindOneAndUpdate(x => x.Id == id, update);

                return Ok(new DataResponse()
                {
                    Data = configuration.GetValue<string>("Messages:EntityDelete") ?? "",
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(UsersController, Delete) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }
    }
}
