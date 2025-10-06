namespace SolidarityBookCatalog.Models.WKModels
{
  
    public class Chunking_config
    {
        /// <summary>
        /// 
        /// </summary>
        public int chunk_size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int chunk_overlap { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> separators { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool enable_multimodal { get; set; }
    }

    public class Image_processing_config
    {
        /// <summary>
        /// 
        /// </summary>
        public string model_id { get; set; }
    }

    public class Vlm_config
    {
        /// <summary>
        /// 
        /// </summary>
        public string model_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string base_url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string api_key { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string interface_type { get; set; }
    }

    public class Cos_config
    {
        /// <summary>
        /// 
        /// </summary>
        public string secret_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string secret_key { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string region { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string bucket_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string app_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string path_prefix { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string provider { get; set; }
    }

    public class DataItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 人文素养书单
        /// </summary>
        public string name { get; set; }
    /// <summary>
    /// 1、认识自我与扎根中国：建立文化认同与身份坐标。
    /// </summary>
    public string description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int tenant_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Chunking_config chunking_config { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Image_processing_config image_processing_config { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string embedding_model_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string summary_model_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rerank_model_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string vlm_model_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Vlm_config vlm_config { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Cos_config cos_config { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string deleted_at { get; set; }
    }

    public class KnowledgeBase
    {
        /// <summary>
        /// 
        /// </summary>
        public List<DataItem> data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool success { get; set; }
    }



}
