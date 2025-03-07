using Microsoft.Extensions.Caching.Memory;
using SolidarityBookCatalog.Models;
using System.Text;
using System.Text.Json;

namespace SolidarityBookCatalog.Services
{
    public interface IBDIot
    {
        public Task<bool> sendTopicMsgAsync(string topic, Msg msg);
    }

    public class BDIot : IBDIot
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BDIot> _logger;
        private readonly IConfiguration _configuration;

        private readonly string _BaseUrl;
        private readonly string _UserName;
        private readonly string _Password;

        public BDIot(IConfiguration configuration, IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<BDIot> logger)
        {
            _configuration = configuration;
            _BaseUrl = _configuration["BDIot:BaseUrl"];
            _UserName = _configuration["BDIot:UserName"];
            _Password = _configuration["BDIot:Password"];

            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> sendTopicMsgAsync(string topic, Msg msg)
        {
            try
            {
                string token = GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to retrieve BDIot token.");
                    return false;
                }

                // 使用 token 发送消息的逻辑
                using (var client = _httpClientFactory.CreateClient())
                {
                    //"Content-Type", "application/octet-stream"
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_BaseUrl}/pub?topic={topic}&qos=0")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(msg), Encoding.UTF8, "application/octet-stream")
                    };
                    request.Headers.Add("token", token);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        if (jsonResponse.GetProperty("message").GetString() == "ok")
                        {
                            return true;
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to send message to BDIot server. Status code: {StatusCode}", response.StatusCode);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while sending message to BDIot server.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending message to BDIot server.");
            }

            return false;
        }

        private string GetToken()
        {
            if (!_cache.TryGetValue("iotToken", out string token))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var requestContent = new StringContent(JsonSerializer.Serialize(new
                    {
                        username = _UserName,
                        password = _Password,
                        tokenLifeSpanInSeconds = 7200
                    }), Encoding.UTF8, "application/json");

                    var response = client.PostAsync($"{_BaseUrl}/auth", requestContent).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        token = jsonResponse.GetProperty("token").GetString();

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromSeconds(7000));

                        _cache.Set("iotToken", token, cacheEntryOptions);
                    }
                    else
                    {
                        _logger.LogError("Failed to retrieve BDIot token from server. Status code: {StatusCode}", response.StatusCode);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP request error while retrieving BDIot token.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while retrieving BDIot token.");
                }
            }

            return token;
        }
    }
}
