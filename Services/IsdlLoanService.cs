
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Models.CDLModels;
using SolidarityBookCatalog.Models.CDLModels;

namespace SolidarityBookCatalog.Services
{
    public interface IIsdlLoanService
    {
        Task<IsdlLoanWork> CreateLoanAsync(LoanApplicationDto application, string openId);
        Task<IEnumerable<IsdlLoanWork>> GetLoansListByOpenIdAsync(string readerOpenId);
        Task<IEnumerable<IsdlLoanWork>> GetAllLoansAsync(LoanStatus? status = null);
        Task<bool> UpdateLoanStatusAsync(string loanId, LoanUpdateDto updateDto);
        Task<bool> UploadPdfForLoanAsync(string loanId, FileUploadDto fileUpload);
        Task<(Stream fileStream, string contentType, string fileName)> DownloadPdfAsync(string loanId);
    }

    public class IsdlLoanService : IIsdlLoanService
    {
        private readonly IMongoCollection<IsdlLoanWork> _IsdlLoanWork;
        private readonly IMongoCollection<Biblios> _Biblios;
        private readonly IMongoCollection<Reader> _Reader;
        private readonly string _fileStoragePath;
        private readonly ILogger<IsdlLoanService> _logger;
        private readonly IMongoDatabase database;
        private readonly ToolService _toolService;
        public IsdlLoanService( IConfiguration configuration, IMongoClient client,ILogger<IsdlLoanService> logger,ToolService toolService)
        {
            database = client.GetDatabase("BookReShare");
            _IsdlLoanWork = database.GetCollection<IsdlLoanWork>("isdlLoanWork");
            _Biblios = database.GetCollection<Biblios>("biblios");
            _Reader = database.GetCollection<Reader>("reader");
            _fileStoragePath = configuration["ISDL:path"];
            _logger = logger;
            _toolService = toolService;
        }
        //申请数字借阅
        public async Task<IsdlLoanWork> CreateLoanAsync(LoanApplicationDto application,string openId)
        {
  
            var loan = new IsdlLoanWork
            {
                ISBN = application.ISBN,
                OpenId = openId,
                ReaderOpenId= application.ReaderOpenId,
                Status = LoanStatus.Applying
            };

            try
            {
                Biblios biblios=await _Biblios.Find(x => x.Identifier == application.ISBN).FirstOrDefaultAsync();
                Reader reader = await _Reader.Find(x => x.OpenId == application.ReaderOpenId).FirstOrDefaultAsync();
                //屏蔽相关信息
                reader.Password = "";
                reader.Phone = "";
                reader.OpenId = "";
                //直接写入借阅记录
                loan.Biblios = biblios;
                loan.Reader = reader;
                await _IsdlLoanWork.InsertOneAsync(loan);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                loan = null;
            }
            return loan;
        }
        //借阅查询通过readerOpenId
        public async Task<IEnumerable<IsdlLoanWork>> GetLoansListByOpenIdAsync(string openId)
        {
            var loans = await _IsdlLoanWork
                .Find(l => l.OpenId == openId)
                .ToListAsync();
            //屏蔽openId
            foreach (var loan in loans) { 

                loan.OpenId ="";
            }

            return loans;
        }
        //通过id查询借阅记录
        public async Task<IsdlLoanWork> getLoanById(string id)
        { 
            return await _IsdlLoanWork.Find(x=>x.Id==id).FirstOrDefaultAsync();
        
        }
        //借阅状态查询
        public async Task<IEnumerable<IsdlLoanWork>> GetAllLoansAsync(LoanStatus? status = null)
        {
            var filter = status.HasValue
                ? Builders<IsdlLoanWork>.Filter.Eq(l => l.Status, status.Value)
                : Builders<IsdlLoanWork>.Filter.Empty;

            var loans = await _IsdlLoanWork.Find(filter).ToListAsync();
            return loans;
        }
        //更新借阅状态和过期时间
        public async Task<bool> UpdateLoanStatusAsync(string loanId, LoanUpdateDto updateDto)
        {
            if (!Enum.TryParse<LoanStatus>(updateDto.Status, out var newStatus))
                return false;

            var update = Builders<IsdlLoanWork>.Update
                .Set(l => l.Status, newStatus);

            if (newStatus == LoanStatus.Approved)
            {
                update = update.Set(l => l.ExpiryDate, DateTime.UtcNow.AddDays(14));
            }

            var result = await _IsdlLoanWork.UpdateOneAsync(
                Builders<IsdlLoanWork>.Filter.Eq(l => l.Id, loanId),
                update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UploadPdfForLoanAsync(string loanId, FileUploadDto fileUpload)
        {

            var loan = await _IsdlLoanWork.Find(l => l.Id == loanId).FirstOrDefaultAsync();
            if (loan == null || loan.Status != LoanStatus.Approved) return false;
            string isbn=loan.ISBN;
            // 生成安全的文件名
            var fileName = $"{isbn}{Path.GetExtension(fileUpload.File.FileName)}";
            var filePath = Path.Combine(_fileStoragePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileUpload.File.CopyToAsync(stream);
            }
            var update = Builders<IsdlLoanWork>.Update
                .Set(l => l.FilePath, filePath)
                .Set(l => l.FileUploadTime, DateTime.UtcNow)
                .Set(l => l.FileUploaderOpenId, fileUpload.UploaderOpenId)
                .Set(l => l.Status, LoanStatus.Completed);

            var result = await _IsdlLoanWork.UpdateOneAsync(
                Builders<IsdlLoanWork>.Filter.Eq(l => l.Id, loanId),
                update);

            return result.ModifiedCount > 0;
        }

        public async Task<(Stream fileStream, string contentType, string fileName)> DownloadPdfAsync(string loanId)
        {
            var loan = await _IsdlLoanWork.Find(l => l.Id == loanId).FirstOrDefaultAsync();
            if (loan == null || string.IsNullOrEmpty(loan.FilePath) || !File.Exists(loan.FilePath))
                throw new FileNotFoundException("PDF not found");

            var fileStream = new FileStream(loan.FilePath, FileMode.Open, FileAccess.Read);
            return (fileStream, "application/pdf", $"book_{loan.ISBN}.pdf");
        }

        
    }
}
