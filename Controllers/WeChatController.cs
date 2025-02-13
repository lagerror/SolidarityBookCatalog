using Microsoft.AspNetCore.Mvc;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeChatController : ControllerBase
    {
        IConfiguration _configuration;
        IWeChatTokenService _weChatTokenService;
        public WeChatController(IConfiguration configuration,IWeChatTokenService weChatTokenService)
        {
            _configuration = configuration;
            _weChatTokenService = weChatTokenService;
        }

        // GET: api/<WeChatController>
        [HttpGet]
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
                    string signature = await _weChatTokenService.GetJsapiSignature(cryptOpenid);
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = signature;
                    break;
            }
            return Ok(msg);
        }

        // GET api/<WeChatController>/5
        [HttpGet]
        [Route("oauth")]
        public async  Task<IActionResult> oauth(string code, string state)
        {
            
            string openid = await _weChatTokenService.GetOpenIdByCodeAsync(code);
            string ticket = await _weChatTokenService.GetJsapiTicketAsync();
            string token=await _weChatTokenService.GetTokenAsync();
            string cryptOpenId= Tools.EncryptStringToBytes_Aes(openid, _configuration["Crypt:Key"], _configuration["Crypt:iv"]);
            string jsApiSign = await _weChatTokenService.GetJsapiSignature(cryptOpenId);
            Console.WriteLine($"code:{code};state:{state};openid:{openid};token:{token};jsapisign:{jsApiSign}");
            return Ok();
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
