using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Web;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReaderController : ControllerBase
    {
        private readonly IMongoCollection<Reader> _readers;
        private readonly ReaderService _readerService;
        private readonly ToolService _toolService;  
        private readonly IConfiguration _configuration;
        private readonly string _cryptKey;
        private readonly string _cryptIv;
        public ReaderController(IMongoClient client,IConfiguration configuration,ReaderService readerService,ToolService toolService) 
        {
            var database = client.GetDatabase("BookReShare");
            _readers = database.GetCollection<Reader>("reader");
            _readerService = readerService;
            _configuration = configuration;
            _toolService = toolService;
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
        [HttpGet]
        [Route("getByOpenId")]
        public async Task<IActionResult> Get(string openId)
        {
            //rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9
            //rfWTm26wJZnpQ%2BS0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9
            //必须解码
            Msg msg = new Msg();
            try
            {
                //校验openId的解密
                openId = HttpUtility.UrlDecode(openId);
                var ret = _toolService.DeCryptOpenId(openId);
                if (!ret.Item1)
                {
                    msg.Code = 3;
                    msg.Message = $"OpenId解密失败";
                    return Ok(msg);
                }
                openId = ret.Item2;

                var reader = await _readers.Find(r => r.OpenId == openId).FirstOrDefaultAsync();
                if (reader == null)
                {
                    msg.Code = 1;
                    msg.Message = "未找到";
                    return Ok(msg);
                }
                else
                {
                    var readerDto = new
                    {
                        Name = reader.Name,
                        ReaderNo = reader.ReaderNo,
                        Type = reader.Type,
                        Phone = reader.Phone,
                        BirthYear = reader.BirthYear,
                        Library = reader.Library,
                        AppId = reader.AppId,
                        IsValid= reader.IsValid
                    };
                    msg.Code = 0;
                    msg.Data = readerDto;
                    return Ok(msg);
                }
            }
            catch (Exception ex) { 
                msg.Code = 100;
                msg.Message = ex.Message;
            }
                return Ok(msg);
        }

        // POST api/<ReaderController>
        [HttpPost]
        [Route("insert")]
        public async Task<IActionResult> insert([FromBody] CreateReaderDto dto)
        {
            Msg msg = new Msg();
            Console.WriteLine(dto.OpenId);
            dto.OpenId = HttpUtility.UrlDecode(dto.OpenId);
            //校验openId的解密
            var ret = _toolService.DeCryptOpenId(dto.OpenId);
            if (!ret.Item1)
            {
                msg.Code = 3;
                msg.Message=$"OpenId解密失败";
                return Ok(msg);
            }
            dto.OpenId = ret.Item2;
            var reader = new Reader
            {
                OpenId = dto.OpenId,
                Name = dto.Name,
                ReaderNo = dto.ReaderNo,
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
                msg.Message = $"插入成功:{dto.Name}";
                return Ok(msg);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                msg.Code = 1;
                msg.Message = $"微信openId或者学号{reader.ReaderNo}或手机号{reader.Phone}已存在";
                return Ok(msg);
            }
            catch (Exception ex) {
                msg.Code = 2;
                msg.Message = ex.Message;
                return Ok(msg);
            }
        }

        [HttpPost]
        [Route("search")]
        public async Task<IActionResult> Search(SearchQueryList list, int rows = 10, int page = 1)
        {
           Msg msg = new Msg();
           msg=await _readerService.SearchAsync(list, rows, page);
           return Ok(msg);
        }

        // PUT api/readers/5
        [HttpPost]
        [Authorize(Policy = "ManagerOrReader")]
        [Route("update")]
        public async Task<IActionResult> update(string openId, [FromBody] Reader updateReader)
        {
            Msg msg = new Msg();
            //校验openId的解密
            var ret = _toolService.DeCryptOpenId(openId);
            if (!ret.Item1)
            {
                msg.Code = 3;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            openId=ret.Item2;

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
                //var combinedUpdateDefinition = updateDefinitionBuilder.Combine(updates);

                // 如果没有需要更新的字段，直接返回
                if (updates.Count > 0)
                {
                    var updateDefinition = updateDefinitionBuilder.Combine(updates);
                    var filter = Builders<Reader>.Filter.Eq(b => b.OpenId, openId);
                    var result =await _readers.UpdateOneAsync(filter, updateDefinition);

                    if (result.IsAcknowledged && result.ModifiedCount>0 )
                    {
                        msg.Code = 0;
                        msg.Message = $"update:成功更新{openId}";
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

        //管理人员在读者验证后将有效位置位
        [HttpPost]
        [Route("Audit")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> Audit(string id,bool flag)
        {
            Msg msg = new Msg();
            var name = HttpContext.User.Identity?.Name;
            var updateDefinitionBuilder = Builders<Reader>.Update.Set(x=>x.IsValid,!flag)
                        .Set(x=>x.Auditor,name)
                        .Set(x=>x.AuditDate,DateTime.UtcNow);

            var filter = Builders<Reader>.Filter.Eq(x => x.Id, id);
            var result = await _readers.UpdateOneAsync(filter, updateDefinitionBuilder);
            if (result.IsAcknowledged && result.ModifiedCount > 0)
            {
                msg.Code = 0;
                msg.Message = $"{id}有效性修改成功";
            }
            else
            {
                msg.Code = 1;
                msg.Message = $"{id}有效性修改失败"; 
            }

            return Ok(msg);
        }


        // DELETE api/readers/5
        [HttpGet]
        [Authorize(Policy = "AdminOrManager")]
        [Route("delete")]
        public async Task<IActionResult> Delete(string openId)
        {
            var msg = new Msg();
            //校验openId的解密
            var ret = _toolService.DeCryptOpenId(openId);
            if (!ret.Item1)
            {
                msg.Code = 3;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            try
            {
                var result = await _readers.DeleteOneAsync(r => r.OpenId == openId);

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
    }
}
