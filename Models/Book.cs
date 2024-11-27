using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace SolidarityBookCatalog.Models
{
    /*
     DC（Dublin Core，都会核心元数据）标准是一个用于描述文档资源的元数据标准，它定义了15个核心元素，用于描述资源的基本属性。以下是基于DC标准的图书类定义：

    1. DC 元数据核心元素
    根据DC标准，核心元素包括以下属性：

    Title（标题）
    Creator（创建者/作者）
    Subject（主题/关键字）
    Description（描述）
    Publisher（出版者）
    Contributor（贡献者）
    Date（日期，如出版日期）
    Type（类型，如小说、论文）
    Format（格式，如PDF、纸质书）
    Identifier（标识符，如ISBN）
    Source（来源）
    Language（语言）
    Relation（相关资源）
    Coverage（范围，如时间或地理范围）
    Rights（版权信息）

     */
    public class Book
    {
        private string _identifier;

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // DC 元数据核心元素
        [BsonElement("Title")]
        public string? Title { get; set; } // 标题

        [BsonElement("Creator")]
        public string? Creator { get; set; } // 作者

        [BsonElement("Subject")]
        public string? Subject { get; set; } // 主题或关键字

        [BsonElement("Description")]
        public string? Description { get; set; } // 描述

        [BsonElement("Publisher")]
        public string? Publisher { get; set; } // 出版者

        [BsonElement("Contributor")]
        public string? Contributor { get; set; } // 贡献者

        //[ValidYear(ErrorMessage = "日期必须是4位有效年份。")]
        [BsonElement("Date")]
        public string? Date { get; set; } // 日期 (建议使用 ISO 格式：YYYY)

        [BsonElement("Type")]
        public string? Type { get; set; } // 资源类型

        [BsonElement("Format")]
        public string? Format { get; set; } // 格式 (如 PDF、电子书)

        //[ValidIsbn13(ErrorMessage = "标识符必须是13位不带短横线的ISBN号。如果是10位会自动转换位13位")]
        [BsonElement("Identifier")]
        public string? Identifier { set; get; }  // 标识符 (如 ISBN)
        
        [BsonElement("Source")]
        public string? Source { get; set; } // 来源

        [BsonElement("Language")]
        public string? Language { get; set; } // 语言 (如 "en", "zh")

        [BsonElement("Relation")]
        public string? Relation { get; set; } // 相关资源

        [BsonElement("Coverage")]
        public string? Coverage { get; set; } // 时间或地理范围

        [BsonElement("Rights")]
        public string? Rights { get; set; } // 权利或版权声明

        [BsonElement("Price")]
        public Decimal? Price   { get; set; }=0;  //价格十进制

        public DateTime? Created { get; set; }=DateTime.Now;   //记录创建时间
      
        public string? UserName { get; set; }   //记录创建用户


        //将10位ISBN统一转换位13不带-的格式
        public static string ConvertIsbn10ToIsbn13(string isbn10)
        {
            // 移除非数字字符
            string cleanIsbn10 = Regex.Replace(isbn10, @"[^\dX]", "");

            // 检查长度是否为10位
            if (cleanIsbn10.Length != 10)
            {
                throw new ArgumentException("Invalid ISBN-10 format.");
            }

            // 添加 "978" 前缀并取前12位
            string isbn13Prefix = "978" + cleanIsbn10.Substring(0, 9);

            // 计算校验码
            int checksum = 0;
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(isbn13Prefix[i].ToString());
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            // 校验码为10减去模10的结果
            checksum = (10 - (sum % 10)) % 10;
            // 拼接完整的ISBN-13
            return isbn13Prefix + checksum;
           
        }
        //校验13位isbn
        public static bool CalculateIsbn13Checksum(string isbn13)
        {
            if (isbn13.Length != 13)
            {
                return false;
            }

            int sum = 0;

            // ISBN-13 校验码计算公式
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(isbn13[i].ToString());
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            // 校验码为10减去模10的结果
            int checksum = (10 - (sum % 10)) % 10;
            
            return checksum.ToString() == isbn13.Substring(12, 1);
            
        }
        
        //控制端，检查ISBN，去掉-,统一转换位13
        public static  Tuple<bool,string> validIsbn(string isbn)
        {
            bool flag = false;
            if (isbn.Length != 10 & isbn.Length != 13 & isbn.Length != 17)
            {
                return new Tuple<bool, string>(flag, "错误isbn");
            }
            //将10位统一转化为13位
            isbn = isbn.Replace("-", "");
            if (isbn.Length == 10)
            {
               isbn= ConvertIsbn10ToIsbn13(isbn);
            }
            //校验
            if (CalculateIsbn13Checksum(isbn))
            {
                flag = true;
            }
            return new Tuple<bool,string>(flag,isbn);
        }
        //控制端，把年转为位四位
        public static Tuple<bool, string> validYear(string year)
        { 
            bool flag=false;
            //取出四位数字
            string pattern = @"\b(\d{4})\b";
            Match match = Regex.Match(year, pattern);
            if (match.Success)
            {
                year = match.Groups[1].Value;
            }
            //看是否能转换为年
            DateTime result;
            if (DateTime.TryParseExact(year, "yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            {
                flag=true;
            }
            return new Tuple<bool,string>(flag, year);
        }
        //控制端，把价格转换为包含2位小时的十进制
        public static Tuple<bool, decimal> validPrice(string price)
        {
            bool flag=false;
            decimal result=0;
            string pattern = @"^\d+(\.\d{1,2})?$";
            Match match = Regex.Match(price, pattern);
            if (match.Success)
            {
                price = match.Groups[1].Value;
                if (decimal.TryParse(price, out result))
                { 
                    flag=true;
                }
            }
            return new Tuple<bool, decimal>(flag, result);
        }
    }

    public class ValidYearAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is string year)
            {
                return DateTime.TryParseExact(year, "yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result);
            }
            return false;
        }
    }

    public class ValidIsbn13Attribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            bool flag=false;
            if (value == null)
            {
                return true;
            }
           
            if (value is string isbn)
            {
                isbn = isbn.Replace("-", "");
                // 检查ISBN是否为13位数字
                if (isbn.Length == 13 && isbn.All(char.IsDigit)) {

                    int sum = 0;
                    string isbn12=isbn.Substring(0, 12);
                    string valid = isbn.Substring(12,1);
                    // ISBN-13 校验码计算公式
                    for (int i = 0; i < 12; i++)
                    {
                        int digit = int.Parse(isbn12[i].ToString());
                        sum += (i % 2 == 0) ? digit : digit * 3;
                    }

                    // 校验码为10减去模10的结果
                    int checksum = (10 - (sum % 10)) % 10;
                    try
                    {
                        if(Convert.ToInt32(valid)==checksum)
                            flag = true;
                    }
                    catch (Exception ex) {

                        Console.WriteLine("ISBN校验错误");                        
                    }
                    
                }
            }
            return flag;
        }
    }

    

}
