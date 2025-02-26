﻿using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using System.Text.RegularExpressions;
using SolidarityBookCatalog.Models;
using System.Collections;
using System.Linq;
using DnsClient.Protocol;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Bson;

namespace SolidarityBookCatalog.Services
{
    public class BibliosService
    {
        public readonly IMongoCollection<Biblios> _books;
        private readonly IHttpClientFactory httpClientFactory;
        public readonly string _url;

        public BibliosService(IConfiguration config, IHttpClientFactory httpClientFactory, IMongoClient client)
        {
            var database = client.GetDatabase("BookReShare");
            _books = database.GetCollection<Biblios>("biblios");
            _url = config["91Marc:url"].ToString();
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<Msg> SearchAsync(SearchQueryList list,int rows=10,int page=1)
        {
            Msg msg=new Msg();
            // 构建过滤条件
            var filterBuilder = Builders<Biblios>.Filter;
            var filters = new List<FilterDefinition<Biblios>>();
            foreach(var item in list.List)
            {
                if (item.Field == "title")
                {
                    filters.Add(filterBuilder.Regex(d => d.Title, new BsonRegularExpression(item.Keyword, "i")));
                }
                else if (item.Field == "creator")
                {
                    filters.Add(filterBuilder.Regex(d => d.Creator, new BsonRegularExpression(item.Keyword, "i")));
                }
                else if (item.Field == "publisher")
                {
                    filters.Add(filterBuilder.Regex(d => d.Publisher, new BsonRegularExpression(item.Keyword, "i")));
                }else if(item.Field == "subject")
                {
                    filters.Add(filterBuilder.Regex(d => d.Subject, new BsonRegularExpression(item.Keyword, "i")));
                }
                else if (item.Field == "identifier")
                {
                    filters.Add(filterBuilder.Regex(d => d.Identifier, new BsonRegularExpression(item.Keyword, "i")));
                }   
            }
                       
            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : FilterDefinition<Biblios>.Empty;

            // 执行查询
            var total = await _books.CountDocumentsAsync(finalFilter);
            var results = await _books.Find(finalFilter)
                .Skip((page - 1) * rows)
                .Limit(rows)
                .ToListAsync();
            
            msg.Code = 0;
            msg.Message = "查询成功";
            msg.Data = new { 
                total=total, 
                rows=results 
            };

            return msg;
        }

        public Biblios Get(string identifier)
        {
            return _books.Find<Biblios>(book => book.Identifier == identifier).FirstOrDefault();
        }
        public async Task<Msg> GetFrom91Async(string identifier) {
            Biblios biblios = null;
            bool flag=false;
            Msg msg = new Msg();
            //检查ISBN是否合规
            var tuple= Biblios.validIsbn(identifier);
            if (tuple.Item1)
            {
                identifier = tuple.Item2;
            }
            else {
                msg.Code = 10;
                msg.Message = $"{identifier}校验不对";
                return msg;
            }
            //检查是否存在书目
            biblios= _books.Find<Biblios>(book => book.Identifier == identifier).FirstOrDefault();
            if (biblios != null) {
                msg.Code = 0;
                msg.Message = $"{identifier}已经存在";
                msg.Data = biblios;
                return msg;
            }
            //如果不存在，则下载并插入BIBLIOS
            try
            {
                string result = null;
                //获取MARC
                using (var client = httpClientFactory.CreateClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, _url + identifier);
                    var response = await client.SendAsync(request);
                    var httpContent = response.Content;
                    result= await httpContent.ReadAsStringAsync();
                }
                //解析数据并赋值书目数据
                Dictionary<string,string> ht= ExtractMarcFields(result);
                //填入book字段
                biblios=new Biblios();

                //价格校验
                string priceStr= MarcSubField(ht, "010", "d");
                var tuple3 = Biblios.validPrice(priceStr);
                if (tuple.Item1)
                {
                    biblios.Price = tuple3.Item2;
                }
                else {
                    msg.Code = 1;
                    msg.Message = "价格不对";
                    return msg;
                }
                //出版年
                biblios.Date= MarcSubField(ht, "210", "d");
                var tuple1=Biblios.validYear(biblios.Date);
                if (tuple1.Item1) 
                { 
                    biblios.Date = tuple1.Item2;
                }
                else
                { 
                    msg.Code = 2;
                    msg.Message = "出版年不对";
                    return msg;
                }
                //isbn校验
                biblios.Identifier = MarcSubField(ht, "010", "a");
                var tuple2 = Biblios.validIsbn(biblios.Identifier);
                if (tuple2.Item1) { 
                    biblios.Identifier= tuple2.Item2;
                
                } else {
                    msg.Code= 3;
                    msg.Message = "isbn不对";
                    return msg;
                }
               
                

                //其它字段
                biblios.Title = MarcSubField(ht, "200", "a");
                biblios.Creator = MarcSubField(ht, "200", "f");
                biblios.Relation = MarcSubField(ht, "690", "a");
                biblios.Coverage = MarcSubField(ht, "215", "a");
                biblios.Description= MarcSubField(ht, "330", "a");
                biblios.Subject= MarcSubField(ht, "606", "a");
                biblios.Publisher = MarcSubField(ht, "210", "c");
                msg.Code = 0;
                msg.Message = "成功解析MARC";
                msg.Data = biblios;
                //插入数据库
                biblios.UserName = "hubei.jingzhou.yangtzeu.library";
                _books.InsertOne(biblios);

                return msg;
            }
            catch (Exception ex) { 
                msg.Code = 100;
                msg.Message = ex.Message;
            }
           
            return msg;
        }

