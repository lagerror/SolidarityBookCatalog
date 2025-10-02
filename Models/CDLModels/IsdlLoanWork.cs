using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SolidarityBookCatalog.Models.CDLModels
{
    public enum LoanStatus
    {
        Applying,    // 申请中
        Approved,   // 上传文件完毕
        Rejected,   // 已拒绝
        Completed   // 文件下载完毕
    }

    public class IsdlLoanWork
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        //书
        [BsonElement("isbn")]
        [Required]
        public string ISBN { get; set; } = string.Empty;
        //人
        [BsonElement("openId")]
        [Required]
        public string OpenId { get; set; } = string.Empty;
        //加密openId
        [BsonElement("readerOpenId")]
        [Required]
        public string ReaderOpenId { get; set; } = string.Empty;
        //申请时间
        [BsonElement("applicationTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ApplicationTime { get; set; } = DateTime.UtcNow;
        //文件上传时间
        [BsonElement("fileUpTime")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? FileUpTime { get; set; }
        //文件上传人
        [BsonElement("fileUpOpenId")]
        public string? FileUpOpenId { get; set; }
        //文件路径
        [BsonElement("filePath")]
        public string? FilePath { get; set; }
        //图书详细信息
        [BsonElement("Biblios")]
        public Biblios? Biblios { get; set; }
        //读者详细信息
        [BsonElement("Reader")]
        public Reader? Reader { get; set; }
        //事务处理状态
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public LoanStatus Status { get; set; } = LoanStatus.Applying;
        //过期时间，文件下载后14天
        [BsonElement("expiryDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ExpiryDate { get; set; }
        //文件类型，浏览器可以自己判断
        [BsonElement("fileType")]
        [BsonRepresentation(BsonType.String)]
        public string FileType { get; set; } = "pdf";
        //取消申请时候的备注
        [BsonElement("remark")]
        public string? Remark { get; set; }
    }
}
