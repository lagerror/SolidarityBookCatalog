using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using System.Text.RegularExpressions;

namespace SolidarityBookCatalog.Services
{
    public class HoldingService
    {
        public readonly IMongoCollection<Holding> _holdings;
        public readonly IMongoCollection<Biblios> _biblios;
        public readonly IMongoCollection<User> _user;
        public HoldingService(IConfiguration config, IMongoClient client)
        {
          
            var database = client.GetDatabase("BookReShare");
            _holdings = database.GetCollection<Holding>("holding");
            _biblios = database.GetCollection<Biblios>("biblios");
            _user=database.GetCollection<User>("user");
        }
    
        public Holding Get(string identifier)
        {
            return _holdings.Find<Holding>(hoding => hoding.Identifier == identifier).FirstOrDefault();
        }

        public async Task<Msg> searchAsync(SearchQueryList list, int rows = 10, int page = 1)
        {
            Msg msg = new Msg();
            // 构建过滤条件
            var filterBuilder = Builders<Holding>.Filter;
            var filters = new List<FilterDefinition<Holding>>();
            foreach (var item in list.List)
            {
                if (item.Field == "identifier")
                {
                    filters.Add(filterBuilder.Eq(x=>x.Identifier,item.Keyword));
                }
                else if (item.Field == "userName")
                {
                    var regexPattern = $"^{item.Keyword.Replace(".", "\\.")}.*";
                    filters.Add(filterBuilder.Regex(d => d.UserName, new Regex(regexPattern)));
                }
           }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : FilterDefinition<Holding>.Empty;

            // 执行查询
            var total = await _holdings.CountDocumentsAsync(finalFilter);
            var results = await _holdings.Find(finalFilter)
                .Skip((page - 1) * rows)
                .Limit(rows)
                .ToListAsync();

            msg.Code = 0;
            msg.Message = "查询成功";
            msg.Data = new
            {
                total = total,
                rows = results
            };

            return msg;
        }

        public Msg SearchVant(List<Holding> holdings)
        {
            Msg msg = new Msg();
            try {
                List<SolidarityBookCatalog.Models.User> userList = _user.Find(_ => true).ToList();
                // LINQ查询
                var query = from holding in holdings
                            join user in userList on holding.UserName equals user.AppId
                            select new
                            {
                                Province = user.Province,
                                City = user.City,
                                Name = user.Name,
                                Id = holding.Id,
                                Num = holding.Barcode?.Count
                            };
                var result = query
                 .GroupBy(l => l.Province)
                     .Select(group => new
                     {
                         text = group.Key,
                         value = group.First().Id,
                         children = group.GroupBy(l => l.City)
                             .Select(children => new
                             {
                                 text = children.Key ,
                                 value = children.First().Id,
                                 children = children.Select(l => new
                                 {
                                     text = $"[{l.Num}册]{l.Name}",
                                     value = l.Id
                                 })
                             })
                     });
                //获取图书信息
                var identifier = holdings[0].Identifier;
                var biblios=_biblios.Find(x=>x.Identifier == identifier).FirstOrDefault();
                msg.Code = 0;
                msg.Message = "SearchVant";
                msg.Data =new { 
                    biblios=biblios,
                    holding=result
                };
            }catch(Exception ex) {
                msg.Code = 100;
                msg.Message= "SearchVant";
                msg.Data = ex.Message;
            }
            return  msg;
        }

        public   List<Holding> search(string identifier, string prefix="all")
        { 
            List<Holding> list = new List<Holding>();
            //不限定所在图书馆
            FilterDefinition<Holding> filter = null;
            if (prefix=="all")
            {
                filter = Builders<Holding>.Filter.Eq(u => u.Identifier, identifier);
              
            }
            else    //指定图书馆
            {
                // 创建正则表达式过滤器
                var regexPattern = $"^{prefix.Replace(".", "\\.")}.*";
                filter = Builders<Holding>.Filter.And(
                    Builders<Holding>.Filter.Eq(u => u.Identifier, identifier),
                    Builders<Holding>.Filter.Regex(u => u.UserName, new Regex(regexPattern))
                );
            }
            // 执行查询
            list = _holdings.Find(filter).ToList();
            //为了在移动端前端做pickup选择，后端生成所需数据
           

            return list;
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
