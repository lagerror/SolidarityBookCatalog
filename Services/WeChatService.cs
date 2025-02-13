using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Security.Cryptography;

namespace SolidarityBookCatalog.Services
{
    public interface IWeChatTokenService
    {
        //获取访问令牌
        Task<string> GetTokenAsync();
        //通过code获取openid
        Task<string> GetOpenIdByCodeAsync(string code);
        Task<string> GetJsapiTicketAsync();
        Task<string> GetJsapiSignature(string cryptOpenid);
    }

    public class WeChatTokenService : IWeChatTokenService, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeChatTokenService> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // 确保线程安全

        // 微信 Token 的配置参数（从 appsettings.json 读取）
        private readonly string _appId;
        private readonly string _appSecret;

        public WeChatTokenService(IHttpClientFactory httpClientFactory,IMemoryCache cache,IConfiguration configuration,ILogger<WeChatTokenService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;

            // 从配置中读取微信参数
            _appId = configuration["WeChat:AppId"] ?? throw new ArgumentNullException("WeChat:AppId");
            _appSecret = configuration["WeChat:AppSecret"] ?? throw new ArgumentNullException("WeChat:AppSecret");
        }
        
        //实现通过code获取openid
        public async Task<string> GetOpenIdByCodeAsync(string code)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://api.weixin.qq.com/sns/oauth2/access_token?appid={_appId}&secret={_appSecret}&code={code}&grant_type=authorization_code");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"openid:{json}");
            var jsonObject = JsonSerializer.Deserialize<JsonElement>(json);
            if (jsonObject.TryGetProperty("openId", out JsonElement openId))
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
                return token;
            }

            // 使用信号量确保只有一个线程能执行刷新操作
            await _semaphore.WaitAsync();
            try
            {
                // 再次检查缓存，防止其他线程已经刷新
                if (_cache.TryGetValue("Access_Token", out token))
                {
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
                Console.WriteLine($"Access_Token:{newToken}");
                return newToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch WeChat token");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        //通过access-token换取jsapi-ticket并缓存
        //https://developers.weixin.qq.com/doc/offiaccount/OA_Web_Apps/JS-SDK.html#62
        public async Task<string> GetJsapiTicketAsync()
        {

            // 尝试从缓存中获取 Token
            if (_cache.TryGetValue("Jsapi_Token", out string? Jsapi_Token))
            {
                return Jsapi_Token;
            }

            // 使用信号量确保只有一个线程能执行刷新操作,导致死锁，没有超时退出机制
            await _semaphore.WaitAsync(1000);
            try
            {
                // 再次检查缓存，防止其他线程已经刷新
                if (_cache.TryGetValue("Jsapi_Token", out Jsapi_Token))
                {
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
                Console.WriteLine($"Jsapi_Token:{Jsapi_Token}");
                return Jsapi_Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Jsapi_Token");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        //为jsapi提供签名
        //https://developers.weixin.qq.com/doc/offiaccount/OA_Web_Apps/JS-SDK.html#62
        public async Task<string> GetJsapiSignature(string cryptOpenid)
        {
            string retStr = null;
            try
            {
                string noncestr = Guid.NewGuid().ToString("N");
                string jsapi_ticket = await GetJsapiTicketAsync();
                string timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                string url = "https://reader.yangtzeu.edu.cn/solidarity/wechat.html";
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
                retStr = signature + "@" + cryptOpenid;
                Console.WriteLine($"signature:{retStr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return retStr;

        }

       
        private async Task<string> FetchNewTokenAsync()
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={_appId}&secret={_appSecret}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<WeChatTokenResponse>(json);

            if (tokenResponse?.AccessToken == null)
            {
                throw new InvalidOperationException("Failed to get WeChat token");
            }

            return tokenResponse.AccessToken;
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

    }

    public class WeChatTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
