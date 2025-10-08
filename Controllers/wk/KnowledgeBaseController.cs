using DnsClient.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio.DataModel.Args;
using Nest;
using Npgsql;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Models.CDLModels;
using SolidarityBookCatalog.Models.WKModels;
using SolidarityBookCatalog.Services;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers.wk
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeBaseController : ControllerBase
    {
        //64682c50-7437-4563-983e-7ce6311065a9
        WkService _WkService;
        MinioService _minioService;
        string connStr;
        ToolService _toolService;
        ReaderService _readerService;
        FiscoService _fiscoService;
        string bucketName;
        ILogger<KnowledgeBaseController> _logger;
        public KnowledgeBaseController(ILogger<KnowledgeBaseController> logger, FiscoService fiscoService, ReaderService readerService, ToolService toolService, WkService wkService,MinioService minioService,IConfiguration configuration) 
        {
            _logger = logger;
            _fiscoService = fiscoService;
            bucketName = configuration["Minio:Bucket"].ToString();
            _readerService = readerService;
            _toolService = toolService;
            _WkService = wkService;
            _minioService = minioService;
            connStr= configuration["ConnectionStrings:PgDB"].ToString();
        }
        // 获取所有的知识库
        [HttpPost]
        [Route("search")]
       // [Authorize(Policy = "AdminOrManager")]
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
        //[Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> UpFile(string id,IFormFile file)
        { 
            Msg msg=await _WkService.upFileAsync(id,file);
            return Ok(msg);
        }
        //上传EPUB格式的文件便于流式阅读
        [HttpPost]
        [Route("upfileEpub")]
       // [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> UpFileEpub(string id, IFormFile file)
        {

            Msg msg = new Msg();
            string objectName = $"epub/{id}{Path.GetExtension(file.FileName)}";
            
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connStr))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        string commStr = "update knowledges set source=@source where id=@id";
                        cmd.CommandText = commStr;
                        cmd.Connection = conn;
                         NpgsqlParameter[] pars = new NpgsqlParameter[] {
                                new NpgsqlParameter("@source",objectName),
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
                            msg.Message = "上传EPUB中没有修改成功";
                        }
                        cmd.Dispose();
                        conn.Close();
                    }


                }
            }
            catch (Exception ex) { 
                msg.Code = 100;
                msg.Message =$"上传EPUB异常：{ ex.Message}";
            
            }
            return Ok(msg);

        }
        //修改文件的元数据biblios
        [HttpPost]
        [Route("metadata")]
        public async Task<IActionResult> updata(Biblios biblios, string id,string title,string description)
        {
            //24e9271f-d3ed-476e-b2fe-c65275cf5e6a
            Msg msg = new Msg();
            try
            {
                string bibliosStr = JsonSerializer.Serialize(biblios);
                using (NpgsqlConnection conn = new NpgsqlConnection(connStr))
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        string commStr = "update knowledges set title=@title,description=@description, metadata=@metadata where id=@id";
                        cmd.CommandText = commStr;
                        cmd.Connection = conn;
                        NpgsqlParameter[] pars = new NpgsqlParameter[] {
                            new NpgsqlParameter("metadata", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = bibliosStr },
                            new NpgsqlParameter("id", id),
                            new NpgsqlParameter("title", title),
                            new NpgsqlParameter("description", description)
                        };
                        cmd.Parameters.AddRange(pars);
                        int i = cmd.ExecuteNonQuery();
                        if (i == 1)
                        {
                            msg.Code = 0;
                        }
                        else
                        {
                            msg.Code = 1;
                            msg.Message = $"更新metaData没有成功";

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = $"更新metaData异常:{ex.Message}";
            
            }
            return Ok(msg);
        }

        //查询知识库下文件列表，分页
        [HttpPost]
        [Route("knowledge")]
       // [Authorize(Policy = "AdminOrManager")]
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

        //根据readerOpenId和申请号下载加密文件
        //rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC1X7lACD/5x6lt8bINtNADL
        //d4872a5a-1212-4a46-a05f-38b3df846b4b
        [HttpGet("knowledgeDownload")]
        public async Task<IActionResult> knowledgeDownload(string openId= "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC1X7lACD/5x6lt8bINtNADL", string knowledgeId= "979f50ee-1c14-4de7-84fb-65a47a935988", string flag="pdf")
        {
            // 1. 获取当前用户信息
            Msg msg = new Msg();
            var ret = _toolService.DeCryptOpenId(openId);

            if (!ret.Item1)
            {
                msg.Code = 3;
                msg.Message = $"OpenId解密失败";
                return new EmptyResult();
            }

            //openId = ret.Item2;
            var reader = await _readerService.SearchByOpenId(ret.Item2); // 实现此方法获取当前用户
            reader.OpenId = openId; // 保持原始的OpenId用于加密
            var knowledge = await _WkService.GetKnowledgeItemAsync(knowledgeId);  //获取知识文件信息
           
            //如果没有借阅记录或者文件没准备好返回空
            if (reader == null || knowledge==null)
            {
                return new EmptyResult();
            }
                    

            //获取要加密发送的文件名
            string originalFileName;
            if (flag=="epub")
            {
                originalFileName =$"{knowledge.source}";
            }
            else 
            {
                originalFileName = knowledge.file_path.Substring(19);
            }

            var downloadFileName = reader.ReaderNo + "_" + knowledge.id+"_"+flag + ".enc";
            //从minio获取文件
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{downloadFileName}\"");
            Response.ContentType = "application/octet-stream";

            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject($"{originalFileName}");
                var stat = await _minioService._minioClient.StatObjectAsync(args).ConfigureAwait(false);
                if (stat == null)
                {
                    return new EmptyResult();
                }
                //获取文件流
                Stream? minioStream = null;
                var memoryBuffer = new MemoryStream();  //使用一个缓存，避免直接连接两个网络流
                var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(originalFileName)
                .WithCallbackStream(async (stream, cancellationToken) =>
                {
                    minioStream = stream;
                    await stream.CopyToAsync(memoryBuffer, 81920, cancellationToken);
                    memoryBuffer.Position = 0;
                });
                await _minioService._minioClient.GetObjectAsync(getObjectArgs).ConfigureAwait(false);
                // 5. 流式加密和传输
                await _toolService.EncryptKnowledgeFile(reader, knowledge, memoryBuffer, Response.Body);

            }

            catch (Exception ex)
            {
                Response.StatusCode = 500;
                await Response.WriteAsJsonAsync(ex);
                return new EmptyResult();

            }

            //6.写入Fisco区块链
            JsonSerializerOptions _options = new()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            IsdlLoanWork loan = new IsdlLoanWork();
            loan.Biblios = new Biblios();
            loan.Reader=new Reader();
            loan.Biblios.Description = "";
            loan.Reader = reader;
            loan.Reader.Phone = "";
            loan.Reader.OpenId = "";
            loan.ReaderOpenId = "";
            loan.ISBN = knowledge.id;
            loan.FilePath = originalFileName;
            loan.FileType = flag;
            loan.Biblios.Title = knowledge.title;
            loan.Biblios.Description=knowledge.description;

            string value = JsonSerializer.Serialize<IsdlLoanWork>(loan, _options);
            msg = await _fiscoService.SetCollaborationAsync("", value);
            if (msg.Code != 0)
            {
                _logger.LogError(JsonSerializer.Serialize<Msg>(msg, _options));
            }
            return new EmptyResult(); // 数据已直接写入响应体
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
        //直接上传文件到minio
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
