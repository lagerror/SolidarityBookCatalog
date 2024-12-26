using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
namespace SolidarityBookCatalog.Services
{
    public class ElasticService
    {
        private readonly ElasticsearchClient _elastic;
        public readonly IMongoCollection<Biblios> _books;
        public ElasticService(IConfiguration config)
        {
            string connStr = config.GetConnectionString("ElasticSearchDB").ToString();
            var settings = new ElasticsearchClientSettings(new Uri(connStr));
            settings.DefaultIndex("biblios");
            _elastic = new ElasticsearchClient(settings);

            var client = new MongoClient(config.GetConnectionString("BookDb"));
            var database = client.GetDatabase("BookReShare");
            _books = database.GetCollection<Biblios>("biblios");
        }
        public Msg CreateIndex()
        {
            Msg Msg = new Msg();
            var createIndexResponse = _elastic.Indices.CreateAsync<Biblios>
                (index => index
                .Index("biblios")
                .Mappings(mappings => mappings
                    .Properties(p => p
                       .Text(t => t.Title)
                       .Text(t => t.Creator)
                       .Text(t => t.Subject)
                       .Text(t => t.Description)
                       .Keyword(k => k.Publisher)
                       .Keyword(k => k.Identifier) // ISBN
                       .Keyword(t => t.Relation)
                       .Keyword(d => d.Date)      // Publication Date (used for aggregation)
                       .Keyword(k => k.UserName)
                       )
                    )
                );


            if (createIndexResponse.Result.IsValidResponse)
            {
                Msg.Code = 0;
                Msg.Message = $"创建biblios索引成功";
            }
            else
            {
                Msg.Code = 1;
                Msg.Message = $"创建biblios索引失败";
            }

            return Msg;
        }

        public async Task<Msg> InsertBibliosOneAsync(string isbn)
        {
            Msg msg = new Msg();
            Biblios biblios = _books.Find(x => x.Identifier == isbn).FirstOrDefault();
            if (biblios != null)
            {
                var res = await _elastic.IndexAsync<Biblios>(biblios);
                if (res.IsValidResponse)
                {
                    msg.Code = 0;
                    msg.Message = $"插入{isbn}成功";
                }
                else {
                    msg.Code = 1;
                    msg.Message = $"插入{isbn}失败";
                }
            }
            return msg;
        }

        private bool BulkInsertToElasticsearch(List<Biblios> list, int batchNumber)
        {
            bool flag = false;
            
            //var tokenSource = new CancellationTokenSource();
            //// Prepare the BulkAll request
            //var bulkAllRequest = _elastic.BulkAll<Biblios>(list, b => b
            //    .Index("biblios") // Specify the index
            //    .MaxDegreeOfParallelism(1) // Control parallelism (adjust as necessary)
            //    .Size(1000) // Set batch size for Elasticsearch
            //    .BackOffRetries(2) // Retry attempts on failure
            //    .BackOffTime("3s") // Time between retries
            //,tokenSource.Token);

            //// Set up the observer to track the bulk insert process
            //var bulkAllObserver = new BulkAllObserver(
            //    onNext: response =>
            //    {
            //        Console.WriteLine($"Batch {batchNumber} Indexed: Page {response.Page}, Retries: {response.Retries}");
            //    },
            //    onError: ex =>
            //    {
            //        Console.WriteLine($"Error during bulk insert for batch {batchNumber}: {ex}");
            //    },
            //    onCompleted: () =>
            //    {
            //        flag = true;
            //        Console.WriteLine($"Bulk insert completed for batch {batchNumber}");
            //    }
            //);

            //// Subscribe to the bulk operation to track progress
            ////bulkAllRequest.Subscribe(bulkAllObserver);
            //var bulkAllTask = Task.Run(() =>
            //{
            //    var bulkAllObserver = new BulkAllObserver(
            //        onNext: b => Console.WriteLine($"Indexed {b.Page} documents."),
            //        onError: e => throw e, // Rethrow the exception to the task
            //        onCompleted: () => {
            //            flag = true; 
            //        } // No action needed on completion
            //    );

            //    // Subscribe to the observable bulk operation
            //    bulkAllRequest.Subscribe(bulkAllObserver);
            //});

            //// Wait for the bulk operation to complete
            //// 使用 ContinueWith 来等待 BulkAll 操作完成
            //bulkAllTask.ContinueWith(task =>
            //{
            //    if (task.IsFaulted)
            //    {
            //        // 处理异常
            //        Console.WriteLine($"BulkAll operation failed: {task.Exception}");
            //    }
            //    else if (task.IsCompletedSuccessfully)
            //    {
            //        // 操作成功完成
            //        Console.WriteLine("BulkAll operation completed successfully.");
            //        flag = true;
            //    }
            //    else if (task.IsCanceled)
            //    {
            //        // 操作被取消
            //        Console.WriteLine("BulkAll operation was canceled.");
            //    }
            //}, tokenSource.Token); // 可以传递 CancellationToken 以便在取消时也取消 ContinueWith

            //// 如果你需要在此处等待任务完成，可以使用 task.Wait()，但请注意这可能会导致死锁
            //// bulkAllTask.Wait();
            //Console.WriteLine($"是否结束：{flag}");
            return flag;
        }

