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
    public class BooksController : ControllerBase
    {
        private readonly BookService _bookService;
       
        public BooksController(BookService bookService)
        {
            _bookService = bookService;
        }

        [HttpGet]
        [Route("fun")]
        public IActionResult fun()
        {
            //创建ISBN的索引
            // 创建唯一索引
            var indexKeysDefinition = Builders<Book>.IndexKeys.Ascending(book => book.Identifier);
            var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
            var indexModel = new CreateIndexModel<Book>(indexKeysDefinition, indexOptions);

            _bookService._books.Indexes.CreateOneAsync(indexModel);

            return Ok();
        }
        [HttpGet]
        public ActionResult<List<Book>> Get()
        {
            var books = _bookService.Get();
            return books;
        }

        [HttpGet]
        [Route("identifier")]
        public ActionResult<Msg> Get(string identifier)
        {
            Msg msg=new Msg();
            try
            {
                //检查输入的ISBN
                string cleanValue = Regex.Replace(identifier, @"[^\dX]", "");

                if (cleanValue.Length == 10)
                {
                    // Convert ISBN-10 to ISBN-13
                    identifier = Book.ConvertIsbn10ToIsbn13(cleanValue);
                }
                if (cleanValue.Length == 13)
                {
                    if (cleanValue.Substring(12, 1) != Book.CalculateIsbn13Checksum(cleanValue.Substring(0, 12)).ToString())
                    {
                        msg.Code = 1;
                        msg.Message = $"isbn{identifier}校验和不对";
                    }
                }

                var book = _bookService.Get(identifier);
                msg.Code = 0;
                msg.Data = book;
            }
            catch (Exception ex) {
                msg.Code = 101;
                msg.Message = ex.Message;
            }

            return Ok(msg);
        }

        [HttpPost]
        public ActionResult<Msg> Insert(Book book)
        {
            Msg msg = new Msg();

            //model校验
            //检查输入的ISBN，去掉-，把10位的转换位13位
            Tuple<bool,string> tuple= Book.validIsbn(book.Identifier);
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
            Tuple<bool,string> tuple1=Book.validYear(book.Date);
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
            Tuple<bool, decimal> tuple2 = Book.validPrice(book.Price.ToString());
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

        [HttpPut]
        public ActionResult<Msg> Update(string identifier, Book book)
        {
            Msg msg = new Msg();
            //模型校验
            if (book.Identifier != null) {
                //检查输入的ISBN，去掉-，把10位的转换位13位
                Tuple<bool, string> tuple = Book.validIsbn(book.Identifier);
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

            if (book.Date != null)
            {
                Tuple<bool, string> tuple1 = Book.validYear(book.Date);
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

            if (book.Price != null) {
                //检查价格
                Tuple<bool, decimal> tuple2 = Book.validPrice(book.Price.ToString());
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
    }
}
