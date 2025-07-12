using Nest;

namespace SolidarityBookCatalog.Models
{
    public class DbNavigation
    {
        [Keyword]
        public string Id { get; set; } = Guid.NewGuid().ToString("N"); // 自动生成唯一ID

        [Keyword]
        public string Initial { get; set; }

        [Keyword]
        public string Language { get; set; }

        [Text(Fielddata = true)]
        public string DocTypes { get; set; }

        [Text(Fielddata = true)]
        public string Database { get; set; }

        [Keyword]
        public string Url { get; set; }

        [Date]
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    }
}
