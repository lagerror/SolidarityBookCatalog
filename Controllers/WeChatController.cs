using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SharpCompress.Readers;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Web;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeChatController : ControllerBase
    {
        private IMongoCollection<Reader> _readers;
        IConfiguration _configuration;
        IWeChatTokenService _weChatTokenService;

        public WeChatController(IConfiguration configuration,IWeChatTokenService weChatTokenService,IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("BookReShare");
            _readers = database.GetCollection<Reader>("reader");
            _configuration = configuration;
            _weChatTokenService = weChatTokenService;
        }
        [HttpGet]
        public string Get(string echoStr, string signature, string timestamp, string nonce)
        {
            Console.WriteLine("{0},{1},{2},{3}", echoStr, signature, timestamp, nonce);
            string token = _weChatTokenService.GetTokenAsync().Result;
            string[] data = new string[] { nonce, timestamp, token };
            var temp = Tools.WeChatSign(data);
            Console.WriteLine(temp);
            return echoStr;
        }
        // GET: api/<WeChatController>
        [HttpGet]
        [Route("fun")]
        public async Task<IActionResult> Fun(string act,string pars)
        {
            Msg msg = new Msg();
            switch (act)
            {
                case "gettoken":
                    string token = await _weChatTokenService.GetTokenAsync();
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = token;
                    break;
                case "getopenid":
                    string code = pars;
                    string openid = await _weChatTokenService.GetOpenIdByCodeAsync(code);
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = openid;
                    break;
                case "getjsapiticket":
                    string ticket = await _weChatTokenService.GetJsapiTicketAsync();
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = ticket;
                    break;
                case "getjsapisignature":
                    string cryptOpenid = pars;
                    string signature = await _weChatTokenService.GetJsapiSignature(cryptOpenid, "https://reader.yangtzeu.edu.cn/wechat/scan");
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = signature;
                    break;
                case "EncryptOpenId":
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = _weChatTokenService.EncryptOpenId(pars).Item2;
                    break;
                case "DecryptOpenId":
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = _weChatTokenService.DecryptOpenId(pars).Item2;
                    break;
               
            }
            return Ok(msg);
        }

        // GET api/<WeChatController>/5
        [HttpGet]
        [Route("oauth")]
        public async  Task<IActionResult> oauth(string code, string state)
        {
            string? openId = await _weChatTokenService.GetOpenIdByCodeAsync(code);
            if (openId != null)
            {
                openId = _weChatTokenService.EncryptOpenId(openId).Item2;
                //查询数据库是否存在该用户
                var reader=_readers.Find(r => r.OpenId == openId).FirstOrDefault();
                if (reader == null)  //注册
                {
                    openId = HttpUtility.UrlEncode(openId);
                    return Redirect($"https://reader.yangtzeu.edu.cn/wechat/register?openId={openId}");
                }
                else     //登录
                {
                    openId = HttpUtility.UrlEncode(openId);
                    return Redirect($"https://reader.yangtzeu.edu.cn/wechat/my?openId={openId}");
                }
            }
            else   //错误
            {
                return Redirect($"https://reader.yangtzeu.edu.cn/wechat/error?openId=null");
            }
            //string ticket = await _weChatTokenService.GetJsapiTicketAsync();
            //string token=await _weChatTokenService.GetTokenAsync();
            //string cryptOpenId= Tools.EncryptStringToBytes_Aes(openid, _configuration["Crypt:Key"], _configuration["Crypt:iv"]);
            //string jsApiSign = await _weChatTokenService.GetJsapiSignature(cryptOpenId);
            //Console.WriteLine($"code:{code};state:{state};openid:{openid};token:{token};jsapisign:{jsApiSign}");
          
        }

        [HttpGet]
        [Route("getJsSign")]
        public async Task<IActionResult> GetJsSign(string cryptOpenId)
        {
            Msg msg = new Msg();
            var tuple = _weChatTokenService.DecryptOpenId(cryptOpenId);
            if (!tuple.Item1)
            {
                msg.Code = 1;
                msg.Message = "解密失败";
                return Ok(msg);
            }
            string? jsApiSign = await _weChatTokenService.GetJsapiSignature(cryptOpenId, "https://reader.yangtzeu.edu.cn/wechat/scan");
            if (jsApiSign == null)
            {
                msg.Code = 2;
                msg.Message = "获取失败";
                return Ok(msg);
            }
            else
            {
                msg.Code = 0;
                msg.Message = "获取成功";
                msg.Data = jsApiSign;
                return Ok(msg);
            }
        }
            // POST api/<WeChatController>
            [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<WeChatController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<WeChatController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
