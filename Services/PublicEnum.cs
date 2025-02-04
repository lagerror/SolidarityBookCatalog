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
            Staff=1,       // 教工
            Doctor=2,      // 博士
            Master=3,      // 硕士
            Bachelor=4,    // 本科
            HighSchool=5,  // 初高中
            Child=6,       // 儿童
            Other=7        // 其他
        }
    }
}
