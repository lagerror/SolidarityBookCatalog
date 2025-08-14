using MongoDB.Bson;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;

namespace SolidarityBookCatalog.Services
{
    public class ReaderService
    {
        public readonly IMongoCollection<Reader> _readers;

        public ReaderService(IConfiguration config, IMongoClient client)
        {
            var database = client.GetDatabase("BookReShare");
            _readers = database.GetCollection<Reader>("reader");
        }
        public async Task<Reader> SearchByOpenId(string openId)
        { 
            Reader reader=await _readers.Find(r=>r.OpenId==openId).FirstOrDefaultAsync();
            return reader;
        }

        public async Task<Msg> SearchAsync(SearchQueryList list, int rows = 10, int page = 1)
        {
            Msg msg = new Msg();
            // 构建过滤条件
            var filterBuilder = Builders<Reader>.Filter;
            var filters = new List<FilterDefinition<Reader>>();
            foreach (var item in list.List)
            {
                if (item.Field == "Phone")
                {
                    filters.Add(filterBuilder.Regex(d => d.Phone, new BsonRegularExpression(item.Keyword, "i")));
                }
                else if (item.Field == "Name")
                {
                    filters.Add(filterBuilder.Regex(d => d.Name, new BsonRegularExpression(item.Keyword, "i")));
                }
                else if (item.Field == "ReaderNo")
                {
                    filters.Add(filterBuilder.Eq(x => x.ReaderNo, item.Keyword));
                }
                else if (item.Field == "Library")
                {
                    filters.Add(filterBuilder.Eq(x => x.Library, item.Keyword));
                }
                else if (item.Field == "IsValid")
                {
                    filters.Add(filterBuilder.Eq(x => x.IsValid, bool.Parse(item.Keyword)));
                }

            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : FilterDefinition<Reader>.Empty;

            // 执行查询
            var total = await _readers.CountDocumentsAsync(finalFilter);
            var results = await _readers.Find(finalFilter)
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

    }
}