        public async Task<Msg> InsertBibliosAllAsync(string isbn)
        {
            Msg msg = new Msg();

            using (var cursor = await _books.FindAsync<Biblios>(FilterDefinition<Biblios>.Empty))
            {
                List<Biblios> list = new List<Biblios>();
                int batchSize = 1000;
                int batchNumber = 0;

                // Iterate over all the documents in MongoDB
                while (await cursor.MoveNextAsync())
                {
                    foreach (var item in cursor.Current)
                    {
                        list.Add(item);

                        // If batch size is reached, execute the bulk insert
                        if (list.Count >= batchSize)
                        {
                            // BulkInsertToElasticsearch(list, batchNumber);
                            await _elastic.IndexManyAsync(list);
                            Console.WriteLine($"{batchNumber++}");
                            list.Clear();  // Clear the list for the next batch
                        }
                    }
                }

                // Process any remaining documents after the loop
                if (list.Count > 0)
                {
                    await _elastic.IndexManyAsync(list);
                }
            }
            return msg;
        } 
   
        public async Task<Msg> GetBibliosOneByIdAsync(string id)
        { 
            Msg msg= new Msg();
            var data= await _elastic.GetAsync<Biblios>(id);//
            if (data.IsValidResponse)
            {
                msg.Data = data.Source;
            }
            else
            {
                msg.Code = 1;
                msg.Message = $"没有找到记录id{id}";
            }
            return msg;
        }
        public async Task<Msg> GetBibliosOneByIsbnAsync(string isbn)
        {
            Msg msg = new Msg();
            var res= await _elastic.SearchAsync<Biblios>(s => s
                    .Index("biblios")
                    .From(0)
                    .Size(1)
                    .Query(q => q
                        .Term(t=>t.Field(f => f.Identifier).Value(isbn))
                    )
                );
            if (res.IsValidResponse)
            {
                var result = res.Documents;
                msg.Data = result;
                msg.Code = 0;
            }

            return msg;
        }
        public async Task<Msg> GetBibliosAllByText(string keyword,int rows=10,int page=1)
        {
            Msg msg = new Msg();
            rows = 10;
            try
            {
                var searchResponse = await _elastic.SearchAsync<Biblios>(s => s
                    .Index("biblios") // 替换为你的索引名称
                    .From((page - 1) * rows)                  // 分页起始位置
                    .Size(rows)                 // 每页文档数量
                    .Query(q => q
                        .MultiMatch(m => m
                            .Query(keyword)
                            )
                        )
                    );

                // 检查搜索结果
                if (searchResponse.IsValidResponse)
                {

                    // 搜索成功，处理结果
                    msg.Code = 0;
                    Hashtable ht = new Hashtable();
                   
                    List<Biblios> result = new List<Biblios>();
                    foreach (var item in searchResponse.Documents)
                    {
                        result.Add(item);
                    }
                    ht.Add("total", searchResponse.Total);
                    ht.Add("rows", result.ToList());
                    msg.Message = $"全字段查询成功{keyword}";
                    msg.Data =ht;
                }
                else
                {
                    msg.Code = 1;
                    msg.Message = $"全字段查询失败{keyword}";
                }
            }
            catch (Exception ex) {
                msg.Code = 100;
                msg.Message = $"全字段查询异常：{ex.Message}";
            }   
            return msg;
        }
    }
}
