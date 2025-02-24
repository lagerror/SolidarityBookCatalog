using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SolidarityBookCatalog.Services;
using System;

namespace SolidarityBookCatalog.Models
{
    public class LoanWork
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // 核心图书标识
        [BsonElement("holdingId")]
        public string HoldingId { get; set; }

        // 申请阶段信息
        [BsonElement("application")]
        public ApplicationInfo? Application { get; set; }

        // 图书馆处理阶段
        [BsonElement("libraryProcessing")]
        public LibraryProcessingInfo? LibraryProcessing { get; set; }

        // 运输阶段信息
        [BsonElement("transport")]
        public TransportInfo? Transport { get; set; }

        // 目的地入柜信息
        [BsonElement("destinationLocker")]
        public DestinationLockerInfo? DestinationLocker { get; set; }

        // 用户取书信息
        [BsonElement("pickup")]
        public PickupInfo? Pickup { get; set; }

        // 流程状态
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public PublicEnum.CirculationStatus Status { get; set; }
    }

    // 申请信息嵌套类
    public class ApplicationInfo
    {
        [BsonElement("readerOpenId")]
        public string ReaderOpenId { get; set; }

        [BsonElement("applicationTime")]
        public DateTime ApplicationTime { get; set; } = DateTime.UtcNow;

        [BsonElement("sourceLocker")]
        public string SourceLocker { get; set; }

        [BsonElement("destinationLocker")]
        public string DestinationLocker { get; set; }
    }

    // 图书馆处理信息嵌套类
    public class LibraryProcessingInfo
    {
        [BsonElement("librarianOpenId")]
        public string LibrarianOpenId { get; set; }

        [BsonElement("lockerNumber")]
        public string LockerNumber { get; set; }

        [BsonElement("cellNumber")]
        public string CellNumber { get; set; }

        [BsonElement("depositTime")]
        public DateTime? DepositTime { get; set; }= DateTime.UtcNow;

        [BsonElement("operateIP")]
        public string? OperateIP { get; set; }
    }

    // 运输信息嵌套类
    public class TransportInfo
    {
        [BsonElement("courierOpenId")]
        public string CourierOpenId { get; set; }

        [BsonElement("pickupTime")]
        public DateTime? PickupTime { get; set; }
    }

    // 目的地快递柜信息嵌套类
    public class DestinationLockerInfo
    {
        [BsonElement("courierOpenId")]
        public string CourierOpenId { get; set; }

        [BsonElement("lockerNumber")]
        public string LockerNumber { get; set; }

        [BsonElement("cellNumber")]
        public string CellNumber { get; set; }

        [BsonElement("depositTime")]
        public DateTime? DepositTime { get; set; }

        [BsonElement("operateIP")]
        public string OperateIP { get; set; }
    }

    // 取书信息嵌套类
    public class PickupInfo
    {
        [BsonElement("readerOpenId")]
        public string ReaderOpenId { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("lockerNumber")]
        public string LockerNumber { get; set; }

        [BsonElement("cellNumber")]
        public string CellNumber { get; set; }

        [BsonElement("pickupTime")]
        public DateTime? PickupTime { get; set; }
    }

  
}