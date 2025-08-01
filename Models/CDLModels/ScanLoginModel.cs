namespace SolidarityBookCatalog.Models.CDLModels
{
    public class ScanLoginModel
    {
    }
    // 模型类
    public class QRCodeResponse
    {
        public string Ticket { get; set; } = string.Empty;
        public string QRCodeImage { get; set; } = string.Empty; // Base64格式的二维码图片
        public DateTime ExpireTime { get; set; }
    }

    public class LoginStatusResponse
    {
        public string Status { get; set; } = string.Empty; // waiting, scanned, confirmed, expired
        public Reader? UserInfo { get; set; }
    }

    public class ScanRequest
    {
        public string Ticket { get; set; } = string.Empty;
    }

    // 登录状态类
    public class LoginState
    {
        public string Status { get; set; } = "waiting"; // waiting, scanned, confirmed, expired
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScannedAt { get; set; }
        public Reader? UserInfo { get; set; }
    }
    //通过openid返回读者数字借阅图书
    public class CdlReaderLending
    { 
        public string bookName { set; get; }
        public string dueDate { set; get;   }
        public string userName { set; get; }
        public int totalPages { set; get; }
    }
}
