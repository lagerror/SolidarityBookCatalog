using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Text.RegularExpressions;

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BibliosController : ControllerBase
    {
        private readonly BibliosService _bookService;
        private readonly UserService _userService;
       
        public BibliosController(BibliosService bookService,UserService userService)
        {
            _bookService = bookService;
            _userService = userService;
        }

        [HttpGet]
        [Route("fun")]
        public IActionResult fun(string act,string pars)
        {
            switch(act)
            {
                case "index":// 创建唯一索引
                    var indexKeysDefinition = Builders<Biblios>.IndexKeys.Ascending(book => book.Identifier);
                    var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
                    var indexModel = new CreateIndexModel<Biblios>(indexKeysDefinition, indexOptions);
                    _bookService._books.Indexes.CreateOneAsync(indexModel);
                    break;
                case "91marc":
                    Msg msg =  _bookService.GetFrom91Async(pars).Result;
                    break;
            }
            return Ok();
        }
        [HttpGet]
        public ActionResult<List<Biblios>> Get()
        {
            var books = _bookService.Get().Take(10).ToList();
            return books;
        }

        [HttpGet]
        [Route("marc91")]
        public async Task<Msg> marc91(string appId, string nonce, string sign, string identifier)
        { 
            Msg msg=new Msg();

            User user = new User();
            user.AppId = appId;
            user.Nonce = nonce;
            user.Sign = sign;

            //如果签名验证成功
            msg = _userService.Sign(identifier, user);
            if (msg.Code != 0)
            {
                return msg;
            }
            //检查是否为yangtzeu
            if (appId != "hubei.jingzhou.yangtzeu.library")
            {
                msg.Code = 1;
                msg.Message = "没有从第三方下载MARC权限";
                return msg;
            }
            //下载并插入到biblios并返回
            msg = await _bookService.GetFrom91Async(identifier);

            return msg;
        }

        /// <summary>
        /// 通过ISBN查询基本书目信息
        /// </summary>
        /// <param name="identifier">isbn传入时候无需转换</param>
        /// <returns>基本书目信息</returns>
        [HttpGet]
        [Route("identifier")]
        public async Task<Msg> GetAsync(string identifier)
        {
            Msg msg=new Msg();
            
            try
            {
                //检查输入的ISBN
                var tuple=Biblios.validIsbn(identifier);
                if (tuple.Item1)
                {
                    var book = _bookService.Get(tuple.Item2);
                    if (book != null)
                    {
                        msg.Code = 0;
                        msg.Message = "查到书目";
                        msg.Data = book;
                        return msg;
                    }
                    else
                    {
                        msg.Code = 1;
                        msg.Message = "没有找到对应的书目";
                    }
                }
                else
                {
                    msg.Code = 2;
                    msg.Message = "isbn号没有通过校验";
                }
               
            }
            catch (Exception ex) {
                msg.Code = 101;
                msg.Message = ex.Message;
            }

            return msg;
        }

        [HttpPost]
        public ActionResult<Msg> Insert(Biblios book)
        {
            Msg msg = new Msg();

            //model校验
            //检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool,string> tuple= Biblios.validIsbn(book.Identifier);
            if (tuple.Item1)
            {
                book.Identifier = tuple.Item2;
            }
            else { 
                msg.Code = 1;
                msg.Message = "isbn不合规范";
                return Ok(msg);
            }
            //检查出版年
            Tuple<bool,string> tuple1=Biblios.validYear(book.Date);
            if ((tuple1.Item1))
            {
                book.Date = tuple1.Item2;
            }
            else { 
                msg.Code= 2;
                msg.Message = "出版年位4位年份";
                return Ok(msg);
            }
            //检查价格
            Tuple<bool, decimal> tuple2 = Biblios.validPrice(book.Price.ToString());
            if ((tuple2.Item1))
            {
                book.Price = tuple2.Item2;
            }
            else {
                msg.Code = 3;
                msg.Message = "价格位最多两位小数的十进制数字";
                return Ok(msg);
            }
            //插入数据
            try
            {
                msg = _bookService.Insert(book);
            }
            catch (Exception ex) {
                msg.Code = 101;
                msg.Message =msg.Message+ ex.Message;
            }
            return Ok(msg);
        }
        
        /// <summary>
        /// 各馆通过用户名上传书目数据
        /// </summary>
        /// <param name="appId">馆用户分配的appId</param>
        /// <param name="nonce">时间字符串</param>
        /// <param name="sign">签名字符串</param>
        /// <param name="book">图书基本数据，其中出版社，出版年，价格会进行检查</param>
        /// <returns>MSG，code=0为正常，其它见message返回信息</returns>
        [HttpPost]
        [Route("insertSign")]
        public ActionResult<Msg> InsertSign(string appId,string nonce,string sign,Biblios book)
        { 
            Msg msg = new Msg();
            User user = new User();
            user.AppId = appId;
            user.Nonce = nonce;
            user.Sign = sign;
            
            //如果签名验证成功
            msg = _userService.Sign(book.Identifier, user);
            if (msg.Code != 0)
            {
                return Ok(msg);
            }


            #region model校验
            //检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool, string> tuple = Biblios.validIsbn(book.Identifier);
            if (tuple.Item1)
            {
                book.Identifier = tuple.Item2;
                //提前查找ISBN是否有对应记录
                if (_bookService.Get(book.Identifier) != null) { 
                    msg.Code=5;
                    msg.Message = "ISBN对应记录存在";
                    return Ok(msg);
                }
            }
            else
            {
                msg.Code = 1;
                msg.Message = "isbn不合规范";
                return Ok(msg);
            }
            //检查出版年
            Tuple<bool, string> tuple1 = Biblios.validYear(book.Date);
            if ((tuple1.Item1))
            {
                book.Date = tuple1.Item2;
            }
            else
            {
                msg.Code = 2;
                msg.Message = "出版年位4位年份";
                return Ok(msg);
            }
            //检查价格
            Tuple<bool, decimal> tuple2 = Biblios.validPrice(book.Price.ToString());
            if ((tuple2.Item1))
            {
                book.Price = tuple2.Item2;
            }
            else
            {
                msg.Code = 3;
                msg.Message = "价格位最多两位小数的十进制数字";
                return Ok(msg);
            }
            #endregion

            #region 权限验证
            msg= _userService.chmod("",appId,"biblios", PublicEnum.ActionType.insert);
            if(msg.Code != 0) 
            { 
                msg.Code= 4;
                msg.Message = "没有插入数据的权限";
                return Ok(msg); 
            }
            #endregion
            //插入,并记录创建者
            book.UserName = appId;
            msg = _bookService.Insert(book);

            return Ok(msg);
        }
        [HttpPut]
        public ActionResult<Msg> Update(string identifier, Biblios book)
        {
            Msg msg = new Msg();



            //模型校验
            //检查输入的ISBN，去掉-，把10位的转换位13位
            if (book.Identifier != null) {
                Tuple<bool, string> tuple = Biblios.validIsbn(book.Identifier);
                if (tuple.Item1)
                {
                    book.Identifier = tuple.Item2;
                }
                else
                {
                    msg.Code = 1;
                    msg.Message = "isbn不合规范";
                    return Ok(msg);
                }
            }
            //检查年份
            if (book.Date != null)
            {
                Tuple<bool, string> tuple1 = Biblios.validYear(book.Date);
                if ((tuple1.Item1))
                {
                    book.Date = tuple1.Item2;
                }
                else
                {
                    msg.Code = 2;
                    msg.Message = "出版年位4位年份";
                    return Ok(msg);
                }
            }
            
            //检查价格
            if (book.Price != null) {
                Tuple<bool, decimal> tuple2 = Biblios.validPrice(book.Price.ToString());
                if ((tuple2.Item1))
                {
                    book.Price = tuple2.Item2;
                }
                else
                {
                    msg.Code = 3;
                    msg.Message = "价格位最多两位小数的十进制数字";
                    return Ok(msg);
                }

            }


            var bookOrgin = _bookService.Get(identifier);

            if (bookOrgin == null)
            {
                msg.Code = 1;
                msg.Message = $"没有找到对应{identifier}的记录";
            }
            try
            {
                msg = _bookService.Update(identifier, book);
            }
            catch (Exception ex) { 
                msg.Code=101;
                msg.Message=msg.Message+ex.Message; 
            }

            return  Ok(msg);
        }

        [HttpPost]
        [Route("updateSign")]
        public ActionResult<Msg> UpdateSign(string appId, string nonce, string sign, Biblios book)
        { 
            Msg msg = new Msg();

            User user = new User();
            user.AppId = appId;
            user.Nonce = nonce;
            user.Sign = sign;
            //如果验证成功
            msg = _userService.Sign(book.Identifier, user);
            if (msg.Code != 0)
            {
                msg.Code = 1;
                msg.Message = "签名没有通过";
                return Ok(msg);
            }
            //检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool, string> tuple = Biblios.validIsbn(book.Identifier);
            if (tuple.Item1)
            {
                book.Identifier = tuple.Item2;
            }
            else
            {
                msg.Code = 1;
                msg.Message = "isbn不合规范";
                return Ok(msg);
            }
            //权限验证
            msg = _userService.chmod(book.Identifier, appId,"biblios", PublicEnum.ActionType.update);
            if (msg.Code != 0)
            {
                msg.Code = 4;
                msg.Message = $"没有修改数据{book.Identifier}的权限";
                return Ok(msg);
            }
            //更新数据
            msg =_bookService.Update(book.Identifier, book);

            return Ok(msg);
        }


        [HttpDelete]
        public ActionResult<Msg> Delete(string identifier)
        {
            Msg msg= new Msg();

            var book = _bookService.Get(identifier);

            if (book == null)
            {
                msg.Code = 10;
                msg.Message = $"没找到对应{identifier}的记录";
            }
            try
            {
                msg = _bookService.Delete(book.Identifier);
            }
            catch (Exception ex) {
                msg.Code = 101;
                msg.Message = ex.Message;
            
            }
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
            msg = _userService.chmod(identifier, appId,"biblios", PublicEnum.ActionType.delete);
            if (msg.Code != 0)
            {
                msg.Code = 4;
                msg.Message = $"没有删除数据{identifier}的权限";
                return Ok(msg);
            }
            //删除
            msg= _bookService.Delete(identifier);
            return msg;
        }
    }
}
