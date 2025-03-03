namespace SolidarityBookCatalog.Models
{
    public class Locker
    {
        // 主键，自增ID
        public int Id { get; set; }

        // 长江大学
        public string Owner { get; set; }

        // 所属区域（东校区图书馆）
        public string Area { get; set; }

        // 具体位置（一楼流通总台）
        public string Location { get; set; }

        // 设备IMEI（唯一标识）
        public string IMEI { get; set; }

        // 设备序列号（唯一标识）
        public string SN { get; set; }

        // SIM卡ICCID（可选）
        public string ICCID { get; set; }

        // 状态（Active/Inactive/Faulty）
        public string Status { get; set; } = "Active";

        // 创建时间
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 柜子总格子数（cellNumber）
        public int TotalCells { get; set; } = 12;

        // 当前格子是否打开状态，二进制字符串表述，0为开，1为关，不过最好每次使用前先查询可用数量，而不是从这里读取
        public string TotalUsable { get; set; } = "111111111111";

        //上次状态刷新时间
        public DateTime RefreshTime { get; set; } = DateTime.Now;

        // 联系人姓名和电话
        public string ContactPhone { get; set; }

        // 备注（长文本）
        public string Notes { get; set; }
    }
}
