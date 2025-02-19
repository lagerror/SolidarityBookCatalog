using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReaderController : ControllerBase
    {
        private readonly IMongoCollection<Reader> _readers;
        private readonly IConfiguration _configuration;
        private readonly string _cryptKey;
        private readonly string _cryptIv;
        public ReaderController(IMongoClient client,IConfiguration configuration) 
        {
            var database = client.GetDatabase("BookReShare");
            _readers = database.GetCollection<Reader>("reader");
            _configuration = configuration;
            _cryptKey = _configuration["Crypt:key"];
            _cryptIv = _configuration["Crypt:iv"];
        }
       
        //功能测试
        [HttpGet]
        [Route("fun")]
        public async Task<IActionResult> Fun(string act,string pars)
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
        [Route("insert")]
        public async Task<IActionResult> insert([FromBody] CreateReaderDto dto)
        {
            Msg msg = new Msg();
            //校验openId的解密
            if (!EncryptOpenId(dto.OpenId).Item1)
            {
                return BadRequest("OpenId解密失败");
            }
            var reader = new Reader
            {
                OpenId = dto.OpenId,
                Name = dto.Name,
                StudentId = dto.ReaderNo,
                Phone = dto.Phone,
                BirthYear = dto.BirthYear,
                Type = dto.Type,
                Library = dto.Library,
                AppId=dto.AppId,
                Password = dto.Password
            };
           
            try
            {
                await _readers.InsertOneAsync(reader);
                msg.Code = 0;
                return Ok(msg);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                msg.Code = 1;
                msg.Message = $"读者证号{reader.OpenId}或者学号{reader.StudentId}或手机号{reader.Phone}已存在";
                return BadRequest(msg);
            }
            catch (Exception ex) {
                msg.Code = 2;
                msg.Message = ex.Message;
                return BadRequest(msg);
            }
        }

        // PUT api/readers/5
        [HttpPost]
        [Authorize(Policy = "ManagerOrReader")]
        [Route("update")]
        public async Task<IActionResult> update(string openid, [FromBody] Reader updateReader)
        {
           
            Msg msg = new Msg();
            
            var updateDefinitionBuilder = Builders<Reader>.Update;
            var updates = new List<UpdateDefinition<Reader>>();
            try
            {
                // 根据字段是否为 null 构建更新定义，应该使用反射的循环
                var properties = updateReader.GetType().GetProperties();
                foreach (var property in properties)
                {
                    //不能更改的字段
                    if (property.Name == "OpenId" || property.Name == "Id")
                    {
                        continue;
                    }
                    //更新非空字段
                    var value = property.GetValue(updateReader, null);
                    if (property.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)value))
                    {
                        updates.Add(updateDefinitionBuilder.Set(property.Name, value));
                    }
                    else if (property.PropertyType != typeof(string) && value != null)
                    {
                        updates.Add(updateDefinitionBuilder.Set(property.Name, value));
                    }
                }
                // 将所有更新定义组合成一个更新定义
                var combinedUpdateDefinition = updateDefinitionBuilder.Combine(updates);

                // 如果没有需要更新的字段，直接返回
                if (updates.Count > 0)
                {
                    var updateDefinition = updateDefinitionBuilder.Combine(updates);
                    var filter = Builders<Reader>.Filter.Eq(b => b.OpenId, openid);
                    var result =await _readers.UpdateOneAsync(filter, updateDefinition);

                    if (result.IsAcknowledged && result.ModifiedCount>0 )
                    {
                        msg.Code = 0;
                        msg.Message = $"update:成功更新{openid}";
                    }
                    else
                    {
                        msg.Code = 1;
                        msg.Message = $"update:更新失败 {result.ModifiedCount.ToString()}";
                    }
                }
                else
                {
                    msg.Code = 2;
                    msg.Message = $"update: 没有对应更新字段";
                }
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = ex.Message;
            }

            return Ok(msg);
        }

        // DELETE api/readers/5
        [HttpGet]
        [Authorize(Policy = "AdminOrManager")]
        [Route("delete")]
        public async Task<IActionResult> Delete(string openid)
        {
            var msg = new Msg();
            try
            {
                var result = await _readers.DeleteOneAsync(r => r.OpenId == openid);

                if (result.DeletedCount == 1)
                {
                    msg.Code = 0;
                    msg.Message = "删除成功";
                }
                else
                {
                    msg.Code = 1;
                    msg.Message = "删除失败";
                }
            }
            catch (Exception ex) { 
                msg.Code = 100;
                msg.Message = ex.Message;   
            }
            return Ok(msg);
        }


        //以下是自定义函数
        //加解密openid
        private Tuple<bool,string> EncryptOpenId(string openid)
        {
            Tuple<bool, string> result = new Tuple<bool, string>(false, "");
            string cryptOpenId = Tools.DecryptStringFromBytes_Aes(openid, _cryptKey, _cryptIv);
            if (cryptOpenId != null)
            {
                result = new Tuple<bool, string>(true, cryptOpenId);
            }
            return result;
        }
        private Tuple<bool, string> DecryptOpenId(string cryptOpenid)
        {
            Tuple<bool, string> result = new Tuple<bool, string>(false, "");
            string openId = Tools.EncryptStringToBytes_Aes(cryptOpenid, _cryptKey, _cryptIv);
            if (openId != null)
            {
                result = new Tuple<bool, string>(true, openId);
            }
            return result;
        }
    }
}
