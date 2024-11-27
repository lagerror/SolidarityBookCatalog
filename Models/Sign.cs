namespace SolidarityBookCatalog.Models
{
    /*
        用于API调用签名;简单处理，没有防止回放攻击
     */
    public class Sign
    {
        public string apiKey { get; set; }
        public string nonce { get; set; }
        public string sign { get; set; }

        private IConfiguration _configuration;

        public Sign(IConfiguration configuration)
        { 
            _configuration = configuration;

        
        }
    }
}
