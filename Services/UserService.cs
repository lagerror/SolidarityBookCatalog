﻿using MongoDB.Bson;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SolidarityBookCatalog.Services
{
    public class UserService
    {
        public readonly IMongoCollection<User> _users;
        public readonly IMongoCollection<Biblios> _books;
        public readonly IMongoCollection<Holding> _holdings;
        public readonly IMongoCollection<Reader> _readers;

        public UserService(IConfiguration config, IMongoClient client)
        {
            var database = client.GetDatabase("BookReShare");
            _users = database.GetCollection<User>("user");
            _books = database.GetCollection<Biblios>("biblios");
            _holdings= database.GetCollection<Holding>("holding");
            _readers = database.GetCollection<Reader>("reader");
        }
        public bool insert(User user) { 
            bool flag=false;
            string publicKey;
            string privateKey;
            using (var rsa = new RSACryptoServiceProvider(2048)) // 2048位密钥长度
            {
                // 导出公钥
                var publicPem = rsa.ExportSubjectPublicKeyInfoPem(); // PEM 格式的公钥
                var privatePem = rsa.ExportPkcs8PrivateKeyPem(); // PEM 格式的私钥

                publicKey = rsa.ToXmlString(false); // 公钥
                privateKey = rsa.ToXmlString(true); // 私钥
            }
            user = new User
            {
                Username = "jwtToken",
                Password = "thisistest",
                Province = "湖北",
                City = "荆州",
                AppKey = "7a8F6lTaUmz9wX5w",
                AppId = "hubei.jingzhou.yangtzeu.jwtToken",
                Name = "第三方TOKEN管理专用账号",
                Chmod = "FFFF",
                PublicKey = publicKey,
                PrivateKey = privateKey,
            };



            _users.InsertOne(user);
           
            return flag;
        }

        public Msg GroupLibAdd(string flag)
        { 
            Msg msg = new Msg();
            // 创建投影，只选择特定的字段
            var projection = Builders<User>.Projection
                                                     .Include(u => u.Province)
                                                     .Include(u => u.City)
                                                     .Include(u => u.AppId)
                                                     .Include(u => u.Name);
            List<User> users;
            if (flag == "vant")
            {
                var filter = Builders<User>.Filter.Eq(x => x.IsInterLend, true);
                users = _users.Find(filter).Project<User>(projection).ToList();

            }
            else
            {
                // 执行查询并获取结果
                users = _users.Find(new BsonDocument()).Project<User>(projection).ToList();
            }
            var groupedLibraries = users
           .GroupBy(l => l.Province)
               .Select(group => new
               {
                   ProvinceName = group.Key,
                   ProviceId = group.First().AppId.Substring(0, group.First().AppId.IndexOf('.')),
                   Cities = group.GroupBy(l => l.City)
                       .Select(cityGroup => new
                       {
                           CityName = cityGroup.Key,
                           CityId = cityGroup.First().AppId.Substring(0, cityGroup.First().AppId.IndexOf('.', cityGroup.First().AppId.IndexOf('.') + 1)),
                           Libraries = cityGroup.Select(l => new
                           {
                               LibraryName = l.Name,
                               LibraryId = l.AppId
                           })
                       })
               });
          
            
            // 将分组后的结果转换为JSON字符串
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All)
                //WriteIndented = true // 设置为true以美化输出
            };
            string json = JsonSerializer.Serialize(groupedLibraries,options);

            msg.Code = 0;
            msg.Data = json;
            return msg;
        }

        //获取tokenPrivateKey
        public  string getTokenPrivateKey(string type)
        {
            string privateKey;
            if (type == "pem")
            {
                privateKey = _users.Find(x => x.Username == "jwtToken").FirstOrDefault().PrivatePem;
            }
            else
            {
                privateKey = _users.Find(x => x.Username == "jwtToken").FirstOrDefault().PrivateKey;
            }
            return privateKey;
        }


        /// <summary>
        /// openapi调用时实现签名验证
        /// </summary>
        /// <param name="isbn">图书的ISBN</param>
        /// <param name="user">用户的appId,Nonce,Sign</param>
        /// <returns></returns>
        public Msg Sign(string isbn, User user)
        {
            Msg msg = new Msg();
            //模型校验
            if (user.AppId == null || user.Nonce == null || user.Sign==null) 
            {
                msg.Code = 1;
                msg.Message = "模型校验不对";
                return msg;
            }
            //验证签名
            using (var md5 = MD5.Create())
            {
                //数据库后台取出对应的appKey
                var appKey=_users.Find(x=>x.AppId == user.AppId).FirstOrDefault().AppKey;
                
                var data = Encoding.UTF8.GetBytes($"{user.AppId}{user.Nonce}{isbn}");
                var hash = md5.ComputeHash(data);
                string signStr= BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr + appKey));
                signStr = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(); 
                if (signStr == user.Sign)
                { 
                   msg.Code=0;
                }
            }
           
            return msg;
        }

        /// <summary>
        /// openapi调用时候chmod校验,16位bit控制，分别为保留，省，市，创建者用户名（appId）,算法需要优化
        /// </summary>
        /// <param name="isbn">图书</param>
        /// <param name="appId">用户</param>
        /// <param name="actionType">操作类型</param>
        /// <returns></returns>
        public Msg chmod(string? isbn, string appId, string tableName, PublicEnum.ActionType actionType) {
            Msg msg = new Msg();

            //通过appId查到要操作的用户
            User user = _users.Find<User>(x => x.AppId == appId).FirstOrDefault();
            if (user == null)
            {
                msg.Code = 1;
                msg.Message = "没找到用户";
            }
            //把chmod十六进制转换为二进制
            UInt16 right = Convert.ToUInt16(user.Chmod, 16);

            //如果是插入，提前处理，因为不用检查修改和删除对象
            if (actionType == PublicEnum.ActionType.insert)
            {
                if ((right & (UInt16)PublicEnum.RightMask.insert) == (UInt16)PublicEnum.RightMask.insert)
                {
                    msg.Code = 0;
                    return msg;
                }
            }
            User creator = new User();
            switch (tableName)
            {
                case "biblios":
                //通过isbn查找图书
                    Biblios book = _books.Find<Biblios>(x => x.Identifier == isbn).FirstOrDefault();
                    if (book == null)
                    {
                        msg.Code = 2;
                        msg.Message = "没找到书目";
                    }
                    //通过图书的创建者查找记录所有者的省，市，和用户名
                    creator = _users.Find<User>(x => x.AppId == book.UserName).FirstOrDefault();
                    break;
                case "holding":
                    Holding holding = _holdings.Find<Holding>(x => x.Identifier == isbn & x.UserName == appId).FirstOrDefault();
                    if (holding == null)
                    {
                        msg.Code = 2;
                        msg.Message = "没找到馆藏";
                    }
                    //通过图书的创建者查找记录所有者的省，市，和用户名
                    creator = _users.Find<User>(x => x.AppId == holding.UserName).FirstOrDefault();
                    break;
        }
            //如果是自己的记录
            if (user.AppId == creator.AppId)
            {
                switch (actionType)
                {
                    case PublicEnum.ActionType.delete:
                        if ((right & (UInt16)PublicEnum.RightMask.delete) == (UInt16)PublicEnum.RightMask.delete)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.insert:
                        if ((right & (UInt16)PublicEnum.RightMask.insert) == (UInt16)PublicEnum.RightMask.insert)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.update:
                        if ((right & (UInt16)PublicEnum.RightMask.update) == (UInt16)PublicEnum.RightMask.update)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.select:
                        if ((right & (UInt16)PublicEnum.RightMask.select) == (UInt16)PublicEnum.RightMask.select)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                }
            }
            else if (user.City == creator.City)  //拥有市级权限
            {
                switch (actionType)
                {

                    case PublicEnum.ActionType.delete:
                        if ((right >> 4 & (UInt16)PublicEnum.RightMask.delete) == (UInt16)PublicEnum.RightMask.delete)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.insert:
                        if ((right >> 4 & (UInt16)PublicEnum.RightMask.insert) == (UInt16)PublicEnum.RightMask.insert)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.update:
                        if ((right >> 4 & (UInt16)PublicEnum.RightMask.update) == (UInt16)PublicEnum.RightMask.update)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.select:
                        if ((right >> 4 & (UInt16)PublicEnum.RightMask.select) == (UInt16)PublicEnum.RightMask.select)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                }
            }
            else if (user.Province == creator.Province)  //拥有省级权限
            {
                switch (actionType)
                {

                    case PublicEnum.ActionType.delete:
                        if ((right >> 8 & (UInt16)PublicEnum.RightMask.delete) == (UInt16)PublicEnum.RightMask.delete)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.insert:
                        if ((right >> 8 & (UInt16)PublicEnum.RightMask.insert) == (UInt16)PublicEnum.RightMask.insert)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.update:
                        if ((right >> 8 & (UInt16)PublicEnum.RightMask.update) == (UInt16)PublicEnum.RightMask.update)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                    case PublicEnum.ActionType.select:
                        if ((right >> 8 & (UInt16)PublicEnum.RightMask.select) == (UInt16)PublicEnum.RightMask.select)
                        {
                            msg.Code = 0;
                            return msg;
                        }
                        break;
                }
            }
            
           
            return msg;
        }
    }
}
