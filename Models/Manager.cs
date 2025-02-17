using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text;
using System.Security.Cryptography;

namespace SolidarityBookCatalog.Models
{
    public class Manager
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userName")]
        public string UserName { get; set; }

        [BsonElement("openId")]
        public string OpenId { get; set; }

        [BsonElement("password")]
        private string Password { get; set; }

        [BsonElement("organizationId")] // 关联user表，为外键 
        [BsonRepresentation(BsonType.ObjectId)]
        public string OrganizationId { get; set; }

        [BsonElement("role")]
        public string Role { get; set; } = "manager";

        [BsonElement("isEnabled")]
        public bool IsEnabled { get; set; }

        [BsonElement("auditor")] // 审核人

        public string Auditor { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("auditordAt")]
        public DateTime? ApprovedAt { get; set; }

        // 密码加密方法 
        public void SetPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            Password = Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Password == Convert.ToBase64String(hashedBytes);
        }
    }

}

