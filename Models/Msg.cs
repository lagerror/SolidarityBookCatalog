namespace SolidarityBookCatalog.Models
{
    public class Msg
    {
        public int Code { set; get; } = -1;
        public string? Message { set; get; }
        public dynamic? Data { set; get; }

        public override string ToString()
        {
            return $"{Code}:{Message}";
        }
    }
}
