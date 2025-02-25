using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoanWorkController : ControllerBase
    {
        IConfiguration _configuration;
        IMongoClient _client;
        ILogger<LoanWorkController> _logger;
        IMongoCollection<LoanWork> _loanWork;
        IMongoCollection<Reader> _reader;
        HttpClient _httpClient;
        private readonly string _cryptKey;
        private readonly string _cryptIv;
        private readonly string _baseUrl;
        public LoanWorkController(IMongoClient client, HttpClient httpClient, IConfiguration configuration,ILogger<LoanWorkController> logger)
        {
            _configuration = configuration;
            _baseUrl = _configuration["Express:BaseUrl"];
            _httpClient = httpClient;
            var database = client.GetDatabase("BookReShare");
            _loanWork = database.GetCollection<LoanWork>("loanWork");
            _reader = database.GetCollection<Reader>("reader");
            _logger = logger;
            _cryptKey = _configuration["Crypt:key"];
            _cryptIv = _configuration["Crypt:iv"];
        }

        // GET: api/<LoanWorkController>
        [HttpGet]
        public IActionResult fun(string act,string pars)
        {
            Msg msg = new Msg();
            switch (act)
            {
                case "index":// 创建唯一索引
                    var indexKeysDefinition = Builders<LoanWork>.IndexKeys.Ascending(x => x.Id);
                    var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
                    var indexModel = new CreateIndexModel<LoanWork>(indexKeysDefinition, indexOptions);
                    _loanWork.Indexes.CreateOneAsync(indexModel);
                    break;
            }

                return Ok(msg);
        }

        /// <summary>
        /// 哪个人，申请哪本书，从哪个柜子到哪个柜子
        /// </summary>
        /// <param name="readerOpenId">人</param>
        /// <param name="holdingId">书</param>
        /// <param name="sourceLocker">从哪个柜子</param>
        /// <param name="destinationLocker">到哪个柜子</param>
        /// <returns></returns>
        [HttpPost]
        [Route("apply")]
        public  async Task<IActionResult> apply (string readerOpenId,string holdingId,string destinationLocker)
        {
            Msg msg = new Msg();
            try
            {//rfWTm26wJZnpQ%252bS0Jgywkd1CwWuzRhO7mTGiI0%252fQZlY%253d  rfWTm26wJZnpQ+S0Jgywkd1CwWuzRhO7mTGiI0/QZlY= oS4N5tzvoJn2uJ7b1PzMJj99JgTw rfWTm26wJZnpQ+bS0Jgywkd1CwWuzRhO7mTGiI0%2fQZlY=    675805eff80692346fa8abe9   675805eff80692346fa8abe9
                string v3 = WebUtility.UrlDecode(readerOpenId);
                string v1 = Tools.EncryptStringToBytes_Aes("oS4N5tzvoJn2uJ7b1PzMJj99JgTw", _cryptKey, _cryptIv);
                string v2 = Tools.DecryptStringFromBytes_Aes(v1, _cryptKey, _cryptIv);
                //校验openId的解密
                var ret = Tools.DecryptStringFromBytes_Aes(readerOpenId, _cryptKey, _cryptIv);
                if (ret == null)
                {
                    msg.Code = 3;
                    msg.Message = $"OpenId解密失败";
                    return Ok(msg);
                }
                readerOpenId = ret;

                LoanWork loanWork = new LoanWork();
                ApplicationInfo applicationInfo = new ApplicationInfo();
                loanWork.Application = applicationInfo;

                loanWork.HoldingId = holdingId;
                loanWork.Status = PublicEnum.CirculationStatus.已申请;
                loanWork.Application.ReaderOpenId = readerOpenId;
                loanWork.Application.DestinationLocker = destinationLocker;

                await _loanWork.InsertOneAsync(loanWork);

                msg.Code = 0;
                msg.Message = "申请成功";
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = ex.Message;
            }   
            return Ok(msg);
        }

        /// <summary>
        /// 图书馆互借人员找书入柜处理事务
        /// </summary>
        /// <param name="applyId">申请事务号</param>
        /// <param name="openId">工作人员</param>
        /// <param name="lockerNumber">放入哪个柜体,自动获取空余格口</param>
        
        /// <returns></returns>
        [HttpPost]
        [Route("libraryProcess")]
        public async Task<IActionResult> Post(string applyId, string openId, string iccid)
        {
            Msg msg = new Msg();
            //校验openId的解密
            var ret = Tools.DecryptStringFromBytes_Aes(openId, _cryptKey, _cryptIv);
            if (ret == null)
            {
                msg.Code = 1;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            openId = ret;

            //判断该人员是否为图书馆找书工作人员
            var reader= _reader.FindAsync<Reader>(x => x.OpenId == openId).Result.FirstOrDefault();
            if (reader == null)
            {
                msg.Code = 2;
                msg.Message = "没有找到对应的工作人员";
                return Ok(msg);

            }
            if (reader.Type != PublicEnum.Type.馆际互借人员)
            {
                msg.Code = 3;
                msg.Message = $"该用户{reader.Name}没有馆际互借人员权限";
                return Ok(msg);
            }

            //找到事务
            var apply = _loanWork.FindAsync(x => x.Id == applyId).Result.FirstOrDefault();
            if (apply == null)
            {
                msg.Code = 4;
                msg.Message = "没找到对应申请事务号";
                return Ok(msg);
            }
           

            //建立图书馆人员处理流程节点
            LibraryProcessingInfo libraryProcessingInfo = new LibraryProcessingInfo();
            //开柜自动分配格口
            string url = _baseUrl + $"/api/Locker/OpenEmptyCell?iccid={iccid}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            // 确保HTTP响应状态为200 (OK)
            if (response.IsSuccessStatusCode)
            {
                // 读取响应内容
                string responseBody = await response.Content.ReadAsStringAsync();
                Msg msgTemp= System.Text.Json.JsonSerializer.Deserialize<Msg>(responseBody);

                if (msgTemp.Code == 0)
                {
                    libraryProcessingInfo.LockerNumber =iccid;   //柜子
                    libraryProcessingInfo.CellNumber = msgTemp.Message.Split('|')[1];   //格口
                    apply.Status = PublicEnum.CirculationStatus.图书已找到;    //状态
                }
                else
                {
                    apply.Status = PublicEnum.CirculationStatus.申请终止;
                    libraryProcessingInfo.Remark = msgTemp.Message;
                    msg.Code= 5;
                    msg.Message += msgTemp.Message;
                    return Ok(msg);
                }
            }
            else
            {
                msg.Code = 6;
                msg.Message += $"开柜请求发送错误";
                return Ok(msg);
            }
            //将图书馆取书信息加入到流程节点
            apply.LibraryProcessing = libraryProcessingInfo;
            //更新事务
            await _loanWork.ReplaceOneAsync(x => x.Id == applyId, apply);

            return Ok(msg);
        }

        // PUT api/<LoanWorkController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<LoanWorkController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
