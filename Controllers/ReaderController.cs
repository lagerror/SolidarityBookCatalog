using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReaderController : ControllerBase
    {
        private readonly IMongoCollection<Reader> _readers;
        public ReaderController(IMongoClient client) 
        {
            var database = client.GetDatabase("BookReShare");
            _readers = database.GetCollection<Reader>("reader");
        }
       
        //功能测试
        [HttpGet]
        public async Task<IActionResult> Fun(string act)
        {
            Msg msg = new Msg();
            switch (act)
            { 
                case "index":
                    try
                    {
                        //openid
                        var indexKeysDefinition = Builders<Reader>.IndexKeys.Ascending(Reader => Reader.OpenId);
                        var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
                        var indexModel = new CreateIndexModel<Reader>(indexKeysDefinition, indexOptions);
                        var result = await _readers.Indexes.CreateOneAsync(indexModel);
                        //手机
                        indexKeysDefinition = Builders<Reader>.IndexKeys.Ascending(Reader => Reader.Phone);
                        indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
                        indexModel = new CreateIndexModel<Reader>(indexKeysDefinition, indexOptions);
                        result += await _readers.Indexes.CreateOneAsync(indexModel);

                        msg.Code = 0;
                        msg.Message = result;

                        return Ok(msg);
                        
                    }
                    catch (Exception ex) { 
                        msg.Code = 1;
                        msg.Message = ex.Message;
                        return BadRequest(msg);
                        
                    }

                    break;
            }
            return Ok(msg);
        }

        // GET api/<ReaderController>/5
        [HttpGet("{openid}")]
        public async Task<ActionResult<Reader>> Get(string openid)
        {
            var reader = await _readers.Find(r => r.OpenId == openid).FirstOrDefaultAsync();
            if (reader == null) return NotFound();
            return Ok(reader);
        }

        // POST api/<ReaderController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateReaderDto dto)
        {
            var reader = new Reader
            {
                OpenId = dto.OpenId,
                Name = dto.Name,
                StudentId = dto.StudentId,
                Phone = dto.Phone,
                Age = dto.Age,
                Type = dto.Type,
                Library = dto.Library,
                Area=dto.Area,
                Password = dto.Password
            };
            Msg msg = new Msg();
            try
            {
                await _readers.InsertOneAsync(reader);
                msg.Code = 0;
                return Ok(msg);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                msg.Code = 1;
                msg.Message = $"学号{reader.StudentId}或手机号{reader.Phone}已存在";
                return BadRequest(msg);
            }
            catch (Exception ex) {
                msg.Code = 2;
                msg.Message = ex.Message;
                return BadRequest(msg);
            }
        }

        // PUT api/readers/5
        [HttpPost("{openid}")]
        [Route("update")]
        public async Task<IActionResult> update(string openid, [FromBody] Reader reader)
        {
            var msg = new Msg();
            if (openid != reader.OpenId)
            {
                msg.Code = 1;
                msg.Message = "没找到对应记录";
                return BadRequest(msg);
            }

            var result = await _readers.ReplaceOneAsync(r => r.OpenId == openid, reader);
            if (result.MatchedCount == 0)
            {
                msg.Code = 2;
                msg.Message = "没找到对应记录";
                return BadRequest(msg);
            }
            msg.Code = 0;
            return Ok(msg);
        }

        // DELETE api/readers/5
        [HttpPost("{openid}")]
        public async Task<IActionResult> Delete(string openid)
        {
            var msg = new Msg();
            var result = await _readers.DeleteOneAsync(r => r.OpenId == openid);
            if (result.DeletedCount == 0)
            {
                msg.Code= 1;
                msg.Message = "没有删除成功";
                return BadRequest(msg);
            }
            msg.Code = 0;
            return Ok(msg);
        }
    }
}
