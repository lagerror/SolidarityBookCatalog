using MongoDB.Driver;
using SolidarityBookCatalog.Models.CDLModels;
using SolidarityBookCatalog.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SolidarityBookCatalog.Services
{
    //https://reader.yangtzeu.edu.cn/fisco/api/contract/collaboration/get?userAddress=0xb4bd4aa44700de4a2b6d21cfb782c208441bb1fd
    //https://reader.yangtzeu.edu.cn/fisco/api/block/getByNum/71
    /*https://reader.yangtzeu.edu.cn/fisco/api/contract/collaboration/set
     * 
     * {
    "userAddress":"0xb4bd4aa44700de4a2b6d21cfb782c208441bb1fd",
    "value":"674592077a0e8157d42befec_yangtzeu:674edeacff4de3a7a60ba2d8_9787115239792:67cd5572bb50e1f16929bbc5_300120"}
}   */
    public interface IFiscoService
    {
        Task<Msg> SetCollaborationAsync(string userAddress, string value);
    }

    public class FiscoService:IFiscoService
    {
        private readonly IMongoCollection<IsdlLoanWork> _IsdlLoanWork;
        private readonly IMongoCollection<Biblios> _Biblios;
        private readonly IMongoCollection<Reader> _Reader;
        private readonly IMongoCollection<User> _User;
        private readonly ILogger<FiscoService> _logger;
        private readonly IMongoDatabase database;
        private readonly ToolService _toolService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _userAddress;
        
        public FiscoService(IMongoClient client,HttpClient hc,IConfiguration configuration) {
            database = client.GetDatabase("BookReShare");
            _IsdlLoanWork = database.GetCollection<IsdlLoanWork>("isdlLoanWork");
            _Biblios = database.GetCollection<Biblios>("biblios");
            _Reader = database.GetCollection<Reader>("reader");
            _User=database.GetCollection<User>("user");
            _httpClient = hc;
            _httpClient.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));
            _configuration = configuration;
            _baseUrl = _configuration["Fisco:BaseUrl"].ToString();
            _userAddress = _configuration["Fisco:UserAddress"].ToString();
        }

        public async Task<Msg> SetCollaborationAsync(string userAddress,string value)
        {
            Msg msg = new Msg();
            try {
                var request = new
                {
                    userAddress =String.IsNullOrEmpty(userAddress)? _userAddress:userAddress,
                    value = value
                };
                var response = await _httpClient.PostAsJsonAsync(_baseUrl+ "/api/contract/collaboration/set", request);
                if (response.IsSuccessStatusCode) {
                    var content = await response.Content.ReadAsStringAsync();
                    try { 
                        var jsonResponse=JsonDocument.Parse(content);
                        if (jsonResponse.RootElement.TryGetProperty("code", out var code))
                        {
                            if (code.ToString() == "0")
                            {
                                msg.Code = 0;
                                msg.Data = jsonResponse.RootElement.GetProperty("data");
                            }
                            else {
                                msg.Code = 201;
                                msg.Message = $"set返回代码：{code.ToString()}";
                            }
                        }
                    } catch (Exception ex) {
                        msg.Code = 200;
                        msg.Message = ex.Message;
                    }
                }

            } catch (Exception ex) {
                msg.Code = 100;
                msg.Message= ex.Message;

            }
            return (msg);
        }
    }
}
