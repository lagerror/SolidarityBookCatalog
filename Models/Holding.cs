using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SolidarityBookCatalog.Models
{
    public class Holding
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("identifier")]
        public string Identifier { get; set; }
        [BsonElement("bookrecno")]
        public string? BookRecNo{ get; set; }
        [BsonElement("UserName")]
        public string? UserName { get; set; }
        [BsonElement("barcode")]
        public List<string>? Barcode { get; set; }
    }

    
}
