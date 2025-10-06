using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Models.WKModels;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SolidarityBookCatalog.Services
{
    public class WkService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _ip;
        public WkService(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _ip = configuration["WeKnora:Ip"].ToString();
            _apiKey = configuration["WeKnora:AppKey"].ToString();
        }
        //获取知识库列表
        public async Task<KnowledgeBase> GetKnowledgeBaseAsync()
        { 
            var requestMessage=new HttpRequestMessage(HttpMethod.Get, $"{_ip}/api/v1/knowledge-bases");
            //requestMessage.Headers.Add( "Content-Type", "application/json" );
            requestMessage.Headers.Add("X-API-Key", _apiKey);
            var response=await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseJson=await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<KnowledgeBase>(responseJson);

            return result;
        }
        //获取知识库列表下知识（文件）
        public async Task<Knowledge> GetKnowledgeAsync(string id,int page_size=20,int page=1)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_ip}/api/v1/knowledge-bases/{id}/knowledge?page_size={page_size}&page={page}");
            //requestMessage.Headers.Add( "Content-Type", "application/json" );
            requestMessage.Headers.Add("X-API-Key", _apiKey);
            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Knowledge>(responseJson);
            return result;
        }

        public async Task<Msg> upFileAsync(string id,IFormFile file)
        {
            Msg msg=new Msg();
            try
            {
                // 1. 创建 MultipartFormDataContent，这是上传文件的标准格式
                using var content = new MultipartFormDataContent();

                // 2. 添加文件流到 content
                // 第一个参数 "file" 必须与目标API期望的表单字段名匹配
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                // 4. 构建请求
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_ip}/api/v1/knowledge-bases/{id}/knowledge/file");
                requestMessage.Headers.Add("X-API-Key", _apiKey);
                requestMessage.Content = content;

                // 5. 发送请求
                // 注意：HttpClient 会自动处理 Content-Type 的 'multipart/form-data' 和 boundary
                var response = await _httpClient.SendAsync(requestMessage);

                msg.Code = 0;
                msg.Message = "success";
                msg.Data = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) {
                msg.Code = 100;
                msg.Message = ex.Message;
            }
            return msg;

        }
    }
}
