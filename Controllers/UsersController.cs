using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        // GET: api/<UsersController>
        private  UserService _userService;
        public UsersController( UserService userService)
        { 
            _userService = userService;
        }
        [HttpGet]
        public Msg  Get()
        {
            //var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(user => user.Username);
            //var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
            //var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);

            //_userService._users.Indexes.CreateOneAsync(indexModel);
           // _userService.insert(new Models.User());
            Msg msg= _userService.GroupLibAdd();
            return msg;
        }
    }
}
