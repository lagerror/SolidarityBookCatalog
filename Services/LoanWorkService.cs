using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using System.Threading.Tasks;

namespace SolidarityBookCatalog.Services
{
    public class LoanWorkService
    {

        public readonly IMongoCollection<User> _users;
        public readonly IMongoCollection<Biblios> _biblios;
        public readonly IMongoCollection<Holding> _holdings;
        public readonly IMongoCollection<Reader> _readers;
        private IMongoDatabase  database;
        public LoanWorkService(IConfiguration config, IMongoClient client)
        {
            database = client.GetDatabase("BookReShare");
            _users = database.GetCollection<User>("user");
            _biblios = database.GetCollection<Biblios>("biblios");
            _holdings = database.GetCollection<Holding>("holding");
            _readers = database.GetCollection<Reader>("reader");

        }
        //返回快递柜详细信息
        public async Task<dynamic> getDestinationLockerDetailAsync(string destinationLocker)
        {
            var temp =new {
                name="test"
            };
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

}
