using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using QRCoder;
using Serilog;
using SolidarityBookCatalog.Models.CDLModels;
using SolidarityBookCatalog.Services;
using System.Security.Cryptography;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeChatLoginController : ControllerBase
    {
        private const int QRCodeExpireSeconds = 60; // 二维码有效期(秒)
        private const int CheckInterval = 2000; // 检查间隔(毫秒)
    
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeChatLoginController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IBDIot _bDIot;
        public WeChatLoginController(IMemoryCache cache, ILogger<WeChatLoginController> logger,IConfiguration configuration, IBDIot bDIot)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _bDIot = bDIot;
        }

        // GET: api/<CdlController>
        [HttpGet]
        public IActionResult Get()
        {
            CdlReaderLending lend=new CdlReaderLending();
            lend.bookName = "书名";
            lend.userName = "李靖";
            lend.dueDate = "2025-05-01";
            lend.totalPages = 10;
            return Ok(lend);
        }

        /// <summary>
        /// 生成登录二维码
        /// </summary>
        [HttpGet("wxLogin")]
        public IActionResult GenerateQRCode()
        {
            // 生成唯一ticket
            var ticket = GenerateTicket();

            // 创建二维码内容 (实际应用中应使用微信API生成)
            var qrContent =string.Format("https://open.weixin.qq.com/connect/oauth2/authorize?appid=wx86605acd8f3d0820&redirect_uri=https://reader.yangtzeu.edu.cn/solidarity/api/wechat/oauth&response_type=code&scope=snsapi_userinfo&state={0}&connect_redirect=1#wechat_redirect",ticket); 

            // 生成二维码图片
            var qrCodeImage = GenerateQRCodeImage(qrContent);

            // 设置缓存状态
            var cacheEntry = new LoginState
            {
                Status = "waiting",
                CreatedAt = DateTime.UtcNow
            };
            //70秒后缓存值过期
            _cache.Set(ticket, cacheEntry, TimeSpan.FromSeconds(QRCodeExpireSeconds + 10));

            _logger.LogInformation($"生成二维码: {ticket}");
            //返回TICKER，二维码和过期时间
            return Ok(new QRCodeResponse
            {
                Ticket = ticket,
                QRCodeImage = $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}",
                ExpireTime = DateTime.UtcNow.AddSeconds(QRCodeExpireSeconds)
            });
        }

        /// <summary>
        /// 检查登录状态
        /// </summary>
        [HttpGet("check/{ticket}")]
        public IActionResult CheckLoginStatus(string ticket)
        {
            //如果找不到缓存值，则提示过期
            if (!_cache.TryGetValue(ticket, out LoginState? state) || state == null)
            {
                return Ok(new LoginStatusResponse { Status = "expired" });
            }

            // 检查是否过期
            if (DateTime.UtcNow > state.CreatedAt.AddSeconds(QRCodeExpireSeconds))
            {
                _cache.Remove(ticket);
                return Ok(new LoginStatusResponse { Status = "expired" });
            }

            // 模拟用户确认延迟
            if (state.Status == "confirmed" &&
                DateTime.UtcNow > state.ScannedAt!.Value.AddSeconds(5))
            {
               
            }
            
            return Ok(new LoginStatusResponse
            {
                Status = state.Status,
                UserInfo = state.Status == "confirmed" ? state.UserInfo : null
            });
        }

        /// <summary>
        /// 模拟手机扫码
        /// </summary>
        [HttpPost("scan")]
        public IActionResult ScanQRCode([FromBody] ScanRequest request)
        {
            if (!_cache.TryGetValue(request.Ticket, out LoginState? state) || state == null)
            {
                return BadRequest("二维码已过期");
            }

            state.Status = "scanned";
            state.ScannedAt = DateTime.UtcNow;

            _logger.LogInformation($"二维码已扫描: {request.Ticket}");

            return Ok();
        }

        /// <summary>
        /// 模拟手机确认登录
        /// </summary>
        [HttpPost("confirm")]
        public IActionResult ConfirmLogin([FromBody] ScanRequest request)
        {
            if (!_cache.TryGetValue(request.Ticket, out LoginState? state) || state == null)
            {
                return BadRequest("二维码已过期");
            }

            state.Status = "confirmed";
           
            _logger.LogInformation($"用户已确认登录: {request.Ticket}");

            return Ok();
        }


        private static string GenerateTicket()
        {
            var randomBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return "WxLogin_" + Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Substring(0, 16);
        }

        private static byte[] GenerateQRCodeImage(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(10);
        }
    }
}
