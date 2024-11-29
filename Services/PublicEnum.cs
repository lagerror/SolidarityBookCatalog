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
    }
}
