using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Security.Cryptography;
using System.Web;
using MongoDB.Bson.IO;
using SolidarityBookCatalog.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using System.Collections.Generic;

namespace SolidarityBookCatalog.Services
{
    public interface IWeChatService
    {
        //获取访问令牌
        Task<string> GetTokenAsync();
        //通过code获取openid
        Task<string> GetOpenIdByCodeAsync(string code);
        Task<string> GetJsapiTicketAsync();
        Task<string> GetJsapiSignature(string cryptOpenid,string url);
        Task<WeChatUserInfoResponse> GetUserInfoAsync(string accessToken,string openid);
        public Task<bool> SendTemplateMessageAsync(string act, string openid, string redirectUrl, object data, string templateId = "Y3ry7I_3kr1o-q7_biGMWNuys1M3pQva_H6m89yz88Y");
    }

    public class WeChatService : IWeChatService, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeChatService> _logger;
        private readonly IConfiguration _configuration; 
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // 确保线程安全

        // 微信 Token 的配置参数（从 appsettings.json 读取）
        private readonly string _appId;
        private readonly string _appSecret;

        public WeChatService(IHttpClientFactory httpClientFactory,IMemoryCache cache,IConfiguration configuration,ILogger<WeChatService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
            _configuration  = configuration;    

            // 从配置中读取微信参数
            _appId = configuration["WeChat:AppId"] ?? throw new ArgumentNullException("WeChat:AppId");
            _appSecret = configuration["WeChat:AppSecret"] ?? throw new ArgumentNullException("WeChat:AppSecret");
        }

        //发送留言提醒，只有在用户关注公众号后才能发送，文档地址：https://developers.weixin.qq.com/doc/offiaccount/Message_Management/Template_Message_Interface.html
        //文字两行，一行20个字，最多5行
        public async Task<bool> SendTemplateMessageAsync(string act, string openid, string redirectUrl, object odata, string templateId = "X7AKgVggnJgst45K9AddUthvpr4428ZaaXtk-cNaEOw")
        {
            string accessToken= await GetTokenAsync();
            string url = $"https://api.weixin.qq.com/cgi-bin/message/template/send?access_token={accessToken}";
            switch (act)
            {
                //申请
                case "apply":
                    templateId = "DidJuByMbu4ocgWM5uLdu3Bb_J9NEzExdSuAN2lQDT4";
                   
                    break;
                //借书
                case "loan":
                    templateId = "jlokyJVUwOsXzio09VPt4TSdGELfgBxfOOF9SYIpFY0";
                    
                    /*{ { first.DATA} }
                    读者姓名：{ { keyword1.DATA} }
                    图书题名：{ { keyword2.DATA} }
                    图书条码：{ { keyword3.DATA} }
                    借书日期：{ { keyword4.DATA} }
                    应还日期：{ { keyword5.DATA} }
                    { { remark.DATA} }
                    */

                    break;
                //还书
                case "return":
                    templateId = "ibRn0TdibQpntp68rr-pqAGntvIY_Nmu9PRGO3CzoT8";
                    /*{{first.DATA}}
                        读者姓名：{{keyword1.DATA}}
                        图书题名：{{keyword2.DATA}}
                        图书条码：{{keyword3.DATA}}
                        应还日期：{{keyword4.DATA}}
                        实还日期：{{keyword5.DATA}}
                        {{remark.DATA}}
                     */
                    break;
                //留言
                case "notice":
                    templateId = "X7AKgVggnJgst45K9AddUthvpr4428ZaaXtk-cNaEOw";
                    /*{{first.DATA}}
                        学校：{{keyword1.DATA}}
                        通知人：{{keyword2.DATA}}
                        时间：{{keyword3.DATA}}
                        通知内容：{{keyword4.DATA}}
                        {{remark.DATA}}*/
                    break;
            }

            using (HttpClient sendClient = new HttpClient())
            {
                var messageData = new
                {
                    touser = openid,
                    template_id = templateId,
                    url = redirectUrl,
                    data = odata
                };

                StringContent content = new StringContent(JsonSerializer.Serialize(messageData), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await sendClient.PostAsync(url, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                return response.IsSuccessStatusCode;
            }
        }

        //实现通过code获取openid
        public async Task<string> GetOpenIdByCodeAsync(string code)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://api.weixin.qq.com/sns/oauth2/access_token?appid={_appId}&secret={_appSecret}&code={code}&grant_type=authorization_code");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            //Console.WriteLine($"openid:{json}");
            var jsonObject = JsonSerializer.Deserialize<JsonElement>(json);
            if (jsonObject.TryGetProperty("openid", out JsonElement openId))
            {
                return openId.GetString();
            }
            else
            {
                return null;
            }
        }
        
        //实现获取访问令牌
        public async Task<string> GetTokenAsync()
        {
            // 尝试从缓存中获取 Token
            if (_cache.TryGetValue("Access_Token", out string? token))
            {
                Console.WriteLine($"缓存中获取了Access_Token:{token}");
                return token;
            }

            // 使用信号量确保只有一个线程能执行刷新操作
            //await _semaphore.WaitAsync();
            try
            {
                // 再次检查缓存，防止其他线程已经刷新
                if (_cache.TryGetValue("Access_Token", out token))
                {
                    Console.WriteLine($"线程进入时缓存中获取了Access_Token:{token}");
                    return token;
                }

                // 从微信 API 获取新 Token
                var newToken = await FetchNewTokenAsync();
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    // 设置缓存过期时间（微信 Token 有效期通常为 7200 秒）
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(7000) // 提前 20 秒过期
                };

                _cache.Set("Access_Token", newToken, cacheOptions);
                Console.WriteLine($"获取了新Access_Token:{newToken}");
                return newToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch WeChat token");
                throw;
            }
            finally
            {
                //_semaphore.Release();
            }
        }

