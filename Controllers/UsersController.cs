﻿using Microsoft.AspNetCore.Authorization;
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
using Elastic.Clients.Elasticsearch;
using Serilog;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using Elastic.Clients.Elasticsearch.Security;
using User = SolidarityBookCatalog.Models.User;
using Microsoft.VisualBasic;

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
        private ILogger<UsersController> _logger;
        public UsersController( UserService userService,IConfiguration configuration,ILogger<UsersController> logger)
        { 
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
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
                case "index":
                    var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(user => user.Username);
                    var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
                    var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);

                    _userService._users.Indexes.CreateOneAsync(indexModel);
                    break;

            }
            return Ok(msg);
        }


        [HttpGet]
        public Msg  Get()
        {
            Msg msg= _userService.GroupLibAdd();
            return msg;
        }
        [HttpPost]
        [Route("search")]
        [Authorize(Policy = "AdminOrManager")]
        public async Task<IActionResult> SearchAsync(SearchQueryList list, int rows = 10, int page = 1)
        {
            Msg msg = new Msg();

            var filterBuilder = Builders<User>.Filter;
            var filters = new List<FilterDefinition<User>>();
            foreach (var item in list.List)
            {
                if (item.Field == "username")
                {
                    filters.Add(filterBuilder.Eq(x => x.Username, item.Keyword));
                }
                else if(item.Field == "province")
                {
                    filters.Add(filterBuilder.Eq(x => x.Province, item.Keyword));
                }
                else if (item.Field == "city")
                {
                    filters.Add(filterBuilder.Eq(x => x.City, item.Keyword));
                }
                else if (item.Field == "name")
                {
                    filters.Add(filterBuilder.Eq(x => x.Name, item.Keyword));
                }
                else if (item.Field == "appId")
                {
                    filters.Add(filterBuilder.Regex(d => d.AppId, new BsonRegularExpression(item.Keyword, "i")));
                }
            }
         

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : FilterDefinition<User>.Empty;

            // 执行查询
              var total = await _userService._users.CountDocumentsAsync(finalFilter);
            var results = await _userService._users.Find(finalFilter)
                .Skip((page - 1) * rows)
                .Limit(rows)
                .ToListAsync();
            //使用linq查询返回指定字段
            var data = results.Select(x => new
            {
                x.Id,
                x.Username,
                x.Province,
                x.City,
                x.AppId,
                x.Name,
                x.Chmod,
                x.MobilePhone
            }).ToList();
            //使用匿名类返回数据
            msg.Code = 0;
            msg.Message = "查询成功";
            msg.Data = new
            {
                total = total,
                rows = data
            };
            return Ok(msg);
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
            // 获取当前用户的ClaimsPrincipal
            var user = HttpContext.User;

            // 检查用户是否已经认证
            if (user.Identity.IsAuthenticated)
            {
                // 获取用户名
                var userName = user.Identity.Name;
                // 获取特定的Claim
                var idClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                var mobilePhone = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.MobilePhone)?.Value;
                msg.Code = 0;
                msg.Data = new
                {
                    userName = userName,
                    idClaim = idClaim,
                    roleClaim = roleClaim,
                    mobilePhoneClaim=mobilePhone
                };
            }

            return msg;
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserDTOLogin login, string source)
        {
            Msg msg = new Msg();

            var ret = new Dictionary<string, string>
            {
                { "Id", "" },
                { "UserName", "" },
                { "Role", "" },
                { "source", source },
                { "MobilePhone", "" },
                { "OpenId",""}
            };

            try
            {
                switch (source)
                {
                    case "admin":
                        var filter = Builders<User>.Filter.And(
                            Builders<User>.Filter.Eq("username", login.username),
                            Builders<User>.Filter.Eq("password", login.password)
                        );
                        var user = _userService._users.Find(filter).FirstOrDefault();
                        if (user == null)
                        {
                            msg.Code = 1;
                            msg.Message = "密码不匹配";
                            return Ok(msg);
                        }
                        else
                        {
                            ret["Id"] = user.Id;
                            ret["UserName"] = user.Username;
                            ret["Role"] = "admin";
                        }
                        break;
                    case "postman":
                    case "manager":
                        var filterManager = Builders<Manager>.Filter.And(
                           Builders<Manager>.Filter.Eq("username", login.username),
                           Builders<Manager>.Filter.Eq("password", login.password)
                       );
                        var manager =_userService._managers.Find(filterManager).FirstOrDefault();
                        if (manager == null)
                        {
                            msg.Code = 1;
                            msg.Message = "密码不匹配";
                            return Ok(msg);
                        }
                        else
                        {
                            ret["Id"] = manager.Id;
                            ret["UserName"] = manager.UserName;
                            ret["Role"] =source;
                        }
                        break;
                    
                    case "reader":
                        var readerManager = Builders<Manager>.Filter.And(
                          Builders<Manager>.Filter.Eq("username", login.username),
                          Builders<Manager>.Filter.Eq("password", login.password)
                      );
                        var reader = _userService._managers.Find(readerManager).FirstOrDefault();
                        if (reader == null)
                        {
                            msg.Code = 1;
                            msg.Message = "密码不匹配";
                            return Ok(msg);
                        }
                        else
                        {
                            ret["Id"] = reader.Id;
                            ret["UserName"] = reader.UserName;
                            ret["Role"] = "reader";
                        }
                        break;
                }

                var rsa = RSA.Create();
                rsa.ImportFromPem(_userService.getTokenPrivateKey("pem"));
                var securityKey = new RsaSecurityKey(rsa);
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, ret["Id"].ToString()),
                    new Claim(ClaimTypes.Name, ret["UserName"].ToString()),
                    new Claim(ClaimTypes.MobilePhone, ret["MobilePhone"].ToString() ?? "没有录入手机号"),
                    new Claim(ClaimTypes.Role, ret["Role"].ToString()),
                    new Claim(ClaimTypes.Dsa, ret["source"].ToString()),
                    new Claim(ClaimTypes.Dns, ret["OpenId"].ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(300),
                    signingCredentials: credentials
                );

                var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
                msg.Code = 0;
                msg.Message = $"{login.username}成功获取管理token";
                msg.Data = new
                {
                    token = tokenStr,
                    expire = DateTime.UtcNow.AddMinutes(300),
                    name = ret["UserName"].ToString(),
                    Information = ret
                };
            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = $"{login.username}:{ex.Message}";
            }
            _logger.LogInformation(msg.ToString());
            return Ok(msg);
        }
    }
}
