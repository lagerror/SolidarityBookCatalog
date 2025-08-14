using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.AspNetCore.Mvc;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Models.CDLModels;
using SolidarityBookCatalog.Services;
using System.Threading.Tasks;
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
        private readonly ILogger _logger;
        private IConfiguration _configuration;
        private string filePath;
        public IsdlController(IsdlLoanService isdlLoanService, BibliosService bibliosService, ILogger<IsdlController> logger,ReaderService readerService,ToolService toolService,IConfiguration configuration)
        {
            _sdlLoanService = isdlLoanService;
            _bibliosService = bibliosService;
            _logger = logger;
            _readerService = readerService;
            _toolService = toolService;
            _configuration = configuration;
            filePath =_configuration["ISDL:path"];
        }

        [HttpPost("decrypt")]
        [RequestSizeLimit(100_000_000)] // 限制100MB
        [Consumes("application/octet-stream")]
        public async Task<IActionResult> DecryptFile(string openId,string applyId)
        {
            try
            {
                // 1. 获取当前用户信息
                Msg msg = new Msg();
                var ret = _toolService.DeCryptOpenId(openId);

                if (!ret.Item1)
                {
                    msg.Code = 3;
                    msg.Message = $"OpenId解密失败";
                    return BadRequest($"Invalid file format: {msg.Message}");
                }

                openId = ret.Item2;
                var reader =await _readerService.SearchByOpenId(openId); // 实现此方法获取当前用户
                IsdlLoanWork loan = await _sdlLoanService.getLoanById(applyId);  //获取指定的借阅记录

                //获取要解密的文件
                var originalFileName =   $"{filePath}\\{reader.ReaderNo}_{loan.ISBN}.enc";
                var downloadFileName = $"{reader.ReaderNo}_{loan.ISBN}.{loan.FileType}";
               

               

                using (var fileStream = System.IO.File.OpenRead(originalFileName)) {
                    Response.Headers.ContentDisposition = $"attachment; filename={downloadFileName}";
                    Response.ContentType = "application/octet-stream";

                   await _toolService.DecryptFileStreaming(
                   reader,
                   loan,
                   fileStream,
                  Response.Body);
                }
                return new EmptyResult();
            }
            catch (InvalidDataException ex)
            {
                return BadRequest($"Invalid file format: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Decryption failed: {ex.Message}");
            }
        }

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
            var originalFileName = filePath + $"\\{loan.ISBN}";
            if (System.IO.File.Exists(originalFileName + ".epub"))
            {
                originalFileName = originalFileName + ".epub";
            }
            else if (System.IO.File.Exists(originalFileName + ".pdf"))
            {
                originalFileName = originalFileName + ".pdf";
            }

            var downloadFileName = reader.ReaderNo + "_" + loan.ISBN + ".enc";

            
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{downloadFileName}\"");
            Response.ContentType = "application/octet-stream";

            // 4. 打开文件流
            await using var fileStream = new FileStream(
                originalFileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920, // 与加密缓冲区匹配
                FileOptions.SequentialScan); // 优化顺序读取

            // 5. 流式加密和传输
            await _toolService.EncryptFile(
                reader,
                loan,
                fileStream,
                Response.Body);

            return new EmptyResult(); // 数据已直接写入响应体
        }

        //查询借阅记录
        [HttpGet("GetLoanListByOpenId")]
        public async Task<IActionResult> GetLoanListByOpenId(string readerOpenId)
        {
            Msg msg = new Msg();
            var ret = _toolService.DeCryptOpenId(readerOpenId);

            if (!ret.Item1)
            {
                msg.Code = 100;
                msg.Data = "openId解密失败";
                return Ok(msg);
            }
            string openId = ret.Item2;
            try {
                var loans = await _sdlLoanService.GetLoansListByOpenIdAsync(openId);
                msg.Code = 0;
                msg.Data = loans;
            } catch (Exception ex) 
            {
                msg.Code = 101;
                msg.Data = ex.Message;
            
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