        public static string MarcSubField(Dictionary<string,string> ht, string field, string subChar)
        {

            //606主题词字段，有重复单独处理
            string retStr = null;
            if (field == "606")
            {
                if (ht[field] != null)
                {
                    string pattern1 = @"\|\w{1}(.*?)(?=\||$)";
                    Regex regex=new Regex(pattern1);
                    MatchCollection matches = regex.Matches(ht[field]);
                    HashSet<string> keys = new HashSet<string>();
                    foreach (Match match in matches)
                    {
                        keys.Add(match.Groups[1].Value);
                    }
                    retStr=string.Join(",",keys);
                }
                return retStr;
            }




            string pattern = @"\|a.*?(?=\||$)".Replace("a", subChar);
            try
            {
                if (ht.ContainsKey(field))
                {
                    Match match1 = Regex.Match(ht[field].ToString(), pattern);
                    if (match1.Success)
                    {
                        retStr = match1.Value.Substring(2);
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message); 
            }   
            return retStr;

        }

        /// <summary>
        /// 解析MARC
        /// </summary>
        /// <param name="marcRecord"></param>
        /// <returns>字段，子字段，内容</returns>
        public static Dictionary<string,string> ExtractMarcFields(string marcRecord)
        {
            var fields = new Dictionary<string, string>();
            Regex regex = new Regex(@"(\d{3}).{4}(\|.*?)\\r\\n", RegexOptions.Singleline);
            MatchCollection matches = regex.Matches(marcRecord);

            foreach (Match match in matches)
            {
                string tag = match.Groups[1].Value;
                string content = match.Groups[2].Value;
                //重复字段合并
                if (fields.ContainsKey(tag))
                {
                    fields[tag] = fields[tag] + content;
                }
                else
                { 
                    fields.Add(tag, content);
                }
            }
            return fields;
        }

        public Msg Insert(Biblios book)
        {
            Msg msg = new Msg();
            try
            {
                _books.InsertOne(book);
                msg.Code = 0;
            }
            catch (Exception ex) {
                msg.Code = 100;
                msg.Message = ex.Message;
            }
            return msg;
        }

        public Msg Update(string identifier, Biblios updatedBook)
        {
            //不使用replaceOne整体替换，据说是updateOne高效一些
            Msg msg = new Msg();

            var updateDefinitionBuilder = Builders<Biblios>.Update;
            var updates = new List<UpdateDefinition<Biblios>>();
            try
            {
                // 根据字段是否为 null 构建更新定义，应该使用反射的循环
                var properties = updatedBook.GetType().GetProperties();
                foreach (var property in properties)
                {
                    //这三个字段不能更改
                    if (property.Name == "Identifier" || property.Name == "Id" || property.Name== "UserName")
                    {
                        continue;
                    }
                    //更新非空字段
                    var value = property.GetValue(updatedBook, null);
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
                    var filter = Builders<Biblios>.Filter.Eq(b => b.Identifier, identifier);
                    var result = _books.UpdateOne(filter, updateDefinition);

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
            catch (Exception ex) {
                msg.Code = 100;
                msg.Message = ex.Message;
            }

            return msg;
        }


        public Msg Delete(string identifier)
        {
            Msg msg=new Msg();
            try
            {
                _books.DeleteOne(book => book.Identifier == identifier);
                msg.Code = 0;
            }
            catch (Exception ex)
            {
                msg.Message=ex.Message;
                msg.Code = 100;
            }
            return msg;
        }

    }
}
