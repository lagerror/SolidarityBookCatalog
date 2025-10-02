using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio.DataModel.Args;
using MongoDB.Driver;
using SharpCompress.Compressors.Xz;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Models.CDLModels;
using SolidarityBookCatalog.Services;
using System.IO;
using System.IO.Pipes;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using static SolidarityBookCatalog.Services.PublicEnum;
using static System.Net.Mime.MediaTypeNames;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IsdlController : ControllerBase
    {
        private readonly ReaderService _readerService;
        private readonly BibliosService _bibliosService;
        private readonly ToolService _toolService;
        private readonly IsdlLoanService _sdlLoanService;
        private readonly IWeChatService _weChatService;
        private readonly IMongoClient _client;
        IMongoCollection<IsdlLoanWork> _isdlLoanWork;
        private readonly ILogger _logger;
        private IConfiguration _configuration;
        private string filePath;
        private string bucketName;
        private string userAddress;
        private FiscoService _fiscoService;
        private MinioService _minioService;
        public IsdlController(MinioService minioService, FiscoService fiscoService, IWeChatService weChatService, IMongoClient client, IsdlLoanService isdlLoanService, BibliosService bibliosService, ILogger<IsdlController> logger,ReaderService readerService,ToolService toolService,IConfiguration configuration)
        {
            var database = client.GetDatabase("BookReShare");
            _weChatService = weChatService;
            _isdlLoanWork = database.GetCollection<IsdlLoanWork>("isdlLoanWork");
            _sdlLoanService = isdlLoanService;
            _bibliosService = bibliosService;
            _logger = logger;
            _readerService = readerService;
            _toolService = toolService;
            _fiscoService= fiscoService;
            _configuration = configuration;
            _minioService= minioService;
            filePath =_configuration["ISDL:path"];
            bucketName = _configuration["Minio:Bucket"];

        }

        //[HttpPost("decrypt")]
        //[RequestSizeLimit(100_000_000)] // 限制100MB
        //[Consumes("application/octet-stream")]
        //public async Task<IActionResult> DecryptFile(string openId,string applyId)
        //{
        //    try
        //    {
        //        // 1. 获取当前用户信息
        //        Msg msg = new Msg();
        //        var ret = _toolService.DeCryptOpenId(openId);

        //        if (!ret.Item1)
        //        {
        //            msg.Code = 3;
        //            msg.Message = $"OpenId解密失败";
        //            return BadRequest($"Invalid file format: {msg.Message}");
        //        }

        //        openId = ret.Item2;
        //        var reader =await _readerService.SearchByOpenId(openId); // 实现此方法获取当前用户
        //        IsdlLoanWork loan = await _sdlLoanService.getLoanById(applyId);  //获取指定的借阅记录

        //        //获取要解密的文件
        //        var originalFileName =   $"{filePath}\\{reader.ReaderNo}_{loan.ISBN}.enc";
        //        var downloadFileName = $"{reader.ReaderNo}_{loan.ISBN}.{loan.FileType}";
               

               

        //        using (var fileStream = System.IO.File.OpenRead(originalFileName)) {
        //            Response.Headers.ContentDisposition = $"attachment; filename={downloadFileName}";
        //            Response.ContentType = "application/octet-stream";

        //           await _toolService.DecryptFileStreaming(
        //           reader,
        //           loan,
        //           fileStream,
        //          Response.Body);
        //        }
        //        return new EmptyResult();
        //    }
        //    catch (InvalidDataException ex)
        //    {
        //        return BadRequest($"Invalid file format: {ex.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Decryption failed: {ex.Message}");
        //    }
        //}

        //根据readerOpenId和申请号下载加密文件
        [HttpGet("download")]
        public async Task<IActionResult> DownloadEncryptedFile(string openId,string applyId)
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

            openId = ret.Item2;
            var reader =await _readerService.SearchByOpenId(openId); // 实现此方法获取当前用户
            IsdlLoanWork loan= await _sdlLoanService.getLoanById(applyId);  //获取指定的借阅记录
            //如果没有借阅记录或者文件没准备好返回空
            if (loan == null) { 
                return new EmptyResult();
            }
            //获取要加密发送的文件名
            var originalFileName = $"{loan.ISBN}";
            if (loan.FilePath.Contains(".epub"))
            {
                originalFileName = originalFileName + ".epub";
            }
            else 
            {
                originalFileName = originalFileName + ".pdf";
            }

            var downloadFileName = reader.ReaderNo + "_" + loan.ISBN + ".enc";
            //从minio获取文件
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{downloadFileName}\"");
            Response.ContentType = "application/octet-stream";
           
            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(originalFileName);
                var stat = await _minioService._minioClient.StatObjectAsync(args).ConfigureAwait(false);
                if (stat == null)
                {
                    return new EmptyResult();
                }
                //获取文件流
                Stream? minioStream=null;
                var memoryBuffer =new MemoryStream();  //使用一个缓存，避免直接连接两个网络流
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
                await _toolService.EncryptFile(reader,loan,memoryBuffer,Response.Body);

            }

            catch (Exception ex)
            {
                Response.StatusCode = 500;
                await Response.WriteAsJsonAsync(ex);
                return new EmptyResult();

            }

            // 4. 打开文件流
            //await using var fileStream = new FileStream(
            //    originalFileName,
            //    FileMode.Open,
            //    FileAccess.Read,
            //    FileShare.Read,
            //    bufferSize: 81920, // 与加密缓冲区匹配
            //    FileOptions.SequentialScan); // 优化顺序读取
            //从minio获取文件流


            //6.写入Fisco区块链
               JsonSerializerOptions _options = new()
               {
                   Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                   WriteIndented = false,
                   ReferenceHandler = ReferenceHandler.IgnoreCycles
               };
            loan.Biblios.Description = "";
            loan.Reader.Phone = "";
            loan.Reader.OpenId = "";
            loan.ReaderOpenId = "";
            string value=JsonSerializer.Serialize<IsdlLoanWork>(loan, _options);
            msg= await _fiscoService.SetCollaborationAsync("",value);
            if (msg.Code != 0)
            {
                _logger.LogError(JsonSerializer.Serialize<Msg>(msg, _options));
            }
            return new EmptyResult(); // 数据已直接写入响应体
        }

        //查询借阅记录
        [HttpPost("loanList")]
        public async Task<IActionResult> LoanList(SearchQueryList list,string readerOpenId, int rows = 10, int page = 1)
        {
            Msg msg = new Msg();
            var ret=_toolService.DeCryptOpenId(readerOpenId);
            string openId = "";
            if (ret.Item1)
            {
                openId = ret.Item2;
            }
            else {
                msg.Code = 100;
                msg.Message = "openId解密失败";
                return Ok(msg);
            }
                try
                {
                    SearchQuery searchQueryMyself = new SearchQuery();
                    searchQueryMyself.Field = "openId";
                    searchQueryMyself.Keyword = openId;
                    list.List.Add(searchQueryMyself);

                    msg = await search(list, rows, page);


                }
                catch (Exception ex)
                {
                    msg.Code = 101;
                    msg.Data = ex.Message;
                }

            return Ok(msg);
        }

        //查询
        [HttpPost]
        [Route("search")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<Msg> search(SearchQueryList list, int rows = 10, int page = 1)
        {
            Msg msg = new Msg();
            try
            {
                var builder = Builders<IsdlLoanWork>.Filter;
                var filters = new List<FilterDefinition<IsdlLoanWork>>();

                // 按字段分组处理条件
                var groupedConditions = list.List.GroupBy(item => item.Field);
                foreach (var group in groupedConditions)
                {
                    var field = group.Key;
                    var conditions = group.ToList();
                    var orFilters = new List<FilterDefinition<IsdlLoanWork>>();

                    foreach (var item in conditions)
                    {
                        // 根据字段类型生成对应的过滤器
                        switch (item.Field)
                        {
                            case "status":
                                if (Enum.TryParse<LoanStatus>(item.Keyword, out var status))
                                {
                                    orFilters.Add(builder.Eq(x => x.Status, status));
                                }
                                break;
                            case "readerOpenId":
                                orFilters.Add(builder.Eq(x => x.ReaderOpenId, item.Keyword));
                                break;
                            case "openId":
                                orFilters.Add(builder.Eq(x => x.OpenId, item.Keyword));
                                break;
                            case "id":
                                orFilters.Add(builder.Eq(x => x.Id, item.Keyword));
                                break;
                            case "phone":
                                orFilters.Add(builder.Eq(x => x.Reader.Phone, item.Keyword));
                                break;
                            case "readerNo":
                                orFilters.Add(builder.Eq(x=>x.Reader.ReaderNo, item.Keyword));  
                                break;
                            case "library":
                                orFilters.Add(builder.Eq(x=>x.Reader.Library, item.Keyword));   
                                break;
                            case "name":
                                orFilters.Add(builder.Eq(x => x.Reader.Name, item.Keyword));
                                break;

                           
                        }
                    }

                    if (orFilters.Count == 0)
                    {
                        continue; // 忽略无效条件
                    }
                    else if (orFilters.Count == 1)
                    {
                        filters.Add(orFilters[0]); // 单个条件直接添加
                    }
                    else
                    {
                        filters.Add(builder.Or(orFilters)); // 多个条件用OR组合
                    }
                }
                // 组合所有分组过滤器为AND条件
                var finalFilter = filters.Count > 0
                    ? builder.And(filters)
                    : FilterDefinition<IsdlLoanWork>.Empty;

                // 执行查询
                var total = await _isdlLoanWork.CountDocumentsAsync(finalFilter);
                var results = await _isdlLoanWork.Find(finalFilter).SortByDescending(x => x.ApplicationTime)
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

            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = ex.Message;
            }
            return msg;
        }
        //上传对应申请的文件，可以同时上传pdf和epub
        [HttpPost]
        [Route("upFile")]
        [RequestSizeLimit(100 * 1024 * 1024)]
       // [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> upFile(string id, IFormFile file)
        { 
            Msg msg = new Msg();
            //判断权限
            // 获取当前用户的ClaimsPrincipal
            var user = HttpContext.User;

            // 检查用户是否已经认证
            if (!user.Identity.IsAuthenticated)
            {
                msg.Code = 200;
                msg.Message = "用户没有登录";
                return Ok(msg);
            }
            
            //查找对应的借阅记录
            IsdlLoanWork loan=await _isdlLoanWork.Find(x=>x.Id==id).FirstOrDefaultAsync();
            if (loan == null) {
                msg.Code = 100;
                msg.Message = "没有找到对应的申请";
                return Ok(msg);
            }
            //保存文件
            //string fileName = Path.Combine(filePath, $"{loan.ISBN}{Path.GetExtension(file.FileName)}");
            //using (var stream = new FileStream(fileName, FileMode.Create))
            //{ 
            //   await file.CopyToAsync(stream);
            //}
            
            //上传文件到minio
            //_minioService._minioClient
            using (var stream = new MemoryStream())
            { 
                await file.CopyToAsync (stream);
                stream.Position = 0;
                //上传参数
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject($"{loan.ISBN}{Path.GetExtension(file.FileName)}")
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(Path.GetExtension(file.FileName));
                //开始上传
                try
                {
                    await _minioService._minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    msg.Code = 200;
                    msg.Message = $"文件上传到minio失败：{ex.Message}";
                    return Ok(msg);
                }
            }
            //更新记录状态
            var update = Builders<IsdlLoanWork>.Update
                .Set(x => x.FilePath, Path.GetExtension(file.FileName))
                .Set(x => x.FileUpTime, DateTime.UtcNow)
                .Set(x=>x.ExpiryDate, DateTime.UtcNow.AddDays(15))
                .Set(x => x.FileUpOpenId, user.Identity.Name)
                .Set(x => x.Status, LoanStatus.Approved);
            try
            {
                var result = await _isdlLoanWork.UpdateOneAsync(
                    Builders<IsdlLoanWork>.Filter.Eq(x => x.Id, id),
                    update
                    );
                if (result.ModifiedCount > 0)
                {
                    msg.Code = 0;
                    msg.Message = $"{id}上传文件成功";
                    loan = await _isdlLoanWork.Find(x => x.Id == id).FirstOrDefaultAsync();
                    //发送微信通知
                    var wxMessage = new
                    {
                        keyword1 = new { value = $"{loan.Reader?.Library}:{loan.Reader?.ReaderNo}：{loan.Reader?.Name}" },
                        keyword2 = new { value = $"{loan.Biblios?.Title}:{loan.Biblios?.Identifier}:{loan.Biblios?.Price.ToString()}" },  //书名
                        keyword3 = new { value = $"{loan.Biblios?.Publisher}:{loan.Biblios.Coverage}" },   // ISBN
                        keyword4 = new { value = $"{loan.FileUpTime?.ToString("yyyy-MM-dd")}" },   //借的时间
                        keyword5 = new { value = $"{loan.ExpiryDate?.ToString("yyyy-MM-dd")}设备还回" }   //还的时间地点
                    };
                    await _weChatService.SendTemplateMessageAsync("loan", loan.OpenId, $"https://reader.yangtzeu.edu.cn/wechat/my?openId={HttpUtility.UrlEncode(loan.ReaderOpenId)}", wxMessage);

                }
                else {
                    msg.Code = 101;
                    msg.Message = $"{id}上传文件失败";
                }
            }
            catch (Exception ex) {
                msg.Code = 102;
                msg.Message = ex.Message;
            }
            return Ok(msg);
        }
       
        //管理人员修改申请的状态，并给出原因，后台发送微信通知
        [HttpPost]
        [Route("status")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> status(string applyId, string remark, string status = "Rejected")
        {
            Msg msg = new Msg();
            //找到事务
            var apply = _isdlLoanWork.FindAsync(x => x.Id == applyId).Result.FirstOrDefault();
            if (apply == null)
            {
                msg.Code = 1;
                msg.Message = "没找到对应申请事务号";
                return Ok(msg);
            }

            apply.Remark = remark;

            try
            {
                Enum.TryParse<LoanStatus>(status, out var statusOut);
                apply.Status = statusOut;
            }
            catch
            {
                msg.Code = 2;
                msg.Message = "状态值无效";
                return Ok(msg);
            }

            // 创建更新定义
            var updateDefinition = Builders<IsdlLoanWork>.Update
                .Set(x => x.Remark, apply.Remark)
                .Set(x => x.Status, apply.Status);

            // 更新数据库中的文档
            var updateResult = _isdlLoanWork.UpdateOne(x => x.Id == apply.Id, updateDefinition);

            // 检查更新结果
            if (updateResult.ModifiedCount == 1)
            {
                msg.Code = 0;
                msg.Message = "更新成功";

                //发送微信通知
                //发送微信通知
                if (apply.ReaderOpenId != null)
                {
                    var notice = new
                    {
                        keyword1 = new { value = $"{apply.Reader.Library}" },  //学校
                        keyword2 = new { value = $"{apply.Reader.Name}借阅图书{apply.Biblios.Title}:状态更改" },  //通知人，快递员电话
                        keyword3 = new { value = $"{apply.ApplicationTime.ToString("yyyy-MM-dd")}" },   // 时间
                        keyword4 = new { value = $"该申请因为 {remark} 而取消" },   //取货的地点

                    };
                    await _weChatService.SendTemplateMessageAsync("notice", apply.OpenId, $"https://reader.yangtzeu.edu.cn/wechat/my?openId={HttpUtility.UrlEncode(apply.ReaderOpenId)}", notice);
                }
            }

            return Ok(msg);
        }
        //提交借阅申请
        [HttpGet]
        [Route("apply")]
        public async Task<IActionResult> apply(string readerOpenId,string isbn)
        {
            
            /*
             {
              "isbn": "9787115239792",
              "readerOpenId": "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC1X7lACD/5x6lt8bINtNADL"}
            */
            LoanApplicationDto application = new LoanApplicationDto();
            application.ISBN = isbn;
            application.ReaderOpenId = readerOpenId;

            Msg msg =new Msg();
            var ret = _toolService.DeCryptOpenId(readerOpenId);

            if (!ret.Item1)
            {
                msg.Code = 100;
                msg.Message = "openId解密失败";
                return Ok(msg);
            }
            string openId = ret.Item2;
            Reader reader=await _readerService.SearchByOpenId(openId);
            if (reader == null)
            {
                msg.Code = 101;
                msg.Message = "没有找到对应读者";
                return Ok(msg);
            }
            else 
            {
                if ((bool)!reader.IsValid)
                {
                    msg.Code = 102;
                    msg.Message = "读者证处于无效状态";
                }
            }

            try
            {
                //还要判断是否重复借阅ISbn
                
                var loan = await _sdlLoanService.CreateLoanAsync(application,openId);
                if (loan == null)
                {
                    msg.Code = 104;
                    msg.Message = "申请记录插入失败";
                }
                else
                {
                    msg.Message = "借阅成功";
                    msg.Code = 0;
                }
            }
                catch (Exception ex)
                {
                    msg.Code = 103;
                    msg.Message = "申请发生错误";
                    msg.Data = ex.ToString();
                }

            return Ok(msg);
        }
              

    }
}
