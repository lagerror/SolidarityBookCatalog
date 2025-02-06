using Elastic.Clients.Elasticsearch;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SolidarityBookCatalog.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("username")]
        public string? Username { get; set; }

        [BsonElement("password")]
        public string? Password { get; set; }

        [BsonElement("province")]
        public string? Province { get; set; }

        [BsonElement("city")]
        public string? City { get; set; }

        [BsonElement("appKey")]
        public string? AppKey { get; set; }

        [BsonElement("appId")]
        public string? AppId { get; set; }

        [BsonElement("name")]  //用户昵称
        public string? Name { get; set; }
        
        //权限控制列表 仿LINUX chmod 777 模式，自己用户下数据为delete,insert,update,同一个市的，同一个省的，select默认全部具有
        [BsonElement("chmod")]
        public string? Chmod { get; set; }
        
        //临时用权限签名
        [BsonElement("sign")]
        public string? Sign { get; set; }
        //临时用权限签名
        [BsonElement("nonce")]
        public string? Nonce { get; set; }

        //关于密钥的存储只是用于测试，后继可以采用国密和webase的模式
        //rsa2048公钥
        [BsonElement("publicKey")]
        public string? PublicKey { get; set; }
        //rsa2048私钥
        [BsonElement("privateKey")]
        public string? PrivateKey { get; set; }

        //rsa2048公钥
        [BsonElement("publicPem")]
        public string? PublicPem { get; set; }
        //rsa2048私钥
        [BsonElement("privatePem")]
        public string? PrivatePem { get; set; }
        //手机号，便于联系
        [BsonElement("mobilePhone")]
        public string? MobilePhone { get; set; }

    }
    //用户登录model
    public class UserDTOLogin { 
        public string username {  get; set; } 
        public string password { get; set; }    
    }
}

