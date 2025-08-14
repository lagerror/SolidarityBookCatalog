using System.ComponentModel.DataAnnotations;

namespace SolidarityBookCatalog.Models.CDLModels
{
    public class LoanApplicationDto
    {
        [Required]
        public string ISBN { get; set; } = string.Empty;

        [Required]
        public string ReaderOpenId { get; set; } = string.Empty;
    }

    public class LoanResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string ReaderNo { get; set; } = string.Empty;
        public DateTime ApplicationTime { get; set; }
        public string? OpenId {set;get;}
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public Biblios? Biblios { get; set; }
        public Reader? Reader { get; set; }
    }

    public class LoanUpdateDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class FileUploadDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string UploaderOpenId { get; set; } = string.Empty;
    }
}
