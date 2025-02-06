using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SolidarityBookCatalog.Services;

namespace SolidarityBookCatalog.Models
{
    public class Reader
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        //微信OPENID，应用基于微信
        [Required]
        [StringLength(28, MinimumLength = 28)]
        public string OpenId { get; set; }
        //读者姓名
        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string Name { get; set; }
        //读者学号
        [Required]
        public string StudentId { get; set; }
        //读者类型，教工，博士，硕士，本科，初高中，儿童，其它
        public PublicEnum.Type? Type { set; get; }
        //年龄
        public int? BirthYear { get; set; }
        //手机号
        [Required]
        [RegularExpression(@"^1[3-9]\d{9}$")]
        public string Phone { get; set; }
        //开户馆
        [Required]
        public string Library { get; set; }
        //校区
        public string? Area { get; set; }
        //审核人
        public string? Auditor { get; set; }
        //审核时间
        public DateTime? AuditDate { get; set; }
        //注册时间
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime RegisterDate { get; set; } = DateTime.Now;
        //备注
        public string? Remark { get; set; }
        //身份证后六位，便于审核员在本地图书馆管理系统中确认注册者的身份，不作登录使用，登录强制使用微信扫码
        public string? Password { get; set; } // 
        //是否有效
        public bool? IsValid { get; set; } = false;
    }

    // DTO 用于创建请求
    public class CreateReaderDto
    {
        [Required]
        public string OpenId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string StudentId { get; set; }
       
        public PublicEnum.Type? Type { set; get; }
       
        public int? BirthYear { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Library { get; set; }
        //校区，因为多数大学有相距较远的多个校区
        public string Area { get; set; }

        [Required]
        public string Password { get; set; }
    }
}