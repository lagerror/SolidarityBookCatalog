namespace SolidarityBookCatalog.Services
{
    public static class PublicEnum
    {
        public enum ActionType 
        { 
            delete,
            insert,
            update,
            select
        }

        public enum RightMask:UInt16
        { 
            delete=8, //(UInt16)0b1000,
            insert=4, //(UInt16)0b0100,
            update=2, //(UInt16)0b0010,
            select=1 //(UInt16)0b0001
        }
        public enum Type
        {
            教工 = 1,       // 教工
            大学生 = 2,      // 博士
            企业员工 = 3,      // 硕士
            社会读者 = 4,    // 本科
            小初高学生 = 5,  // 初高中
            儿童 = 6,       // 儿童
            其他 = 7,    // 其他
            馆际互借人员=8,  
            快递人员=9
        }

        public enum CirculationStatus
        {
            已申请 = 1,          // 已申请
            图书已找到 = 2,       // 图书已找到
            运输中 = 3,       // 运输中
            已到达目的地 = 4,// 已到达目的地
            已完成取书 = 5,        // 已完成取书
            申请中 = 6
        }
    }
}
