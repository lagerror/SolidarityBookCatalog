using MongoDB.Driver;
using SolidarityBookCatalog.Models;

namespace SolidarityBookCatalog.Services
{
    public class ReaderService
    {
        public readonly IMongoCollection<Reader> _readers;

        public ReaderService(IConfiguration config,IMongoClient client)
        {
            var database = client.GetDatabase("BookReShare");
            _readers = database.GetCollection<Reader>("reader");
        }


    }
}
