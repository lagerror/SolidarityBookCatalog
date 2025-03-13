
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using SolidarityBookCatalog.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoldingController : ControllerBase
    {

        private readonly BibliosService _bookService;
        private readonly UserService _userService;
        private readonly HoldingService _holdingService;
        private readonly ILogger<HoldingController> _logger;

        public HoldingController(BibliosService bookService, UserService userService,HoldingService holdingService, ILogger<HoldingController> logger   )
        {
            _bookService = bookService;
            _userService = userService;
            _holdingService = holdingService;
            _logger = logger;
        }

        // GET: api/<HoldingController>
        [HttpGet]
        [Route("fun")]
        public IEnumerable<string> fun(string act,string pars)
        {
            //var indexKeysDefinition = Builders<Holding>.IndexKeys.Ascending(Holding => Holding.Identifier);
            //var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
            //var indexModel = new CreateIndexModel<Holding>(indexKeysDefinition, indexOptions);
            //_holdingService._holdings.Indexes.CreateOneAsync(indexModel);

            //_userService._users.Indexes.CreateOneAsync(indexModel);

            // 定义复合索引键，用于单个图书馆的馆藏唯一 	identifier	bookrecno	UserName
            //var indexKeysDefinition = Builders<Holding>.IndexKeys
            //    .Ascending("identifier")
            //    .Ascending("bookrecno")
            //    .Ascending("UserName");

            //// 创建索引模型
            //try
            //{
            //    var indexModel = new CreateIndexModel<Holding>(indexKeysDefinition, new CreateIndexOptions { Unique = true });
            //    _holdingService._holdings.Indexes.CreateOneAsync(indexModel);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}

            return new string[] { "value1", "value2" };
        }
        //生成借阅二维码
        [HttpGet]
        [Route("qr")]
        public  IActionResult qr(string url)
        {
            return File(ToolService.GenerateQRCode(url, 4), "image/png");
        }
        //生成带logo凤凰的二维码，要从GITHUB加载qrcode.cs到本地来扩展
        [HttpGet]
        [Route("qrLogo")]
        public IActionResult qrLogo(string url)
        {
            url = $"https://open.weixin.qq.com/connect/oauth2/authorize?appid=wx86605acd8f3d0820&redirect_uri=https://reader.yangtzeu.edu.cn/solidarity/api/wechat/oauth&response_type=code&scope=snsapi_userinfo&state={url}&connect_redirect=1#wechat_redirect";
            return File(ToolService.GenerateQRCodeWithLogo(url, 4), "image/png");
        }

        /// <summary>
        /// 通过ISBN查询馆藏
        /// </summary>
        /// <param name="identifier">ISBN</param>
        /// <returns>馆藏记录</returns>
        [HttpGet]
        [Route("identifier")]
        public ActionResult<Msg> Get(string identifier)
        {
            Msg msg = new Msg();
            try
            {
                var tuple = Biblios.validIsbn(identifier);
                if (tuple.Item1)
                {
                    var holding = _holdingService.Get(tuple.Item2);
                    if (holding != null)
                    {
                        msg.Code = 0;
                        msg.Message = "查询到馆藏";
                        msg.Data = holding;
                    }
                    else
                    {
                        msg.Code = 1;
                        msg.Message = "没有查询到馆藏";
                    }
                }
                else
                {
                    msg.Code = 2;
                    msg.Message = "isbn没有通过校验";
                }
            }
            catch (Exception ex) {
                msg.Code = 100;
                msg.Message = $"馆藏查询异常：{ex.Message}";
            }

            return msg;
        }


        /// <summary>
        /// flag=vant时候，返回层级数据
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="prefix"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search")]
        public ActionResult<Msg> Search(string identifier,string prefix="all",string flag="common")
        {
            Msg msg = new Msg();
            try
            {
                var tuple = Biblios.validIsbn(identifier);
                if (tuple.Item1)
                {
                    List<Holding> holding = _holdingService.search(tuple.Item2,prefix);
                    if (holding != null)
                    {
                        msg.Code = 0;
                        msg.Message = "查询到馆藏";
                        msg.Data = holding;
                        if (flag == "vant")
                        {
                            msg= _holdingService.SearchVant(holding);
                            return msg;
                        }
                    }
                    else
                    {
                        msg.Code = 1;
                        msg.Message = "没有查询到馆藏";
                    }
                }
                else
                {
                    msg.Code = 2;
                    msg.Message = "isbn没有通过校验";
                }
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = $"馆藏查询异常：{ex.Message}";
            }

            return msg;
        }

        [HttpPost]
        [Route("search")]
        public async Task<IActionResult> Search(SearchQueryList list, int rows = 10, int page = 1)
        {
            Msg msg = new Msg();
            try
            {
                msg = await _holdingService.searchAsync(list, rows, page);
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = $"查询错误：{ex.Message}";
            }
            return Ok(msg);
        }


        [HttpPost]
        [Route("insert")]
        [Authorize(Policy = "AdminOrManager")]
        public ActionResult<Msg> Insert(Holding holding)
        {
            Msg msg = new Msg();
            msg.Message = "暂未开放";
            return Ok(msg);
            try
            {
                _holdingService.insert(holding);
                msg.Code = 0;
                msg.Message = "插入成功";
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = $"插入异常：{ex.Message}";
            }
            return msg;
        }

        [HttpPost]
        [Route("update/{identifier}")]
        [Authorize(Policy = "AdminOrManager")]
        public ActionResult<Msg> Update(string identifier, Holding holding)
        {
            Msg msg = new Msg();
            msg.Message = "暂未开放";
            return Ok(msg);
            try
            {
                _holdingService.Update(holding.Identifier, holding);
                msg.Code = 0;
                msg.Message = "更新成功";
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = $"更新异常：{ex.Message}";
            }
            return msg;
        }
        [HttpPost]
        [Route("delete/{identifier}")]
        [Authorize(Policy = "AdminOrManager")]
        public ActionResult<Msg> Delete(string identifier)
        {
            Msg msg = new Msg();
            msg.Message = "暂未开放";
            return Ok(msg);
            try
            {
                _holdingService.Delete(identifier);
                msg.Code = 0;
                msg.Message = "删除成功";
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = $"删除异常：{ex.Message}";
            }
            return msg;
        }

        /// <summary>
        /// 提交各馆馆藏
        /// </summary>
        /// <param name="appId">各馆appId</param>
        /// <param name="nonce">时间</param>
        /// <param name="sign">签名</param>
        /// <param name="holding">馆藏</param>
        /// <returns>MSG</returns>
        [HttpPost]
        [Route("insertSign")]
        public ActionResult<Msg> InsertSign(string appId,string nonce,string sign,Holding holding)
        {
            Msg msg=new Msg();
            User user=new User();
            user.AppId = appId;
            user.Nonce = nonce;
            user.Sign = sign;
            //1签名验证
            msg=_userService.Sign(holding.Identifier, user);
            if (msg.Code != 0)
            {
                msg.Message = "无效签名";
                return Ok(msg);
            }

            //2检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool, string> tuple = Biblios.validIsbn(holding.Identifier);
            if (tuple.Item1)
            {
                holding.Identifier = tuple.Item2;
            }
            else
            {
                msg.Code = 1;
                msg.Message = "isbn不合规范";
                return Ok(msg);
            }

            //3检查是否存在相同用户，isbn,记录号的馆藏
            if (_holdingService.RepeatKeyHolding(holding.Identifier, appId, holding.BookRecNo))
            {
                msg.Code = 4;
                msg.Message = $"已有对应馆藏:{holding.Identifier};{appId};{holding.BookRecNo}";
                return Ok(msg);
            }

            //4权限验证
            msg = _userService.chmod("", appId, "holding", PublicEnum.ActionType.insert);
            if (msg.Code != 0)
             {
                    msg.Code = 5;
                    msg.Message = "没有插入数据的权限";
                    return Ok(msg);
             }
            holding.UserName = appId;
            msg=_holdingService.insert(holding);

            return msg;
        }

        [HttpPost]
        [Route("updateSign")]
        public ActionResult<Msg> updateSign(string appId, string nonce, string sign, Holding holding)
        {
            Msg msg = new Msg();

            User user = new User();
            user.AppId = appId;
            user.Nonce = nonce;
            user.Sign = sign;
            //如果验证成功
            msg = _userService.Sign(holding.Identifier, user);
            if (msg.Code != 0)
            {
                msg.Code = 1;
                msg.Message = "签名没有通过";
                return Ok(msg);
            }
            //检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool, string> tuple = Biblios.validIsbn(holding.Identifier);
            if (tuple.Item1)
            {
                holding.Identifier = tuple.Item2;
            }
            else
            {
                msg.Code = 1;
                msg.Message = "isbn不合规范";
                return Ok(msg);
            }
            //权限验证
            msg = _userService.chmod(holding.Identifier, appId,"holding", PublicEnum.ActionType.update);
            if (msg.Code != 0)
            {
                msg.Code = 4;
                msg.Message = $"没有修改数据{holding.Identifier}的权限";
                return Ok(msg);
            }
            //更新数据
            msg = _holdingService.Update(holding.Identifier, holding);
            return Ok(msg);
        }

        [HttpPost]
        [Route("deleteSign")]
        public ActionResult<Msg> DeleteSign(string appId, string nonce, string sign, string identifier)
        {
            Msg msg = new Msg();

            User user = new User();
            user.AppId = appId;
            user.Nonce = nonce;
            user.Sign = sign;

            //签名验证
            msg = _userService.Sign(identifier, user);
            if (msg.Code != 0)
            {
                msg.Message = "签名没有通过";
                return Ok(msg);
            }
            //检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool, string> tuple = Biblios.validIsbn(identifier);
            if (tuple.Item1)
            {
                identifier = tuple.Item2;
            }
            else
            {
                msg.Code = 1;
                msg.Message = "isbn不合规范";
                return Ok(msg);
            }
            //权限验证
            msg = _userService.chmod(identifier, appId,"holding", PublicEnum.ActionType.delete);
            if (msg.Code != 0)
            {
                msg.Code = 4;
                msg.Message = $"没有删除数据{identifier}的权限";
                return Ok(msg);
            }
            //删除
            msg = _holdingService.Delete(identifier);
            return msg;
        }

       
    }
}
