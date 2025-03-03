using MongoDB.Bson.IO;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace SolidarityBookCatalog.Services
{
    public class LoanWorkService
    {

        public readonly IMongoCollection<User> _users;
        public readonly IMongoCollection<Biblios> _biblios;
        public readonly IMongoCollection<Holding> _holdings;
        public readonly IMongoCollection<Reader> _readers;
        public readonly HttpClient _httpClient;
        private readonly string _baseUrl;
       
        private IMongoDatabase  database;
        public LoanWorkService(IConfiguration config, IMongoClient client, HttpClient httpClient)
        {
            database = client.GetDatabase("BookReShare");
            _httpClient = httpClient;
            _users = database.GetCollection<User>("user");
            _biblios = database.GetCollection<Biblios>("biblios");
            _holdings = database.GetCollection<Holding>("holding");
            _readers = database.GetCollection<Reader>("reader");
            _baseUrl = config["Express:BaseUrl"];
            _httpClient = httpClient;
        }
        //返回快递柜详细信息
        public async Task<Locker> getDestinationLockerDetailAsync(string destinationLocker)
        {
            var paras = $"{{'list':[{{'field':'ICCID','keyWord': '{destinationLocker}','logic': ''}}]}}".Replace("'","\"");
            string url = _baseUrl + $"/api/Locker/search?rows=10&page=1";
            HttpContent httpContent = new StringContent(paras, Encoding.UTF8, "application/json") ;
            HttpResponseMessage response = await _httpClient.PostAsync(url,httpContent);
            // 确保HTTP响应状态为200 (OK)
            if (response.IsSuccessStatusCode)
            {
                // 读取响应内容
                string responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                Msg msgTemp = JsonSerializer.Deserialize<Msg>(responseBody, options);

                if (msgTemp.Code == 0)
                {
                    TempLocker tempLocker = JsonSerializer.Deserialize<TempLocker>(msgTemp.Data.ToString(), options);
                    return tempLocker.rows[0];
                }
            }

            return null;
        }


        //返回读者学号和所在图书馆
        public async Task<dynamic> getReaderDetailAsync(string openId = "oS4N5tzvoJn2uJ7b1PzMJj99JgTw")
        {
            var reader = await _readers.Find(x => x.OpenId == openId).FirstOrDefaultAsync();
            if (reader != null)
            {
                return new 
                {
                    ReaderNo = reader.ReaderNo,
                    Library = reader.Library,
                };
            }
            return null;
        }
        //返回图书详细信息
        public async Task<dynamic> getHoldingDetailAsync(string id)
        {
            var holding = await _holdings.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (holding != null) 
            {
                var biblios= await _biblios.Find(x => x.Identifier == holding.Identifier).FirstOrDefaultAsync();
                if (biblios != null)
                {
                   
                    var user=await _users.Find(x => x.AppId == holding.UserName).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        return new
                        {
                            title = biblios.Title,
                            creator = biblios.Creator,
                            publisher = biblios.Publisher,
                            price = biblios.Price,
                            isbn = biblios.Identifier,
                            year = biblios.Date,
                            barcode = holding.Barcode,
                            bookRecNo = holding.BookRecNo,
                            library = user.Province + user.City + user.Name + user.MobilePhone
                        };
                    }
                }
            }

            return null;
        }

        //返回图书馆找书入柜人员信息
        public async Task<dynamic> getLibrarianDetailAsync(string openId)
        {
            var reader=await _readers.Find(x=>x.OpenId == openId).FirstOrDefaultAsync();
            if (reader != null)
            {
                return new
                {
                    name = reader.Name,
                    library = reader.Library,
                    phone = reader.Phone
                };
            
            }

            return null;
        }
        //返回取书快递人员信息
        public async Task<dynamic> getCourierDetailAsync(string openId)
        {
            var reader = await _readers.Find(x => x.OpenId == openId).FirstOrDefaultAsync();
            if (reader != null)
            {
                return new
                {
                    name = reader.Name,
                    phone = reader.Phone,
                    remark= reader.Remark
                };

            }

            return null;
        }

    }

    public class TempLocker
    { 
        public int total { set; get; }
        public List<Locker>? rows { set; get; }
    }
}
