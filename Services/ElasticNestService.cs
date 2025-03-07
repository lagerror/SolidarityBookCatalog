using Nest;
using SolidarityBookCatalog.Models;
using System.Collections;
using SortOrder = Nest.SortOrder;
namespace SolidarityBookCatalog.Services
{
    public class ElasticNestService
    {
        private readonly Nest.ElasticClient _client;

        public ElasticNestService(IConfiguration config)
        {
            string connStr = config.GetConnectionString("ElasticSearchDB").ToString();
            var settings = new ConnectionSettings(new Uri(connStr));
            settings.DefaultIndex("biblios");
            _client = new ElasticClient(settings);
        }

        public async Task<Msg> SearchAsync(SearchQueryList queryList, int rows=10,int page=1)
        {
            Msg msg = new Msg();
            Hashtable ht=new Hashtable();
            //防止大数据量返回和深度翻页
            if (rows > 100)
            {
                rows = 100;
            }
            if (page > 10) { 
                page = 10;
            }
            //将查询逻辑组合起来
            var queryAll = new QueryContainer();
            foreach (var item in queryList.List)
            {
                QueryContainer query = null;
                //防止传参异常
                switch (item.Field)
                {
                    case "title":
                        query = new QueryContainerDescriptor<Biblios>().Match(m => m.Field(f => f.Title).Query(item.Keyword));
                        break;
                    case "creator":
                        query = new QueryContainerDescriptor<Biblios>().Match(m => m.Field(f => f.Creator).Query(item.Keyword));
                        break;
                    case "publisher":
                        query = new QueryContainerDescriptor<Biblios>().Match(m => m.Field(f => f.Publisher).Query(item.Keyword));
                        break;
                    case "identifier":
                        query = new QueryContainerDescriptor<Biblios>().Match(m => m.Field(f => f.Identifier).Query(item.Keyword));
                        break;
                    case "year":
                        query = new QueryContainerDescriptor<Biblios>().Match(m => m.Field(f => f.Date).Query(item.Keyword));
                        break;
                    case "topic":  //主题检索，分词检索主题词和简介
                        query = new QueryContainerDescriptor<Biblios>().Bool(b => b
                            .Should(
                                s => s.MatchPhrase(m => m.Field(f => f.Subject).Query(item.Keyword)),
                                s => s.MatchPhrase(m => m.Field(f => f.Description).Query(item.Keyword))
                            )
                        );
                        break;
                    default:
                        msg.Code = 1;
                        msg.Message = "不符合检索条件";
                        return msg;
                }
                if (query != null)
                {
                    if (item.Logic == null)
                    {
                        queryAll= query;
                    }
                    else if (item.Logic == "and")
                    {
                       queryAll&=query;
                    }
                    else if (item.Logic == "or")
                    {
                        queryAll |= query;
                    }
                }
            }

            try
            {
                var response = await _client.SearchAsync<Biblios>(s => s
                    .Index("biblios")
                    .Query(q => queryAll)
                    .Size(rows)  // 返回前10条记录
                    //.Sort(sort => sort
                    //        .Field(f => f.Date, SortOrder.Descending) // 按出版年倒序排序
                    //    )
                    .From((page - 1) * rows)
                     .Aggregations(a => a
                        // 聚合出版社
                        .Terms("by_publisher", t => t
                            .Field(f => f.Publisher)  // 使用 keyword 类型字段防止被分析
                            .Size(10)  // 获取前10条聚合记录
                        )
                        // 聚合出版年
                        .Terms("by_publish_date", t => t
                            .Field(f => f.Date)  // 按出版年聚合
                            .Size(10)  // 获取前10条出版年
                        )
                    )
                );


                //返回记录总数
                ht.Add("total", response.HitsMetadata.Total.Value);

                //返回书目记录
                ht.Add("rows", response.Documents);

                // 输出出版年聚合结果
                Dictionary<string, long> aggYear = new Dictionary<string, long>();
                if (response.Aggregations.Terms("by_publish_date") != null)
                {
                    var dateBuckets = response.Aggregations.Terms("by_publish_date").Buckets;
                    foreach (var bucket in dateBuckets)
                    {
                        aggYear.Add(bucket.Key, (long)bucket.DocCount);
                    }
                }
                ht.Add("aggYear", aggYear);

                // 输出出版社聚合结果
                Dictionary<string, long> aggPublisher = new Dictionary<string, long>();
                if (response.Aggregations.Terms("by_publisher") != null)
                {
                    var publisherBuckets = response.Aggregations.Terms("by_publisher").Buckets;
                    foreach (var bucket in publisherBuckets)
                    {
                        aggPublisher.Add(bucket.Key, (long)bucket.DocCount);
                    }
                }
                ht.Add("aggPublisher", aggPublisher);
                
                msg.Code = 0;
                msg.Message = "查询成功";
                msg.Data = ht;
            }
            catch (Exception e) {
                msg.Code = 100;
                msg.Message = e.Message;
            }

            return msg;
        }


        public async Task<Msg> SearchBooksAsync(string keyword)
        {
            Msg msg=new Msg();
            var response = await _client.SearchAsync<Biblios>(s => s
                .Index("biblios")  // 替换为您的索引名称
                .Size(10)  // 返回前10条记录
                .Sort(sort => sort
                .Field(f => f.Date, SortOrder.Descending) // 按出版年倒序排序
                    )
                .Query(q => q
                    .Bool(b => b
                        .Must(mu => mu
                            .Match(m => m
                                .Field(f => f.Title)
                                .Query(keyword)
                            )
                        )
                        .Filter(fi => fi
                            .MatchAll() // 全字段检索
                        )
                    )
                )
                .Aggregations(a => a
                    // 聚合出版社
                    .Terms("by_publisher", t => t
                        .Field(f=>f.Publisher)  // 使用 keyword 类型字段防止被分析
                        .Size(10)  // 获取前10条聚合记录
                    )
                    // 聚合出版年
                    .Terms("by_publish_date", t => t
                        .Field(f=>f.Date)  // 按出版年聚合
                        .Size(10)  // 获取前10条出版年
                    )
                )
            );

            // 输出查询结果
            Console.WriteLine($"Total Hits: {response.HitsMetadata.Total.Value}");

            foreach (var hit in response.Hits)
            {
                Console.WriteLine($"ID: {hit.Id}, Title: {hit.Source}");
            }


            // 输出出版年聚合结果
            if (response.Aggregations.Terms("by_publish_date") != null)
            {
                var dateBuckets = response.Aggregations.Terms("by_publish_date").Buckets;
                foreach (var bucket in dateBuckets)
                {
                    Console.WriteLine($"Year: {bucket.Key}, Count: {bucket.DocCount}");
                }
            }
            // 输出出版社聚合结果
            if (response.Aggregations.Terms("by_publisher") != null)
            {
                var publisherBuckets = response.Aggregations.Terms("by_publisher").Buckets;
                foreach (var bucket in publisherBuckets)
                {
                    Console.WriteLine($"Publisher: {bucket.Key}, Count: {bucket.DocCount}");
                }
            }

            return msg;
        }


    }
}
