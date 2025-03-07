using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using static QRCoder.PayloadGenerator;

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

        /// <summary>
        /// 生成带Logo的二维码并转换为Base64字符串
        /// </summary>
        /// <param name="content">二维码内容</param>
        /// <param name="logoPath">logo图片路径</param>
        /// <param name="height">二维码图片高度，默认240单位pixels</param>
        /// <param name="width">二维码图片宽度，默认240单位pixels</param>
        /// <param name="margin">二维码图片边距，默认为0</param>
        /// <returns>Base64字符串</returns>
        public static byte[] GenerateQRCodeWithLogo(string url,int size=4)
        {
            // 1. 生成二维码基础图像 

            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData codeData = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.L, true);
            QRCoder.BitmapByteQRCode qrImage = new QRCoder.BitmapByteQRCode(codeData);
            PngByteQRCode qrCode = new PngByteQRCode(codeData);
            return qrCode.GetGraphic(size);
        }
    }


}
