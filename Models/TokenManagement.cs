

namespace SolidarityBookCatalog.Models
{
    /// <summary>
    /// 存储JWT相关的配置信息
    /// </summary>
    public class TokenManagement
    {
        /// <summary>
        /// 令牌密钥
        /// </summary>

        public string Secret { get; set; } = null!;
        /// <summary>
        /// 颁发者
        /// </summary>

        public string? Issuer { get; set; }
        /// <summary>
        /// 接收者
        /// </summary>

        public string? Audience { get; set; }
        /// <summary>
        /// Token令牌过期时间（分钟）
        /// </summary>

        public int AccessExpiration { get; set; }
        /// <summary>
        /// Token刷新令牌过期时间（分钟）
        /// </summary>

        public int RefreshExpiration { get; set; }
    }
}
