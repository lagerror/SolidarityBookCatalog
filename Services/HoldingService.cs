using MongoDB.Driver;
using SolidarityBookCatalog.Models;

namespace SolidarityBookCatalog.Services
{
    public class HoldingService
    {
        public readonly IMongoCollection<Holding> _holdings;

        public HoldingService(IConfiguration config)
        {
            var client = new MongoClient(config.GetConnectionString("BookDb"));
            var database = client.GetDatabase("BookReShare");
            _holdings = database.GetCollection<Holding>("holding");
        }
        public Holding Get(string identifier)
        {
            return _holdings.Find<Holding>(hoding => hoding.Identifier == identifier).FirstOrDefault();
        }

        public Msg insert(Holding holding)
        {
            Msg msg = new Msg();
            try
            {
                _holdings.InsertOne(holding);
                msg.Code = 0;
            }
            catch (Exception ex) {
                msg.Code = 100;
                msg.Message = ex.Message;   
            }
            return msg;
        }

        public bool RepeatKeyHolding(string identifier,string username,string bookRecNo)
        {
            bool flag = false;
            var filter = Builders<Holding>.Filter.Eq(b => b.Identifier, identifier)
                       & Builders<Holding>.Filter.Eq(b => b.UserName, username)
                       & Builders<Holding>.Filter.Eq(b=>b.BookRecNo,bookRecNo);
            if (_holdings.Find(filter).FirstOrDefault() != null)
            {
                flag = true;
            }
            return flag;
        }


        public Msg Update(string identifier, Holding updatedHolding)
        {
            //不使用replaceOne整体替换，据说是updateOne高效一些
            Msg msg = new Msg();

            var updateDefinitionBuilder = Builders<Holding>.Update;
            var updates = new List<UpdateDefinition<Holding>>();
            try
            {
                // 根据字段是否为 null 构建更新定义，应该使用反射的循环
                var properties = updatedHolding.GetType().GetProperties();
                foreach (var property in properties)
                {
                    //这三个字段不能更改
                    if (property.Name == "Identifier" || property.Name == "Id" || property.Name == "UserName")
                    {
                        continue;
                    }
                    //更新非空字段
                    var value = property.GetValue(updatedHolding, null);
                    if (property.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)value))
                    {
                        updates.Add(updateDefinitionBuilder.Set(property.Name, value));
                    }
                    else if (property.PropertyType != typeof(string) && value != null)
                    {
                        updates.Add(updateDefinitionBuilder.Set(property.Name, value));
                    }
                }
                // 将所有更新定义组合成一个更新定义
                var combinedUpdateDefinition = updateDefinitionBuilder.Combine(updates);

                // 如果没有需要更新的字段，直接返回
                if (updates.Count > 0)
                {
                    var updateDefinition = updateDefinitionBuilder.Combine(updates);

                    // 使用唯一索引 identifier 作为过滤条件
                    var filter = Builders<Holding>.Filter.Eq(b => b.Identifier, identifier) 
                        & Builders<Holding>.Filter.Eq(b => b.UserName,updatedHolding.UserName);
                    var result = _holdings.UpdateOne(filter, updateDefinition);

                    if (result.ModifiedCount == 1)
                    {
                        msg.Code = 0;
                        msg.Message = $"update:成功更新{identifier}";
                    }
                    else
                    {
                        msg.Code = 1;
                        msg.Message = $"update:更新失败 {result.ModifiedCount.ToString()}";
                    }
                }
                else
                {
                    msg.Code = 2;
                    msg.Message = $"update: 没有对应更新字段";
                }
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = ex.Message;
            }

            return msg;
        }

        public Msg Delete(string identifier)
        {
            Msg msg = new Msg();
            try
            {
                _holdings.DeleteOne(holding => holding.Identifier == identifier);
                msg.Code = 0;
            }
            catch (Exception ex)
            {
                msg.Message = ex.Message;
                msg.Code = 100;
            }
            return msg;
        }
    }
}
