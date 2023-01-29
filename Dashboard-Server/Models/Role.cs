using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Runtime.Serialization;

namespace Dashboard_Server.Models
{
    [Serializable]
    public class Role
    {
        public Role()
        {
            CreatePermissions = new List<string>();
            ReadPermissions = new List<string>();
            UpdatePermissions = new List<string>();
            DeletePermissions = new List<string>();
            CreatedAt = DateTime.Now;
            Status = true;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("name")]
        public string? Name { get; set; }
        [BsonElement("createPermissions")]
        public List<string>? CreatePermissions { get; set; }
        [BsonElement("readPermissions")]
        public List<string>? ReadPermissions { get; set; }
        [BsonElement("updatePermissions")]
        public List<string>? UpdatePermissions { get; set; }
        [BsonElement("deletePermissions")]
        public List<string>? DeletePermissions { get; set; }
        [BsonElement("status")]
        public bool? Status { get; set; }
        [BsonElement("created_by")]
        public string? CreatedBy { get; set; }
        [BsonElement("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
