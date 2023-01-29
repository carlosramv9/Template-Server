using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Dashboard_Server.Models
{
    public class Branch
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("name")]
        public string? name { get; set; }
        [BsonElement("key")]
        public string? Key { get; set; }
        [BsonElement("status")]
        public bool? Status { get; set; } = true;
        [BsonElement("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
