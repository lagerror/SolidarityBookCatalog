namespace SolidarityBookCatalog.Models.WKModels
{
    public class KnowledgeDataItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int tenant_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string knowledge_base_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 奥林匹克之梦中国与体育1895-2008(美 徐国琦).pdf
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// 本书探讨了从1895年至2008年期间，体育在中国现代化进程中的作用及其与民族复兴和国际地位的关系。通过分析中国参与奥运会的历史、乒乓外交等事件，揭示了体育在塑造国家认同和推动国际化进程中所扮演的关键角色。
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string parse_status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string enable_status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string embedding_model_id { get; set; }
        /// <summary>
        /// 奥林匹克之梦中国与体育1895-2008(美 徐国琦).pdf
        /// </summary>
        public string file_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string file_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int file_size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string file_hash { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string file_path { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int storage_size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public dynamic metadata { get; set; }
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
        public string processed_at { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string error_message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string deleted_at { get; set; }
    }

    public class Knowledge
    {
        /// <summary>
        /// 
        /// </summary>
        public List<KnowledgeDataItem> data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int page { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int page_size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool success { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int total { get; set; }
    }

}
