using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SolidarityBookCatalog.Services
{
    public class UserService
    {
        public readonly IMongoCollection<User> _users;

        public UserService(IConfiguration config)
        {
            var client = new MongoClient(config.GetConnectionString("BookDb"));
            var database = client.GetDatabase("BookReShare");
            _users = database.GetCollection<User>("user");
        }
        public bool insert(User user) { 
            bool flag=false;
            string publicKey;
            string privateKey;
            using (var rsa = new RSACryptoServiceProvider(2048)) // 2048位密钥长度
            {
                publicKey = rsa.ToXmlString(false); // 公钥
                privateKey = rsa.ToXmlString(true); // 私钥
            }
            user = new User
            {
                Username = "yangtzeu",
                Password = "thisistest",
                Province = "湖北",
                City = "荆州",
                AppKey = "appKey",
                AppId = "appId",
                Name = "长江大学图书馆",
                Chmod = "777",
                PublicKey = publicKey,
                PrivateKey = privateKey,
            };
            _users.InsertOne(user);

            return flag;
        }
        //openapi调用时实现签名验证
        public Msg Sign(string isbn,User user)
        {
            Msg msg = new Msg();
            //验证签名
            using (var md5 = MD5.Create())
            {
                var data = Encoding.UTF8.GetBytes($"{user.AppId}{user.Nonce}{isbn}");
                var hash = md5.ComputeHash(data);
                string signStr= BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr + user.AppKey));
                signStr = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(); ;
                if (signStr == user.Sign)
                { 
                   msg.Code=0;
                }
            }
            //验证权限

            return msg;
        }
    }
}
