using DnsClient.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio.DataModel.Args;
using Npgsql;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers.wk
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeBaseController : ControllerBase
    {

        WkService _WkService;
        MinioService _minioService;
        string connStr;
        public KnowledgeBaseController(WkService wkService,MinioService minioService,IConfiguration configuration) 
        {
            _WkService = wkService;
            _minioService = minioService;
            connStr= configuration["ConnectionStrings:PgDB"].ToString();
        }
        // GET: api/<KnowledgeBaseController>
        [HttpPost]
        [Route("search")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> Search(SearchQueryList list, int rows = 20, int page = 1)
        {
            Msg msg = new Msg();
            var result =await  _WkService.GetKnowledgeBaseAsync();

            if (result == null) {
                msg.Code = 100;
                msg.Message = "查询库失败";
                return Ok(msg);
            }
            msg.Code = 0;
            msg.Data =new { 
                total=result.data.Count,
                rows= result.data 
            };
            return Ok(msg);
        }
        [HttpPost]
        [Route("upfile")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> UpFile(string id,IFormFile file)
        { 
            Msg msg=await _WkService.upFileAsync(id,file);
            return Ok(msg);
        
        }

        [HttpPost]
        [Route("upfileEpub")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> UpFileEpub(string id, IFormFile file)
        {
            //Msg msg = await _WkService.upFileAsync(id, file);
            Msg msg = new Msg();
            string objectName = $"{id}{Path.GetExtension(file.FileName)}";
            
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connStr))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        string commStr = "update knowledges set source=@source,metadata=@metadata where id=@id";
                        cmd.CommandText = commStr;
                        cmd.Connection = conn;
                        //biblios
                        Biblios biblios = new Biblios();
                        biblios.Title = "书目";
                        biblios.Identifier = "9781234567890";

                        string bibliosStr= JsonSerializer.Serialize(biblios);

                        NpgsqlParameter[] pars = new NpgsqlParameter[] {
                        new NpgsqlParameter("@source",objectName),
                        new NpgsqlParameter("@metadata",NpgsqlTypes.NpgsqlDbType.Jsonb){ Value=bibliosStr},
                        new NpgsqlParameter("@id",id)
                    };
                        cmd.Parameters.AddRange(pars);
                        int i = cmd.ExecuteNonQuery();
                        if (i == 1)
                        {
                            await UploadFileSinglePart(file, "solidarity", objectName);
                            msg.Code = 0;
                        }
                        else
                        {
                            msg.Code = 1;
                            msg.Message = "没有修改成功";
                        }


                    }


                }
            }
            catch (Exception ex) { 
                msg.Code = 100;
                msg.Message =$"数据库操作失败：{ ex.Message}";
            
            }
                return Ok(msg);

        }



        [HttpPost]
        [Route("knowledge")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> Get(string id, SearchQueryList list, int rows=20,int page=1)
        {
            Msg msg = new Msg();
            var result =await _WkService.GetKnowledgeAsync(id,rows,page);
            if (result == null)
            {
                msg.Code = 100;
                msg.Message = "查询库失败";
                return Ok(msg);
            }
            msg.Code = 0;
            msg.Data = new {
                total = result.total,
                rows = result.data
            };
            
            return Ok(msg);


        }

        // GET api/<KnowledgeBaseController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<KnowledgeBaseController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<KnowledgeBaseController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<KnowledgeBaseController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private async Task UploadFileSinglePart(IFormFile file, string bucketName, string objectName)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType);

            await _minioService._minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
        }
    }
}
