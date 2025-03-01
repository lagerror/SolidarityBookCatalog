namespace SolidarityBookCatalog.Services
{
    public class ToolService
    {
        private readonly IConfiguration _configuration;
        private string cryptKey;
        private string cryptIv;

        public ToolService(IConfiguration configuration)
        {
            _configuration = configuration;
            cryptKey = _configuration["Crypt:key"];
            cryptIv = _configuration["Crypt:iv"];
        }

        //统一在这里加解密openid和时间戳，方便管理
        //加密openid和时间戳
        public Tuple<bool, string, string> EnCryptOPenId(string openId)
        {
            string tickerStr = DateTime.Now.Ticks.ToString();
            Tuple<bool, string, string> result = new Tuple<bool, string, string>(false, "", "");
            try
            {
                string encrypted = Tools.EncryptStringToBytes_Aes(openId + "@" + tickerStr, cryptKey, cryptIv);
                result = new Tuple<bool, string, string>(true, encrypted, tickerStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }
        //解密openid和时间戳
        public Tuple<bool, string, string> DeCryptOpenId(string encrypted)
        {
            Tuple<bool, string, string> result = new Tuple<bool, string, string>(false, "", "");
            try
            {
                string decrypted = Tools.DecryptStringFromBytes_Aes(encrypted, cryptKey, cryptIv);
                string[] arr = decrypted.Split('@');
                if (arr.Length == 2)
                {
                    result = new Tuple<bool, string, string>(true, arr[0], arr[1]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }
    }


}
