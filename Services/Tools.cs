using System.Security.Cryptography;
using System.Text;

namespace SolidarityBookCatalog.Services
{
    public static class Tools
    {
        //aes加密
        public static string EncryptStringToBytes_Aes(string plainText, string cryptKey, string cryptIv)
        {
            byte[] encrypted;

            // 创建一个 AES 加密器
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(cryptKey);
                aesAlg.IV = Encoding.UTF8.GetBytes(cryptIv);

                // 创建一个 AES 加密器的加密对象
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // 创建一个内存流来存储加密后的数据
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // 创建一个加密流，将数据写入内存流中
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            // 将数据写入加密流
                            swEncrypt.Write(plainText);
                        }
                    }
                    // 获取加密后的字节数组
                    encrypted = msEncrypt.ToArray();

                }
            }

            // 返回加密后的字节数组
            return Convert.ToBase64String(encrypted);
        }
        //aes解密
        public static string DecryptStringFromBytes_Aes(string cipherText, string cryptKey, string cryptIv)
        {
            string plaintext = null;
            try
            {
                byte[] cipherByte = Convert.FromBase64String(cipherText);

                // 创建一个 AES 解密器
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(cryptKey);
                    aesAlg.IV = Encoding.UTF8.GetBytes(cryptIv);

                    // 创建一个 AES 解密器的解密对象
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    // 创建一个内存流来存储解密后的数据
                    using (MemoryStream msDecrypt = new MemoryStream(cipherByte))
                    {
                        // 创建一个加密流，将数据写入内存流中
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            // 创建一个读取器从加密流中读取数据
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                // 读取解密后的数据
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // 返回解密后的文本
            return plaintext;
        }

    }
}
