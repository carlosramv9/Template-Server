using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Runtime.Serialization;

namespace Dashboard_Server.Models
{
    [Serializable]
    public class User
    {
        public User()
        {
            CreatedAt = DateTime.UtcNow;
            Status = true;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("first_name")]
        public string? FirstName { get; set; }
        [BsonElement("last_name")]
        public string? LastName { get; set; }
        [BsonElement("last_name2")]
        public string? LastName2 { get; set; }
        [BsonElement("email")]
        public string? Email { get; set; }
        [BsonElement("role")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? role { get; set; }
        [BsonElement("username")]
        public string? Username { get; set; }
        [BsonElement("password")]
        public string? Password { get; set; }
        [BsonElement("image")]
        public string? Avatar { get; set; }
        [BsonElement("status")]
        public bool? Status { get; set; }
        [BsonElement("created_at")]
        public DateTime? CreatedAt { get; set; }

    }
}
