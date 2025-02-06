using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using Microsoft.AspNetCore.DataProtection.KeyManagement;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        // GET: api/<UsersController>
        private  UserService _userService;
        private IConfiguration _configuration;
        public UsersController( UserService userService,IConfiguration configuration)
        { 
            _userService = userService;
            _configuration = configuration;
        }
        [HttpPost]
        [Route("fun")]
        public IActionResult fun(string act)
        {
            Msg msg = new Msg();
            switch (act)
            {
                case "insertUser":
                    var result = _userService.insert(new User());
                    break;
                case "token":
                    using (var client = new HttpClient())
                    {
                        string _token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoidXNlcjEiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJhZG1pbiIsImV4cCI6MTczODg1MzgzOCwiaXNzIjoiU29saWRhcml0eUJvb2tSZVNoYXJlLmlzcyIsImF1ZCI6IlNvbGlkYXJpdHlCb29rUmVTaGFyZS5hdWQifQ.Uwrn6DbdNmCOETJj5rb1PJkwGxvcWxIZlsM0AERJwBL_fnsfNxF5NWmUMtngne6ADHBql4j9JXbwtqzI3xamH_Txvuy6ba0B8oTABllrCRHI70X-52sjG5hqdkTccW40VWA3QuAZbG1VnZQ8dJLRdLYlEgau4wVI7U3c6j4Z13-8PMMAh6gQbITmXs4AHSF3-uov0RaOLZ6FHuNdbz451iy9FP3YH5T5vtSP4bwQv5wOS00Bm2isGgzU_VEEDEBjNXBtdex-wmqzZ2u-Kfl4p4B5EjKwS3Sseu-mEqwZmHmr4ob67N2rgftHHgJj9Q64rE25tav3_9NdwQasLR9i7w";
                        // 设置 BaseAddress
                      string url="http://localhost:5173/api/Users/test";

                        // 创建带有 JWT Token 的请求头
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");

                        //// 将数据序列化为 JSON 格式
                        var jsonContent = new StringContent(
                           "",
                            Encoding.UTF8,
                            "application/json"
                        );

                        // 发送 PUT 请求
                        var response = client.PostAsync(url,jsonContent).Result;

                        // 返回响应
                    }
                        break;

            }
            return Ok(msg);
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
        /// <summary>
        /// jwt token权限测试
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("test")]
        //[AllowAnonymous]
        [Authorize]
        public Msg Test() {
            Msg msg=new Msg();
            

            return msg;
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserDTOLogin login)
        {
            // 这里应该有实际的用户验证逻辑
           
           var user = new { Id = 1, Username = "user1", Role = "admin" };
            //获取 RSA 私钥
           var rsa = RSA.Create();
            //var privateKeyXml = _userService.getTokenPrivateKey();
            rsa.ImportFromPem(_userService.getTokenPrivateKey("pem"));
            //rsa.FromXmlString(privateKeyXml);
            var securityKey = new RsaSecurityKey(rsa);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
            
            // 创建 Claims
            var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };
            // 创建 Token
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(300), // 使用 UTC 时间
                signingCredentials: credentials
            );

            // 生成 Token 字符串
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { token = tokenStr });
        }
    }
}
