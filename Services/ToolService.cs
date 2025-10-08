using QRCoder;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Reflection.Emit;
using System.Security.Cryptography;
using SolidarityBookCatalog.Models;
using System.Text;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using SolidarityBookCatalog.Models.CDLModels;
using SolidarityBookCatalog.Models.WKModels;
using Elastic.Clients.Elasticsearch.Ingest;
using System.Web;

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
        //解密文件
        public async Task DecryptFileStreaming(Reader reader,IsdlLoanWork loan,Stream inputStream,Stream outputStream,int bufferSize = 81920)
        {
            try
            {
                // 1. 读取前16字节作为IV
                byte[] iv = new byte[16];
                int bytesRead = await inputStream.ReadAsync(iv, 0, iv.Length);

                if (bytesRead != iv.Length)
                {
                    throw new InvalidDataException("Invalid encrypted file format: missing IV");
                }

                // 2. 生成密钥（与加密逻辑相同）
                byte[] key;
                using (var sha256 = SHA256.Create())
                {
                    string temp = loan.ReaderOpenId+ "|" + loan.Id + "|" + loan.ISBN;
                    key = sha256.ComputeHash(Encoding.UTF8.GetBytes(temp));
                }

                // 3. 创建AES解密器
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);

                // 4. 流式解密
                var buffer = new byte[bufferSize];
                int bytesDecrypted;

                while ((bytesDecrypted = await cryptoStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await outputStream.WriteAsync(buffer, 0, bytesDecrypted);
                    await outputStream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
               
                throw;
            }
        }
        //加密专题库文件
        public async Task EncryptKnowledgeFile(Reader reader, KnowledgeDataItem knowledge, Stream inputStream, Stream outStream, int bufferSize = 81920)
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16];

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            //使用openid+申请id+isbn作为密码，iv随机生成
            using (var sha256 = SHA256.Create())
            {
                string temp = reader.OpenId + "|" + knowledge.id + "|" + knowledge.file_hash;
                Console.WriteLine($"知识库加密串： {temp}");
                key = sha256.ComputeHash(Encoding.UTF8.GetBytes(temp));
                byte[] ivTemp = sha256.ComputeHash(Encoding.UTF8.GetBytes(reader.ReaderNo + "|" + reader.OpenId + "|" + knowledge.id));
                Array.Copy(ivTemp, iv, 16);

            }
            aes.Key = key;
            aes.IV = iv;
            //写入iv
            await outStream.WriteAsync(iv, 0, 16);


            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var cryptoStream = new CryptoStream(outStream, encryptor, CryptoStreamMode.Write);

            var buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await cryptoStream.WriteAsync(buffer, 0, bytesRead);

                await cryptoStream.FlushAsync();
                await outStream.FlushAsync();
            }

            await cryptoStream.FlushFinalBlockAsync();
        }
        //加密文件
        public async Task EncryptFile(Reader reader,IsdlLoanWork loan, Stream inputStream,Stream outStream,int bufferSize=81920)
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16];

            using var aes=Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding=PaddingMode.PKCS7;
            //使用openid+申请id+isbn作为密码，iv随机生成
            using (var sha256 = SHA256.Create()) {
                string temp= HttpUtility.UrlDecode(loan.ReaderOpenId) + "|" + loan.Id+"|"+loan.ISBN;
                Console.WriteLine($"CDL加密串： {temp}");
                key = sha256.ComputeHash(Encoding.UTF8.GetBytes(temp)); 
                byte[] ivTemp= sha256.ComputeHash(Encoding.UTF8.GetBytes(reader.ReaderNo+"|"+reader.Id+"|"+loan.Id));
                Array.Copy(ivTemp, iv, 16);
               
            }
            aes.Key = key;
            aes.IV = iv;
            //写入iv
            await outStream.WriteAsync(iv, 0, 16);
            
            
            using var encryptor=aes.CreateEncryptor(aes.Key,aes.IV);
            using var cryptoStream=new CryptoStream(outStream, encryptor, CryptoStreamMode.Write);

            var buffer=new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await cryptoStream.WriteAsync(buffer, 0, bytesRead);

                await cryptoStream.FlushAsync();
                await outStream.FlushAsync();
            }

            await cryptoStream.FlushFinalBlockAsync();
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
        /// 生成带Logo的二维码
        /// </summary>
        /// <param name="content">二维码内容</param>
        /// <param name="logoPath">logo图片路径</param>
        /// <param name="height">二维码图片高度，默认240单位pixels</param>
        /// <param name="width">二维码图片宽度，默认240单位pixels</param>
        /// <param name="margin">二维码图片边距，默认为0</param>
        /// <returns>Base64字符串</returns>
        public static byte[] GenerateQRCode(string url, int size = 4)
        {
            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData codeData = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.L, true);
            QRCoder.BitmapByteQRCode qrImage = new QRCoder.BitmapByteQRCode(codeData);
            PngByteQRCode qrCode = new PngByteQRCode(codeData);
            return qrCode.GetGraphic(size);
        }
        public static byte[] GenerateQRCodeWithLogo(string url, int size = 4)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData)) // Correct the namespace and type
                {
                    Bitmap qrCodeImage = qrCode.GetGraphic(4, Color.Black, Color.White, (Bitmap)Bitmap.FromFile("./logleft.png"));
                    using (MemoryStream msFinal = new MemoryStream())
                    {
                        qrCodeImage.Save(msFinal, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] finalImageBytes = msFinal.ToArray();
                        return finalImageBytes;
                    }
                }
            }
        }
    }

}
