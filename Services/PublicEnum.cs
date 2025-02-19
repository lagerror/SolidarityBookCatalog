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
            其他 = 7        // 其他
        }
    }
}
