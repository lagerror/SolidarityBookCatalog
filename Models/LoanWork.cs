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

        // 所借图书详细信息
        [BsonElement("holdingDetail")]
        public dynamic? HoldingDetail { get; set; }

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
        
        // 消息提示
        [BsonElement("remark")]
        [BsonRepresentation(BsonType.String)]
        public string? Remark { get; set; }

    }

    // 申请信息嵌套类
    public class ApplicationInfo
    {
        [BsonElement("readerOpenId")]
        public string ReaderOpenId { get; set; }

        // 申请者详细信息
        [BsonElement("readerDetail")]
        public ReaderDetail? ReaderDetail { get; set; }

        [BsonElement("applicationTime")]
        public DateTime ApplicationTime { get; set; } = DateTime.UtcNow;

        [BsonElement("sourceLocker")]
        public string SourceLocker { get; set; }

        [BsonElement("destinationLocker")]
        public string DestinationLocker { get; set; }

        //目的地快递柜信息
        [BsonElement("destinationLockerDetail")]
        public Locker? DestinationLockerDetail { get; set; }
    }

    // 图书馆处理信息嵌套类
    public class LibraryProcessingInfo
    {
        [BsonElement("librarianOpenId")]
        public string LibrarianOpenId { get; set; }

        //找书人员详细信息
        [BsonElement("librarianDetail")]
        public dynamic? LibrarianDetail { get; set; }

        //传递柜ICCID
        [BsonElement("lockerNumber")]
        public string? LockerNumber { get; set; }
        
        //快递柜格口
        [BsonElement("cellNumber")]
        public string? CellNumber { get; set; }

        ////找书人员放置快递柜的详细信息
        [BsonElement("lockerDetail")]
        public Locker? LockerDetail { get; set; }

        [BsonElement("depositTime")]
        public DateTime? DepositTime { get; set; }= DateTime.UtcNow;

        [BsonElement("remark")]
        public string? Remark { get; set; }
    }

    // 运输信息嵌套类
    public class TransportInfo
    {
        [BsonElement("courierOpenId")]
        public string CourierOpenId { get; set; }

        [BsonElement("courierDetail")]
        public dynamic? CourierDetail { get; set; }

        [BsonElement("pickupTime")]
        public DateTime? PickupTime { get; set; } = DateTime.UtcNow;

        [BsonElement("remark")]
        public string Remark { get; set; }
    }

    // 目的地快递柜信息嵌套类
    public class DestinationLockerInfo
    {
        [BsonElement("courierOpenId")]
        public string CourierOpenId { get; set; }

        [BsonElement("courierDetail")]
        public dynamic? CourierDetail { get; set; }

        [BsonElement("lockerNumber")]
        public string LockerNumber { get; set; }

        [BsonElement("cellNumber")]
        public string CellNumber { get; set; }
        
        //放置快递柜详细信息
        [BsonElement("lockerDetail")]
        public Locker? LockerDetail { get; set; }

        [BsonElement("depositTime")]
        public DateTime? DepositTime { get; set; } = DateTime.UtcNow;

        [BsonElement("remark")]
        public string? Remark { get; set; }
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

        [BsonElement("remark")]
        public string Remark { get; set; }
    }

    //以下为二层嵌入类
    //返回给前端的读者信息
    public class ReaderDetail 
    { 
        public string?  Name { get; set; }
        public string? Library {  get; set; }
        public string? Phone { set; get; }
        public string? ReaderNo { get; set; }
    }

}