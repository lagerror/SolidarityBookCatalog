namespace SolidarityBookCatalog.Models
{
    public class SearchQuery
    {
        public string Field {  get; set; }
        public string Keyword { get; set; }
        public string? Logic { get; set; }
    }
    public class SearchQueryList
    { 
        public List<SearchQuery> List { get; set; }
    }
}
