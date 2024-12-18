using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;

using MongoDB.Driver;
using SolidarityBookCatalog.Models;
namespace SolidarityBookCatalog.Services
{
    public class ElasticService
    {
        private readonly ElasticsearchClient client;
        public ElasticService(IConfiguration config) {
            string connStr = config.GetConnectionString("ElasticSearchDB").ToString();
            var settings = new ElasticsearchClientSettings(new Uri(connStr));
            client = new ElasticsearchClient(settings);
        }
        public Msg CreateIndex()
        { 
            Msg Msg = new Msg();
            var createIndexResponse = client.Indices.Create("books", c => c
            .Settings(s => s
                .Analysis(a => a
                    .Tokenizers(t => t
                        .Keyword("ik_max_word", kt => kt.Type("ik_max_word"))  // IK分词器
                    )
                    .Analyzers(an => an
                        .Custom("ik_max_word_analyzer", ca => ca
                            .Tokenizer("ik_max_word")
                        )
                    )
                )
            )
           .Map<Biblios>(m => m
               .Properties(p => p
                   .Keyword(k => k.Name(n => n.Id))
                   .Text(t => t.Name(n => n.Title))
                   .Text(t => t.Name(n => n.Creator))
                   .Text(t => t.Name(n => n.Subject))
                   .Text(t => t.Name(n => n.Description))
                   .Keyword(k => k.Name(n => n.Publisher))
                   .Keyword(k => k.Name(n => n.Identifier)) // ISBN
                   .Text(t => t.Name(n => n.Relation))
                   .Text(t => t.Name(n => n.Coverage))
                   .Keyword(k => k.Name(n => n.Type))
                   .Keyword(k => k.Name(n => n.Format))
                   .Keyword(k => k.Name(n => n.Source))
                   .Keyword(k => k.Name(n => n.Language))
                   .Text(t => t.Name(n => n.Rights))
                   .ScaledFloat(s => s.Name(n => n.Price).ScalingFactor(100))
                   .Date(d => d.Name(n => n.Created))
                   .Keyword(k => k.Name(n => n.UserName))
                   .Keyword(k => k.Name(n => n.Date)) // Publication Date (used for aggregation)
               )
           )
       );
            if (createIndexResponse.IsValid)
            {
                Console.WriteLine("Index created successfully.");
            }
            else
            {
                Console.WriteLine($"Error creating index: {createIndexResponse.OriginalException.Message}");
            }

            return Msg;
        
        }
    }
}
