namespace SolidarityBookCatalog.Models
{
    //微信用户信息
    public class WeChatUserInfoResponse
    {
        public string Openid { get; set; }
        public string Nickname { get; set; }
        public int Sex { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Headimgurl { get; set; }
        // ... 其他用户信息字段
    }
}
