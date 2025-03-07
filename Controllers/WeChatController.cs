using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Web;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
//https://open.weixin.qq.com/connect/oauth2/authorize?appid=wx86605acd8f3d0820&redirect_uri=https%3A%2F%2Freader.yangtzeu.edu.cn%2Fsolidarity%2Fapi%2Fwechat%2Foauth&response_type=code&scope=snsapi_userinfo&state=STATE&connect_redirect=1#wechat_redirect
namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeChatController : ControllerBase
    {
        private IMongoCollection<Reader> _readers;
        private ToolService _toolService;
        IConfiguration _configuration;
        IWeChatService _weChatService;

        public WeChatController(IConfiguration configuration,IWeChatService weChatService,ToolService toolService, IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("BookReShare");
            _readers = database.GetCollection<Reader>("reader");
            _configuration = configuration;
            _weChatService = weChatService;
            _toolService = toolService;
        }
        [HttpGet]
        public string Get(string echoStr, string signature, string timestamp, string nonce)
        {
            Console.WriteLine("{0},{1},{2},{3}", echoStr, signature, timestamp, nonce);
            string token = _weChatService.GetTokenAsync().Result;
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
                    string token = await _weChatService.GetTokenAsync();
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = token;
                    break;
                case "getopenid":
                    string code = pars;
                    string openid = await _weChatService.GetOpenIdByCodeAsync(code);
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = openid;
                    break;
                case "getjsapiticket":
                    string ticket = await _weChatService.GetJsapiTicketAsync();
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = ticket;
                    break;
                case "getjsapisignature":
                    string cryptOpenid = pars;
                    string signature = await _weChatService.GetJsapiSignature(cryptOpenid, "https://reader.yangtzeu.edu.cn/wechat/scan");
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = signature;
                    break;
                case "EncryptOpenId":
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    
                    msg.Data = _toolService.EnCryptOPenId(pars).Item2;
                    break;
                case "DecryptOpenId":
                    msg.Code = 0;
                    msg.Message = "获取成功";
                    msg.Data = _toolService.DeCryptOpenId(pars).Item2;
                    break;
                case "SendTemplateMessageAsync":
                   var data = new
                    {
                        keyword1 = new { value = "长江大学图书馆一楼服务台中转柜", color = "#ff0000" },
                        keyword2 = new { value = "10号格取件码：806018", color = "#ff0000" },
                    };
                    /*收货人
                       {{thing17.DATA}}
                       收货地址
                       {{thing18.DATA}}
                       客服电话
                       {{phone_number5.DATA}}
                       订单编号
                       {{character_string1.DATA}}
                       门店名称
                       {{thing3.DATA}}
                    
                    var apply = new { 
                        thing17=new { value = "300120" },
                        thing18=new { value = "湖北荆州长江大学图书馆西校区" },
                        phone_number5=new {value="13972384460" },
                        character_string1=new {value="cd10000001" },
                        thing3=new { value="荆州文理学院图书馆"}
                    };
                    */

                    /*{ { first.DATA} }
                    读者姓名：{ { keyword1.DATA} }
                    图书题名：{ { keyword2.DATA} }
                    图书条码：{ { keyword3.DATA} }
                    借书日期：{ { keyword4.DATA} }
                    应还日期：{ { keyword5.DATA} }
                    { { remark.DATA} }
                    

                    var loan = new { 
                        first=new { value = "联盟借书申请" },
                        keyword1=new {value="300120:李靖：13972384460" },
                        keyword2=new {value="长江大学东校区：快乐第一" },
                        keyword3=new { value = "cd100000002,社会书库" },
                        keyword4=new { value ="20250303,东校区"    },
                        keyword5=new {value="20250318"},
                        remark=new { value="听说题头和尾注不显示了"}
                    };
                    await _weChatService.SendTemplateMessageAsync("loan",pars, "https://reader.yangtzeu.edu.cn/wechat/",loan);
                   */ 
                    var notice = new
                    {
                        keyword1 = new { value = $"destinationLockerInfo.LockerDetail.Owner" },  //学校
                        keyword2 = new { value = $"快递员联系方式：destinationLockerInfo.CourierDetail.name:destinationLockerInfo.CourierDetail.phone" },  //通知人，快递员电话
                        keyword3 = new { value = $"destinationLockerInfo.DepositTime?.ToString" },   // 时间
                        keyword4 = new { value = $"自助取书地点：" },   //取货的地点

                    };
                    await _weChatService.SendTemplateMessageAsync("notice", pars, $"https://reader.yangtzeu.edu.cn/wechat/my?openId={pars}", notice);

                    break;

            }
            return Ok(msg);
        }

        // GET api/<WeChatController>/5
        [HttpGet]
        [Route("oauth")]
        public async  Task<IActionResult> oauth(string code, string state)
        {
            try
            {
                string? openId = await _weChatService.GetOpenIdByCodeAsync(code);
                Console.WriteLine($"code:{code};state:{state};openid:{openId}");
                if (openId != null)
                {
                    //查询数据库是否存在该用户
                    var reader = _readers.Find(r => r.OpenId == openId).FirstOrDefault();
                    //加密openid并编码
                    openId = _toolService.EnCryptOPenId(openId).Item2;
                    openId = HttpUtility.UrlEncode(openId);
                    if (reader == null)  //注册
                    {
                        Console.WriteLine($"https://reader.yangtzeu.edu.cn/wechat/register?openId={openId}");
                        return Redirect($"https://reader.yangtzeu.edu.cn/wechat/register?openId={openId}");
                    }
                    else     //登录
                    {
                        Console.WriteLine($"https://reader.yangtzeu.edu.cn/wechat/my?openId={openId}");
                        return Redirect($"https://reader.yangtzeu.edu.cn/wechat/my?openId={openId}");
                    }
                }
                else   //错误
                {
                    Console.WriteLine($"https://reader.yangtzeu.edu.cn/wechat/error?openId=null");
                    return Redirect($"https://reader.yangtzeu.edu.cn/wechat/error?openId=null");
                }
                //string ticket = await _weChatTokenService.GetJsapiTicketAsync();
                //string token=await _weChatTokenService.GetTokenAsync();
                //string cryptOpenId= Tools.EncryptStringToBytes_Aes(openid, _configuration["Crypt:Key"], _configuration["Crypt:iv"]);
                //string jsApiSign = await _weChatTokenService.GetJsapiSignature(cryptOpenId);
                //Console.WriteLine($"code:{code};state:{state};openid:{openid};token:{token};jsapisign:{jsApiSign}");
            }
            catch (Exception ex)
            {
                return Redirect($"https://reader.yangtzeu.edu.cn/wechat/error?openId={ex.Message}");
            }
        }

        [HttpGet]
        [Route("getJsSign")]
        public async Task<IActionResult> GetJsSign(string cryptOpenId)
        {
            Msg msg = new Msg();
            var tuple = _toolService.DeCryptOpenId(cryptOpenId);
            if (!tuple.Item1)
            {
                msg.Code = 1;
                msg.Message = "解密失败";
                return Ok(msg);
            }
            string? jsApiSign = await _weChatService.GetJsapiSignature(cryptOpenId, "https://reader.yangtzeu.edu.cn/wechat/scan");
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

    }
}
