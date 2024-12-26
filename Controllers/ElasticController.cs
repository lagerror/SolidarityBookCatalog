using Microsoft.AspNetCore.Mvc;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ElasticController : ControllerBase
    {
        private ElasticService _elastic;
        public ElasticController(ElasticService elasticService)
        {
            _elastic = elasticService;
        
        }
        // GET: api/<ElasticController>
        [HttpGet]
        public  Msg fun(string act, string pars)
        {
            Msg msg = new Msg();
            //msg = _elastic.CreateIndex();
            //string isbn = "9787810893053";
            //msg = _elastic.InsertBibliosOneAsync(isbn).Result;
            //string id = "674ec5dc1e0bac6d98bbffc8";
            //msg = _elastic.GetBibliosOneByIdAsync(id).Result;

            //msg= _elastic.GetBibliosOneByIsbnAsync(isbn).Result;
            switch (act)
            {
                case "GetBibliosAllByText":
                    msg = _elastic.GetBibliosAllByText(pars).Result;
                    break;
                case "GetBibliosOneByIsbnAsync":
                    msg = _elastic.GetBibliosOneByIsbnAsync(pars).Result;
                    break;
                case "CreateIndex":
                    msg = _elastic.CreateIndex();
                    break;
                case "InsertBibliosOneAsync":
                    msg = _elastic.InsertBibliosOneAsync(pars).Result;
                    break;
                case "InsertBibliosAllAsync":
                    msg=_elastic.InsertBibliosAllAsync(pars).Result;
                    break;

            }

            return msg;
        }

       
        [HttpGet]
        [Route("searchByAll")]
        public async Task<Msg> searchByAll(string keyword, int pageSize = 10, int pageNum = 1)
        {
            Msg msg = new Msg();
            //判断是否为isbn
            var tuple = Biblios.validIsbn(keyword);
            if (tuple.Item1)
            {
                keyword = tuple.Item2;
            }
            msg =await _elastic.GetBibliosAllByText(keyword,pageSize,pageNum);
            
            return msg;
        }
    }
}
