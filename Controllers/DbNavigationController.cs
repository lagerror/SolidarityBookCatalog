using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Threading.Tasks;

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DbNavigationController : ControllerBase
    {
        private readonly ElasticsearchClient _elastic;
        public DbNavigationController(IConfiguration config)
        {
            string connStr = config.GetConnectionString("ElasticSearchDB").ToString();
            var settings = new ElasticsearchClientSettings(new Uri(connStr));
            settings.DefaultIndex("Others");
            _elastic = new ElasticsearchClient(settings);
        }
        [HttpGet]
        public async Task<Msg> fun(string act, string pars)
        {
            Msg msg = new Msg();
            switch (act)
            {
                case "creatIndex":
                    var createIndexResponse = _elastic.Indices.CreateAsync<DbNavigation>
                    (index => index
                    .Index("Others")
                    .Mappings(mappings => mappings
                        .Properties(p => p
                           .Text(t => t.DocTypes)  //数据库类型
                           .Keyword(k => k.Initial)   //首字母
                           .Keyword(k => k.Language) // 中外文
                           .Text(t => t.Database)  //数据库名字
                           )
                        )
                    );


                    if (createIndexResponse.Result.IsValidResponse)
                    {
                        msg.Code = 0;
                        msg.Message = $"创建Others索引成功";
                    }
                    else
                    {
                        msg.Code = 1;
                        msg.Message = $"创建Others索引失败";
                    }
                    break;
                case "insertOne":
                    DbNavigation dbNavigation = new DbNavigation();
                    dbNavigation.Initial = "B";
                    dbNavigation.Language = "ch";
                    dbNavigation.Database = "博看期刊数据库";
                    dbNavigation.DocTypes = "期刊/会议论文";
                    dbNavigation.Url = "https://lib.yangtzeu.edu.cn/info/1014/1043.htm";

                    var res =  _elastic.IndexAsync<DbNavigation>(dbNavigation).Result;
                    if (res.IsValidResponse)
                    {
                        msg.Code = 0;
                        msg.Message = $"插入成功";
                    }
                    else
                    {
                        msg.Code = 1;
                        msg.Message = $"插入失败";
                    }
                    break;



            }
            return msg;
        }
    }
}
