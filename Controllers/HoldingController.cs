using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoldingController : ControllerBase
    {

        private readonly BookService _bookService;
        private readonly UserService _userService;
        private readonly HoldingService _holdingService;

        public HoldingController(BookService bookService, UserService userService,HoldingService holdingService)
        {
            _bookService = bookService;
            _userService = userService;
            _holdingService = holdingService;
        }

        // GET: api/<HoldingController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            //var indexKeysDefinition = Builders<Holding>.IndexKeys.Ascending(Holding => Holding.Identifier);
            //var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
            //var indexModel = new CreateIndexModel<Holding>(indexKeysDefinition, indexOptions);
            //_holdingService._holdings.Indexes.CreateOneAsync(indexModel);

            //_userService._users.Indexes.CreateOneAsync(indexModel);
            return new string[] { "value1", "value2" };
        }

        // GET api/<HoldingController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            Holding holding=new Holding();
            holding.Identifier = "9787500161844";
            Book book= _bookService.Get(holding.Identifier);
            if (book != null)
            {
                holding.BookRecNo = "1900180543";
                holding.UserName= "hubei.jingzhou.yangtzeu.library";
                List<string> list = new List<string>();
                string item = "CD11097715,I267.5/169,CD,H01";
                list.Add(item);
                item = "CD11108039,I267.5/169,CD,C11";
                list.Add(item);
                item = "CD11108040,I267.5/169,CD,C11";
                list.Add(item);
                item = "CD11108041,I267.5/169,CD,C11";
                list.Add(item);
                holding.Barcode = list;
                _holdingService.insert(holding);
            }
          

            return "value";
        }

        // POST api/<HoldingController>
        [HttpPost]
        [Route("insertSign")]
        public ActionResult<Msg> InsertSign(string appId,string nonce,string sign,Holding holding)
        {
            Msg msg=new Msg();
            User user=new User();
            user.AppId = appId;
            user.Nonce = nonce;
            user.Sign = sign;
            //签名验证
            msg=_userService.Sign(holding.Identifier, user);
            if (msg.Code != 0)
            {
                msg.Message = "无效签名";
                return Ok(msg);
            }

            //检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool, string> tuple = Book.validIsbn(holding.Identifier);
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
            msg = _userService.chmod("", appId, "holding", PublicEnum.ActionType.insert);
            if (msg.Code != 0)
             {
                    msg.Code = 4;
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
            Tuple<bool, string> tuple = Book.validIsbn(holding.Identifier);
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
            Tuple<bool, string> tuple = Book.validIsbn(identifier);
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