        //通过access-token换取jsapi-ticket并缓存
        //https://developers.weixin.qq.com/doc/offiaccount/OA_Web_Apps/JS-SDK.html#62
        public async Task<string> GetJsapiTicketAsync()
        {

            // 尝试从缓存中获取 Token
            if (_cache.TryGetValue("Jsapi_Token", out string? Jsapi_Token))
            {
                Console.WriteLine($"缓存中获取了Jsapi_Token:{Jsapi_Token}");
                return Jsapi_Token;
            }

            // 使用信号量确保只有一个线程能执行刷新操作,导致死锁，没有超时退出机制
            //await _semaphore.WaitAsync(1000);
            try
            {
                // 再次检查缓存，防止其他线程已经刷新
                if (_cache.TryGetValue("Jsapi_Token", out Jsapi_Token))
                {
                    Console.WriteLine($"进入线程时缓存中获取了Jsapi_Token:{Jsapi_Token}");
                    return Jsapi_Token;
                }

                // 从微信 API 获取新 Token
                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync($"https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={GetTokenAsync().Result}&type=jsapi");
                response.EnsureSuccessStatusCode();

                var jsonStr = await response.Content.ReadAsStringAsync();
                var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonStr);
                
                if (jsonObject.TryGetProperty("ticket", out JsonElement Jsapi_Token_Element))
                {
                    Jsapi_Token = Jsapi_Token_Element.GetString();
                }
               

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    // 设置缓存过期时间（微信 Token 有效期通常为 7200 秒）
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(7000) // 提前 20 秒过期
                };

                _cache.Set("Jsapi_Token", Jsapi_Token, cacheOptions);
                Console.WriteLine($"获取了新的Jsapi_Token:{Jsapi_Token}");
                return Jsapi_Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Jsapi_Token");
                throw;
            }
            finally
            {
                //_semaphore.Release();
            }
        }
        
        //为jsapi提供签名
        //https://developers.weixin.qq.com/doc/offiaccount/OA_Web_Apps/JS-SDK.html#62
        public async Task<string> GetJsapiSignature(string cryptOpenid,string url= "https://reader.yangtzeu.edu.cn/wechat/scan")
        {
            string retStr = null;
            try
            {
                string noncestr = Guid.NewGuid().ToString("N");
                string jsapi_ticket = await GetJsapiTicketAsync();
                string timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                SortedDictionary<string, string> pars = new SortedDictionary<string, string>();
                pars.Add("noncestr", noncestr);
                pars.Add("jsapi_ticket", jsapi_ticket);
                pars.Add("timestamp", timestamp);
                pars.Add("url", url);
                // 拼接成URL键值对格式
                var string1 = string.Join("&", pars.Select(p => $"{p.Key}={p.Value}"));
                var signature = "";
                // 使用SHA-1加密
                using (var sha1 = SHA1.Create())
                {
                    var sha1Bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(string1));
                    signature = BitConverter.ToString(sha1Bytes).Replace("-", "").ToLower();
                }
                retStr = $"{_appId}@{timestamp}@{noncestr}@{signature}@{cryptOpenid}";
                Console.WriteLine($"signature:{retStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return retStr;

        }
        public async Task<WeChatUserInfoResponse> GetUserInfoAsync(string accessToken,string openid)
        {
            //为网页访问token

            var url = $"https://api.weixin.qq.com/sns/userinfo?access_token={accessToken}&openid={openid}&lang=zh_CN";
            var userInfoResponse= new WeChatUserInfoResponse();
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GetUserInfoAsync:{json}");
                userInfoResponse = JsonSerializer.Deserialize<WeChatUserInfoResponse>(json);
                if(userInfoResponse?.Openid == null)
                {
                    _logger.LogError("Failed to fetch WeChat user info: {0}", json);
                    return null;
                }
            }
            return userInfoResponse;
        }

        //获取全局唯一接口调用凭据
        private async Task<string> FetchNewTokenAsync()
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={_appId}&secret={_appSecret}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<WeChatTokenResponse>(json);

            if (tokenResponse?.AccessToken == null)
            {
                _logger.LogError("Failed to fetch WeChat token: {0}", json);
                return null;
            }

            return tokenResponse.AccessToken;
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        public Task<WeChatUserInfoResponse> GetUserInfoAsync(string openid)
        {
            throw new NotImplementedException();
        }
    }
    //微信全局token
    public class WeChatTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
   
}
