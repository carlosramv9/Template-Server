using Dashboard_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dashboard_Server.Filters
{
    public class PermissionsAttribute : ActionFilterAttribute, IAsyncActionFilter
    {
        readonly IConfiguration configuration = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
          .Build();

        readonly IMongoDatabase database;
        readonly string ADMIN_ROLE = "ADMIN_ROLE";

        public PermissionsAttribute()
        {
            string connString = configuration.GetValue<string>("StoreDatabase:ConnectionString") ?? "";
            string dbname = configuration.GetValue<string>("StoreDatabase:DatabaseName") ?? "";
            var client = new MongoClient(connString);
            database = client.GetDatabase(dbname);
        }

        public string Permission { get; set; }
        public string Route { get; set; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var idUser = context.HttpContext.Session.GetString("user:id");
            var error = new BadRequestObjectResult(new ErrorResponse
            {
                Message = configuration.GetValue<string>("Messages:NotAuth") ?? "",
                Status = false
            });

            if (string.IsNullOrEmpty(idUser))
            {
                context.Result = error;
                return;
            }

            var user = database.GetCollection<User>("users").Find(x => x.Id == idUser).FirstOrDefault();
            var role = database.GetCollection<Role>("roles").Find(y => y.Id == user.role).FirstOrDefault().ToBsonDocument();

            if (role == null || !role[Permission].IsBsonArray || role[Permission].AsBsonArray.Count <= 0)
            {
                context.Result = error;
                return;
            }

            if(!role[Permission].AsBsonArray.Values.Contains(Route) && role["name"] != ADMIN_ROLE)
            {
                context.Result = new UnauthorizedObjectResult(new ErrorResponse
                {
                    Message = configuration.GetValue<string>("Messages:BadCredentials") ?? "",
                    Status = false
                });
                return;
            }

            await next();
        }
    }
}
