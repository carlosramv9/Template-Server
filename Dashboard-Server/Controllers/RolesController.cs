using Dashboard_Server.Filters;
using Dashboard_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Dashboard_Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<RolesController> logger;
        private readonly IMongoCollection<Role> rolesCollection;

        public RolesController(IConfiguration configuration, IOptions<StoreDatabaseSettings> database, ILogger<RolesController> logger)
        {
            this.configuration = configuration;
            this.logger = logger;

            var mongoClient = new MongoClient(database.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(database.Value.DatabaseName);

            rolesCollection = mongoDatabase.GetCollection<Role>("roles");
        }

        // GET: api/<RolesController>
        [HttpGet]
        [Token]
        [Permissions(Permission = "readPermissions", Route = "roles")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                logger.LogInformation($"(RolesController, GetAll) Get all roles");

                var roles = rolesCollection.Find(_ => true).ToList();

                return Ok(new DataResponse()
                {
                    Data = roles,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(RolesController, GetAll) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // GET api/<RolesController>/5
        [HttpGet("{id}")]
        [Token]
        [Permissions(Permission = "readPermissions", Route = "roles")]
        public IActionResult Get(string id)
        {
            try
            {
                logger.LogInformation($"(RolesController, GetAll) Get role by id");

                var role = rolesCollection.Find(x => x.Id == id).FirstOrDefault();

                if (role == null) return Unauthorized(new ErrorResponse() { Message = "rolename or password was incorrect.", Status = false });

                return Ok(new DataResponse()
                {
                    Data = role,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(RolesController, Get) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // POST api/<RolesController>
        [HttpPost]
        [Token]
        [Permissions(Permission = "createPermissions", Route = "roles")]
        public IActionResult Post([FromBody] Role value)
        {
            try
            {
                logger.LogInformation($"(RolesController, Post) Add role");

                rolesCollection.InsertOne(value);

                return Ok(new DataResponse()
                {
                    Data = configuration.GetValue<string>("Messages:EntityCreate") ?? "",
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(RolesController, Post) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // PUT api/<RolesController>/5
        [HttpPut("{id}")]
        [Token]
        [Permissions(Permission = "updatePermissions", Route = "roles")]
        public IActionResult Put(string id, [FromBody] Role value)
        {
            try
            {
                logger.LogInformation($"(RolesController, Put) Update role by id");

                var role = rolesCollection.Find(x => x.Id == id).FirstOrDefault();

                if (role == null) return Unauthorized(new ErrorResponse() { Message = "rolename or password was incorrect.", Status = false });

                role.Name = string.IsNullOrEmpty(value.Name) ? role.Name : value.Name;

                rolesCollection.ReplaceOne(x => x.Id == id, role, new ReplaceOptions { IsUpsert = true });

                return Ok(new DataResponse()
                {
                    Data = configuration.GetValue<string>("Messages:EntityUpdate") ?? "",
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(RolesController, Put) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }

        // DELETE api/<RolesController>/5
        [HttpDelete("{id}")]
        [Token]
        [Permissions(Permission = "deletePermissions", Route = "roles")]
        public IActionResult Delete(string id)
        {
            try
            {
                logger.LogInformation($"(RolesController, Delete) Delete role by id");

                var role = rolesCollection.Find(x => x.Id == id && x.Status == true).FirstOrDefault();

                if (role == null) return Unauthorized(new ErrorResponse() { Message = "rolename or password was incorrect.", Status = false });

                UpdateDefinition<Role> update = Builders<Role>.Update.Set("status", false);

                rolesCollection.FindOneAndUpdate(x => x.Id == id, update);

                return Ok(new DataResponse()
                {
                    Data = configuration.GetValue<string>("Messages:EntityDelete") ?? "",
                    Status = true
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"(RolesController, Delete) {ex.Message}");
                return BadRequest(new ErrorResponse()
                {
                    Message = configuration.GetValue<string>("Messages:DataBase") ?? "",
                    Status = false
                });
            }
        }
    }
}
