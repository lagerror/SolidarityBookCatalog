using Elastic.Clients.Elasticsearch.MachineLearning;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Clients.Elasticsearch.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyModel;
using MongoDB.Bson;
using MongoDB.Driver;
using Nest;
using SharpCompress.Readers;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static SolidarityBookCatalog.Services.PublicEnum;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SolidarityBookCatalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoanWorkController : ControllerBase
    {
        IConfiguration _configuration;
        IMongoClient _client;
        ILogger<LoanWorkController> _logger;
        IMongoCollection<LoanWork> _loanWork;
        IMongoCollection<Reader> _reader;
        LoanWorkService _loanWorkService;
        IWeChatService _weChatService;
        private readonly ToolService _toolService;
        HttpClient _httpClient;

        private readonly string _baseUrl;
        public LoanWorkController(IMongoClient client,LoanWorkService loanWorkService,IWeChatService weChatService, HttpClient httpClient, IConfiguration configuration,ToolService toolService, ILogger<LoanWorkController> logger)
        {
            _configuration = configuration;
            _toolService = toolService;
            _baseUrl = _configuration["Express:BaseUrl"];
            _httpClient = httpClient;
            var database = client.GetDatabase("BookReShare");
            _loanWork = database.GetCollection<LoanWork>("loanWork");
            _reader = database.GetCollection<Reader>("reader");
            _loanWorkService= loanWorkService;
            _weChatService = weChatService;
            _logger = logger;

        }

        // GET: api/<LoanWorkController>
        [HttpGet]
        public async Task<IActionResult> fun(string act,string pars)
        {
            Msg msg = new Msg();
            switch (act)
            {
                case "index":// 创建唯一索引
                    var indexKeysDefinition = Builders<LoanWork>.IndexKeys.Ascending(x => x.Id);
                    var indexOptions = new CreateIndexOptions { Unique = true }; // 唯一性
                    var indexModel = new CreateIndexModel<LoanWork>(indexKeysDefinition, indexOptions);
                    _loanWork.Indexes.CreateOneAsync(indexModel);
                    break;
                case "getReaderDetail":
                    msg.Data=await _loanWorkService.getReaderDetailAsync(pars);
                    break;
                case "getDestinationLockerDetailAsync":
                    msg.Data=await _loanWorkService.getDestinationLockerDetailAsync(pars);
                    break;
            }

                return Ok(msg);
        }
        //管理人员修改申请的状态，并给出原因，后台发送微信通知
        [HttpPost]
        [Route("status")]
        [Authorize(Policy="AdminOrManager")]
        public async Task<IActionResult> status(string applyId,string remark,string status="申请终止")
        { 
            Msg msg= new Msg();
            //找到事务
            var apply = _loanWork.FindAsync(x => x.Id == applyId).Result.FirstOrDefault();
            if (apply == null)
            {
                msg.Code = 1;
                msg.Message = "没找到对应申请事务号";
                return Ok(msg);
            }

            apply.Remark = remark;

            try
            {
                Enum.TryParse<CirculationStatus>(status, out var statusOut);
                apply.Status = statusOut;
            }
            catch {
                msg.Code = 2;
                msg.Message = "状态值无效";
                return Ok(msg);
            }
           
            // 创建更新定义
            var updateDefinition = Builders<LoanWork>.Update
                .Set(x => x.Remark, apply.Remark)
                .Set(x => x.Status, apply.Status);

            // 更新数据库中的文档
            var updateResult = _loanWork.UpdateOne(x => x.Id == apply.Id, updateDefinition);

            // 检查更新结果
            if (updateResult.ModifiedCount == 1)
            {
                msg.Code = 0;
                msg.Message = "更新成功";

                //发送微信通知
                //发送微信通知
                if (apply.Application.ReaderDetail!= null)
                {
                    var notice = new
                    {
                        keyword1 = new { value = $"{apply.HoldingDetail?.Library}" },  //学校
                        keyword2 = new { value = $"{apply.Application.ReaderDetail.Name}借阅图书{apply.HoldingDetail?.Title}:状态更改" },  //通知人，快递员电话
                        keyword3 = new { value = $"{apply.Application?.ApplicationTime.ToString("yyyy-MM-dd")}" },   // 时间
                        keyword4 = new { value = $"该申请因为 {remark} 而取消" },   //取货的地点

                    };
                    await _weChatService.SendTemplateMessageAsync("notice", apply.Application.ReaderOpenId, $"https://reader.yangtzeu.edu.cn/wechat/my?openId={HttpUtility.UrlEncode(_toolService.EnCryptOPenId(apply.Application.ReaderOpenId).Item2)}", notice);
                }
            }

            return Ok(msg);
        }

        [HttpPost]
        [Route("search")]
        public async Task<IActionResult> search(SearchQueryList list, string openId = "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9",int rows=10,int page=1)
        {
            Msg msg = new Msg();
            //校验openId的解密，为查询者的openId
            var ret = _toolService.DeCryptOpenId(openId);
            if (!ret.Item1)
            {
                msg.Code = 3;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            openId = ret.Item2;

            //根据查询者OPENID查询读者信息，判断是否有权限和角色
            var reader = _reader.FindAsync<Reader>(x => x.OpenId == openId & x.IsValid==true).Result.FirstOrDefault();

            if (reader == null)
            {
                msg.Code = 2;
                msg.Message = "查询者没有找到或者不是有效状态";
                return Ok(msg);
            }


            //查询条件中是否有openid，如果有则解密
            var uniqueSearchQuery = list.List
                .SingleOrDefault(sq => sq.Field == "readerOpenId");

            // 如果找到了唯一的对象，则修改它的Keyword
            if (uniqueSearchQuery != null)
            {
               
                ret =_toolService.DeCryptOpenId(uniqueSearchQuery.Keyword);
                if (!ret.Item1)
                {
                    msg.Code = 2;
                    msg.Message = $"被查询者OpenId解密失败";
                    return Ok(msg);
                }
                uniqueSearchQuery.Keyword = ret.Item2;
            }

            switch (reader.Type)
            {
                case PublicEnum.Type.管理员:
                    msg = await searchAdmin(list, rows, page);

                    break;
                case PublicEnum.Type.快递人员:
                    //如果是快递人员，加上status=已完成取书 或者 运输中 
                    SearchQuery searchQuery2 = new SearchQuery();
                    searchQuery2.Field = "status";
                    searchQuery2.Keyword = "图书已找到";
                    list.List.Add(searchQuery2);

                    SearchQuery searchQuery3 = new SearchQuery();
                    searchQuery3.Field = "status";
                    searchQuery3.Keyword = "运输中";
                    list.List.Add(searchQuery3);


                    msg = await searchAdmin(list, rows, page);
                    break;
                case PublicEnum.Type.馆际互借人员:
                    //只能看到已经申请状态的
                    SearchQuery searchQuery1 = new SearchQuery();
                    searchQuery1.Field = "status";
                    searchQuery1.Keyword = "已申请";
                    list.List.Add(searchQuery1);

                    msg = await searchAdmin(list, rows, page);
                    break;
                default:   //其它都只能自己查自己的 查询条件会限定openid
                    SearchQuery searchQueryMyself = new SearchQuery();
                    searchQueryMyself.Field = "readerOpenId";
                    searchQueryMyself.Keyword = openId;
                    list.List.Add(searchQueryMyself);

                    msg = await searchAdmin(list, rows, page);
                    break;
            }
           

            return Ok(msg);
        }
        //管理人员查询函数
        private async Task<Msg> searchAdmin(SearchQueryList list, int rows = 10, int page = 1)
        {
            Msg msg = new Msg();
            try
            {
                var builder = Builders<LoanWork>.Filter;
                var filters = new List<FilterDefinition<LoanWork>>();

                // 按字段分组处理条件
                var groupedConditions = list.List.GroupBy(item => item.Field);
                foreach (var group in groupedConditions)
                {
                    var field = group.Key;
                    var conditions = group.ToList();
                    var orFilters = new List<FilterDefinition<LoanWork>>();

                    foreach (var item in conditions)
                    {
                        // 根据字段类型生成对应的过滤器
                        switch (item.Field)
                        {
                            case "status":
                                if (Enum.TryParse<CirculationStatus>(item.Keyword, out var status))
                                {
                                    orFilters.Add(builder.Eq(x => x.Status, status));
                                }
                                break;
                            case "readerOpenId":
                                orFilters.Add(builder.Eq(x => x.Application.ReaderOpenId, item.Keyword));
                                break;
                            case "id":
                                orFilters.Add(builder.Eq(x => x.Id, item.Keyword));
                                break;
                            case "phone":
                                orFilters.Add(builder.Eq(x => x.Application.ReaderDetail.Phone, item.Keyword));
                                break;
                            case "appId":
                                orFilters.Add(builder.Eq(x => x.HoldingDetail.AppId, item.Keyword));
                                break;
                        }
                    }

                    if (orFilters.Count == 0)
                    {
                        continue; // 忽略无效条件
                    }
                    else if (orFilters.Count == 1)
                    {
                        filters.Add(orFilters[0]); // 单个条件直接添加
                    }
                    else
                    {
                        filters.Add(builder.Or(orFilters)); // 多个条件用OR组合
                    }
                }
                // 组合所有分组过滤器为AND条件
                var finalFilter = filters.Count > 0
                    ? builder.And(filters)
                    : FilterDefinition<LoanWork>.Empty;

                // 执行查询
                var total = await _loanWork.CountDocumentsAsync(finalFilter);
                var results = await _loanWork.Find(finalFilter)
                    .Skip((page - 1) * rows)
                    .Limit(rows)
                    .ToListAsync();

                msg.Code = 0;
                msg.Message = "查询成功";
                msg.Data = new
                {
                    total = total,
                    rows = results
                };

            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = ex.Message;
            }
            return msg;
        }
    
        /// <summary>
        /// 哪个人，申请哪本书，从哪个柜子到哪个柜子
        /// </summary>
        /// <param name="readerOpenId">人</param>
        /// <param name="holdingId">书</param>
        /// <param name="destinationLocker">到哪个柜子</param>
        /// <returns></returns>
        [HttpPost]
        [Route("apply")]
        public  async Task<IActionResult> apply (string readerOpenId= "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9", string holdingId = "675805eff80692346fa8abe9", string destinationLocker = "898608341523D0678886")
        {
            Msg msg = new Msg();
            try
            {//rfWTm26wJZnpQ%252bS0Jgywkd1CwWuzRhO7mTGiI0%252fQZlY%253d  rfWTm26wJZnpQ+S0Jgywkd1CwWuzRhO7mTGiI0/QZlY= oS4N5tzvoJn2uJ7b1PzMJj99JgTw rfWTm26wJZnpQ+bS0Jgywkd1CwWuzRhO7mTGiI0%2fQZlY=    675805eff80692346fa8abe9   675805eff80692346fa8abe9
                //string v3 = WebUtility.UrlDecode(readerOpenId);
                //string v1 = Tools.EncryptStringToBytes_Aes("oS4N5tzvoJn2uJ7b1PzMJj99JgTw", _cryptKey, _cryptIv);
                //string v2 = Tools.DecryptStringFromBytes_Aes(v1, _cryptKey, _cryptIv);
                //将传入的openid做编码后传给前端
               string openId = HttpUtility.UrlEncode(readerOpenId);
                //校验openId的解密
                var ret =_toolService.DeCryptOpenId(readerOpenId);
                if (!ret.Item1)
                {
                    msg.Code = 3;
                    msg.Message = $"OpenId解密失败";
                    return Ok(msg);
                }
                readerOpenId = ret.Item2;

                LoanWork loanWork = new LoanWork();
                loanWork.HoldingDetail=await _loanWorkService.getHoldingDetailAsync(holdingId);
                ApplicationInfo applicationInfo = new ApplicationInfo();
                loanWork.Application = applicationInfo;

                loanWork.HoldingId = holdingId;
                loanWork.Status = PublicEnum.CirculationStatus.已申请;
                loanWork.Application.ReaderOpenId = readerOpenId;
                //申请者详细信息
                loanWork.Application.ReaderDetail = await _loanWorkService.getReaderDetailAsync(readerOpenId);
                loanWork.Application.DestinationLocker = destinationLocker;
                //目的快递柜详细信息
                loanWork.Application.DestinationLockerDetail =await _loanWorkService.getDestinationLockerDetailAsync(destinationLocker);
                //插入数据库
                await _loanWork.InsertOneAsync(loanWork);
                msg.Code = 0;
                msg.Message = "申请成功";
                //发送微信通知
                if (loanWork.Application.ReaderDetail != null)
                {
                    var loan = new
                    {
                        keyword1 = new { value = $"{loanWork.Application.ReaderDetail.Library}:{loanWork.Application.ReaderDetail.ReaderNo}：{loanWork.Application.ReaderDetail.Phone}" },
                        keyword2 = new { value = $"{loanWork.HoldingDetail.Title}:{loanWork.HoldingDetail.isbn}:{loanWork.HoldingDetail.Price.ToString()}" },  //书名
                        keyword3 = new { value = $"所在馆记录号：{loanWork.HoldingDetail.BookRecNo}" },   // ISBN
                        keyword4 = new { value = $"{loanWork.Application.ApplicationTime.ToString("yyyy-MM-dd")}" },   //借的时间
                        keyword5 = new { value = $"{loanWork.Application.ApplicationTime.AddDays(15).ToString("yyyy-MM-dd")}还回{loanWork.HoldingDetail.Library}"   }   //还的时间地点
                    };
                    await _weChatService.SendTemplateMessageAsync("loan", readerOpenId, $"https://reader.yangtzeu.edu.cn/wechat/my?openId={openId}", loan);
                }

            }
            catch (Exception ex)
            {
                msg.Code = 100;
                msg.Message = ex.Message;
                msg.Data = ex.StackTrace;

            }   
            return Ok(msg);
        }

        /// <summary>
        /// 图书馆互借人员找书入柜处理事务
        /// </summary>
        /// <param name="applyId">申请事务号</param>
        /// <param name="openId">工作人员</param>
        /// <param name="lockerNumber">放入哪个柜体,自动获取空余格口</param>
        /// <returns></returns>
        [HttpPost]
        [Route("libraryProcess")]
        public async Task<IActionResult> libraryProcess(string applyId= "67c30b1bf80110a819c18fce", string openId = "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9", string iccid = "898608341523D0678886")
        {
            Msg msg = new Msg();
            //校验openId的解密
           
            var ret =_toolService.DeCryptOpenId(openId);
            if (!ret.Item1)
            {
                msg.Code = 1;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            openId = ret.Item2;

            //判断该人员是否为图书馆找书工作人员
            var reader= _reader.FindAsync<Reader>(x => x.OpenId == openId).Result.FirstOrDefault();
            if (reader == null)
            {
                msg.Code = 2;
                msg.Message = "没有找到对应的工作人员";
                return Ok(msg);

            }
            if (reader.Type == PublicEnum.Type.管理员 | reader.Type == PublicEnum.Type.馆际互借人员)
            {
               
            }
            else
            {
                msg.Code = 3;
                msg.Message = $"该用户{reader.Name}没有馆际互借人员权限";
                return Ok(msg);
            }

                //找到事务
            var apply = _loanWork.FindAsync(x => x.Id == applyId).Result.FirstOrDefault();
            if (apply == null)
            {
                msg.Code = 4;
                msg.Message = "没找到对应申请事务号";
                return Ok(msg);
            }

            //判断是否已经找书成功并入柜
            if (apply.Status == PublicEnum.CirculationStatus.图书已找到)
            {
                msg.Code = 5;
                msg.Message = "该申请已经完成了找书入柜";
                return Ok(msg);
            }

            //建立图书馆人员处理流程节点
            LibraryProcessingInfo libraryProcessingInfo = new LibraryProcessingInfo();


            //开柜自动分配格口
            string url = _baseUrl + $"/api/Locker/OpenEmptyCell?iccid={iccid}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            // 确保HTTP响应状态为200 (OK)
            if (response.IsSuccessStatusCode)
            {
                // 读取响应内容
                string responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true

                    // 其他可能需要的选项
                };
                Msg msgTemp= System.Text.Json.JsonSerializer.Deserialize<Msg>(responseBody,options);

                if (msgTemp.Code == 0)
                {
                    libraryProcessingInfo.LibrarianOpenId = openId;
                    libraryProcessingInfo.LockerNumber =iccid;   //柜子
                    libraryProcessingInfo.CellNumber = msgTemp.Message.Split('|')[1];   //格口
                    apply.Status = PublicEnum.CirculationStatus.图书已找到;    //状态
                    var temp=JsonSerializer.Deserialize<Locker>(msgTemp.Data.ToString(), options);
                    libraryProcessingInfo.LockerDetail = temp;
                }
                else
                {
                    apply.Status = PublicEnum.CirculationStatus.申请终止;
                    libraryProcessingInfo.Remark = msgTemp.Message;
                    msg.Code= 6;
                    msg.Message += msgTemp.Message;
                    return Ok(msg);
                }
            }
            else
            {
                msg.Code = 7;
                msg.Message += $"开柜请求发送错误";
                return Ok(msg);
            }
            //将图书馆取书信息加入到流程节点
            libraryProcessingInfo.LibrarianDetail = await _loanWorkService.getLibrarianDetailAsync(openId);
            
            apply.LibraryProcessing = libraryProcessingInfo;
            //更新事务
           var result=  await _loanWork.ReplaceOneAsync(x => x.Id == applyId, apply);
            if (result.ModifiedCount != 1)
            {
                msg.Code = 8;
                msg.Message = "更新数据不成功";
            }
            else
            {
                msg.Code = 0;
                msg.Message = "找书入柜成功";
                msg.Data = new
                {
                    LibrarianOpenId = libraryProcessingInfo.LibrarianOpenId,
                    LockerNumber = libraryProcessingInfo.LockerNumber,
                    CellNumber = libraryProcessingInfo.CellNumber
                };
            }

            return Ok(msg);
        }

        /// <summary>
        /// 快递员取书准备运输
        /// </summary>
        /// <param name="applyId">申请号</param>
        /// <param name="openId">快递员openid</param>
        /// <param name="remark">标记</param>
        /// <returns></returns>
        [HttpPost]
        [Route("transport")]
        public async Task<IActionResult> transport(string applyId = "67c30b1bf80110a819c18fce", string openId = "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9", string remark="快递员从快递柜取书成功")
        {
            Msg msg = new Msg();
            //校验openId的解密
            var ret =_toolService.DeCryptOpenId(openId);
            if (!ret.Item1)
            {
                msg.Code = 1;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            openId = ret.Item2;

            //判断该人员是否为图书馆找书工作人员
            var reader = _reader.FindAsync<Reader>(x => x.OpenId == openId).Result.FirstOrDefault();
            if (reader == null)
            {
                msg.Code = 2;
                msg.Message = "没有找到对应的工作人员";
                return Ok(msg);

            }
            if (reader.Type == PublicEnum.Type.管理员 | reader.Type == PublicEnum.Type.快递人员)
            {

            }
            else
            {
                msg.Code = 3;
                msg.Message = $"该用户{reader.Name}没有快递人员取书权限";
                return Ok(msg);
            }

            //找到事务
            var apply = _loanWork.FindAsync(x => x.Id == applyId).Result.FirstOrDefault();
            if (apply == null)
            {
                msg.Code = 4;
                msg.Message = "没找到对应申请事务号";
                return Ok(msg);
            }

            //判断是否由快递人员取书并运输中
            if (apply.Status == PublicEnum.CirculationStatus.运输中)
            {
                msg.Code = 5;
                msg.Message = "该申请已经由快递人员取书并运输中";
                return Ok(msg);
            }

            //建立快递人员处理流程节点
            TransportInfo transportInfo = new TransportInfo();

            //打开指定柜门
            string url = _baseUrl + $"/api/Locker/OpenCell?iccid={apply.LibraryProcessing.LockerNumber}&id={apply.LibraryProcessing.CellNumber}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            // 确保HTTP响应状态为200 (OK)
            if (response.IsSuccessStatusCode)
            {
                // 读取响应内容
                string responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                Msg msgTemp = JsonSerializer.Deserialize<Msg>(responseBody, options);
                
                if (msgTemp.Code == 0)
                {
                    apply.Status = PublicEnum.CirculationStatus.运输中;    //状态
                    transportInfo.CourierOpenId = openId;
                }
                else
                {
                    apply.Status = PublicEnum.CirculationStatus.流程异常;
                    transportInfo.Remark = $"快递员开柜返回异常：{msgTemp.Message}";
                    msg.Code = 6;
                    msg.Message +=$" 快递员开柜返回异常：{msgTemp.Message}";
                    return Ok(msg);
                }
            }
            else
            {
                msg.Code = 7;
                msg.Message= $"快递员取书请求发送错误";
                return Ok(msg);
            }

            //将图书馆取书信息加入到流程节点
            transportInfo.CourierDetail=await _loanWorkService.getCourierDetailAsync(openId);
            apply.Transport = transportInfo;
            //更新事务
            var result = await _loanWork.ReplaceOneAsync(x => x.Id == applyId, apply);
            if (result.ModifiedCount != 1)
            {
                msg.Code = 8;
                msg.Message = "更新数据不成功";
            }
            else
            {
                msg.Code = 0;
                msg.Message = "快递员取书成功";
                msg.Data = new
                {
                    LibrarianOpenId = transportInfo.CourierOpenId,
                    Remark= transportInfo.Remark,
                    PickupTime=transportInfo.PickupTime
                };
            }

            return Ok(msg);
        }

        /// <summary>
        /// 快递员运输后存入快递柜
        /// </summary>
        /// <param name="applyId">申请号</param>
        /// <param name="openId">快递员OPENID</param>
        /// <param name="remark">标记</param>
        /// <returns></returns>
        [HttpPost]
        [Route("DestinationLocker")]
        public async Task<IActionResult> DestinationLocker(string applyId = "67c30b1bf80110a819c18fce", string openId = "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9", string remark = "快递员存入快递柜成功")
        {
            Msg msg = new Msg();
            //校验openId的解密
            string sourceOpenId= openId;
            var ret = _toolService.DeCryptOpenId(openId);
            if (!ret.Item1)
            {
                msg.Code = 1;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            openId = ret.Item2;

            //判断该人员是否快递工作人员
            var reader = _reader.FindAsync<Reader>(x => x.OpenId == openId).Result.FirstOrDefault();
            if (reader == null)
            {
                msg.Code = 2;
                msg.Message = "没有找到对应的工作人员";
                return Ok(msg);

            }
            if (reader.Type == PublicEnum.Type.管理员 | reader.Type == PublicEnum.Type.快递人员)
            {

            }
            else
            {
                msg.Code = 3;
                msg.Message = $"该用户{reader.Name}没有快递人员存书权限";
                return Ok(msg);
            }

            //找到事务
            var apply = _loanWork.FindAsync(x => x.Id == applyId).Result.FirstOrDefault();
            if (apply == null)
            {
                msg.Code = 4;
                msg.Message = "没找到对应申请事务号";
                return Ok(msg);
            }

            //判断是否由快递人员取书并运输中
            if (apply.Status == PublicEnum.CirculationStatus.已到达目的地)
            {
                msg.Code = 5;
                msg.Message = "该申请已经由快递人员存放入目的取书柜";
                return Ok(msg);
            }

            //建立快递人员处理流程节点
            DestinationLockerInfo destinationLockerInfo = new DestinationLockerInfo();


            //开柜自动分配格口
            string iccid = apply.Application.DestinationLocker;
            string url = _baseUrl + $"/api/Locker/OpenEmptyCell?iccid={iccid}"; 
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            // 确保HTTP响应状态为200 (OK)
            if (response.IsSuccessStatusCode)
            {
                // 读取响应内容
                string responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                Msg msgTemp = JsonSerializer.Deserialize<Msg>(responseBody, options);

                if (msgTemp.Code == 0)
                {

                    apply.Status = PublicEnum.CirculationStatus.已到达目的地;    //状态
                    destinationLockerInfo.CourierOpenId = openId;
                    destinationLockerInfo.LockerNumber = iccid;
                    destinationLockerInfo.CellNumber = msgTemp.Message.Split('|')[1]; //格口
                    //解析目的快递柜信息
                    var temp = JsonSerializer.Deserialize<Locker>(msgTemp.Data.ToString(), options);
                    destinationLockerInfo.LockerDetail = temp;
                    destinationLockerInfo.Remark = remark;
                }
                else
                {
                    apply.Status = PublicEnum.CirculationStatus.流程异常;
                    destinationLockerInfo.Remark = $"快递员存入开柜返回异常：{msgTemp.Message}";
                    msg.Code = 6;
                    msg.Message += $" 快递员存入开柜返回异常：{msgTemp.Message}";
                    return Ok(msg);
                }
            }
            else
            {
                msg.Code = 7;
                msg.Message = $"快递员存入请求发送错误";
                return Ok(msg);
            }

            //将图书馆取书信息加入到流程节点
            destinationLockerInfo.CourierDetail = await _loanWorkService.getCourierDetailAsync(openId);

            apply.DestinationLocker = destinationLockerInfo;
            //更新事务
            var result = await _loanWork.ReplaceOneAsync(x => x.Id == applyId, apply);
            if (result.ModifiedCount != 1)
            {
                msg.Code = 8;
                msg.Message = "更新数据不成功";
            }
            else
            {
                msg.Code = 0;
                msg.Message = "快递员存入图书成功";
                msg.Data = new
                {
                    LibrarianOpenId = destinationLockerInfo.CourierOpenId,
                    Remark = destinationLockerInfo.Remark,
                    DepositTime = destinationLockerInfo.DepositTime,
                    LockerNumber = destinationLockerInfo.LockerNumber,
                    CellNumber = destinationLockerInfo.CellNumber
                };
                /*
                学校：{ { keyword1.DATA} }
                通知人：{ { keyword2.DATA} }
                时间：{ { keyword3.DATA} }
                通知内容：{ { keyword4.DATA} }
                */

                //发送微信通知
                if (destinationLockerInfo.CellNumber!= null)
                {
                    var notice = new
                    {
                        keyword1 = new { value = $"{destinationLockerInfo.LockerDetail.Owner}:{destinationLockerInfo.LockerDetail.Location}" },  //学校
                        keyword2 = new { value = $"快递员联系方式：{destinationLockerInfo.CourierDetail.name}:{destinationLockerInfo.CourierDetail.phone}" },  //通知人，快递员电话
                        keyword3 = new { value = $"{destinationLockerInfo.DepositTime?.ToString("yyyy-MM-dd")}" },   // 时间
                        keyword4 = new { value = $"{destinationLockerInfo.LockerDetail.Area}: {destinationLockerInfo.CellNumber}" },   //取货的地点
                       
                    };
                    await _weChatService.SendTemplateMessageAsync("notice", openId, $"https://reader.yangtzeu.edu.cn/wechat/my?openId={sourceOpenId}", notice);
                }

            }

            return Ok(msg);
        }

        /// <summary>
        /// 申请者取书
        /// </summary>
        /// <param name="applyId">事务号</param>
        /// <param name="openId">读者OPENDI</param>
        /// <param name="remark">留言</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Pickup")]
        public async Task<IActionResult> Pickup(string applyId = "67c30b1bf80110a819c18fce", string openId = "rfWTm26wJZnpQ+S0JgywkZPQUU59YMTdoGVAEzPnfC2epRw1ZuI8EqIjbGPV8by9", string remark = "读者取书成功")
        {
            Msg msg = new Msg();
            //校验openId的解密
            
            var ret = _toolService.DeCryptOpenId(openId);
            if (!ret.Item1)
            {
                msg.Code = 1;
                msg.Message = $"OpenId解密失败";
                return Ok(msg);
            }
            openId = ret.Item2;

            //判断该人员是否为注册读者
            var reader = _reader.FindAsync<Reader>(x => x.OpenId == openId).Result.FirstOrDefault();
            if (reader == null)
            {
                msg.Code = 2;
                msg.Message = "没有找到对应的人员";
                return Ok(msg);

            }
         

            //找到事务
            var apply = _loanWork.FindAsync(x => x.Id == applyId).Result.FirstOrDefault();
            if (apply == null)
            {
                msg.Code = 4;
                msg.Message = "没找到对应申请事务号";
                return Ok(msg);
            }

            //判断申请者是否已经取出图书
            if (apply.Status == PublicEnum.CirculationStatus.已完成取书)
            {
                msg.Code = 5;
                msg.Message = "该申请已经由借阅者取书成功";
                return Ok(msg);
            }

            //判断是否由申请者本人取书
            if(openId != apply.Application.ReaderOpenId)
            {
                msg.Code = 6;
                msg.Message = "该申请不是由本人取书";
                return Ok(msg);
            }

            //建立取书人处理流程节点
            PickupInfo pickupInfo = new PickupInfo();

            //打开指定格口
            string url = _baseUrl + $"/api/Locker/OpenCell?iccid={apply.LibraryProcessing.LockerNumber}&id={apply.LibraryProcessing.CellNumber}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            // 确保HTTP响应状态为200 (OK)
            if (response.IsSuccessStatusCode)
            {
                // 读取响应内容
                string responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                Msg msgTemp = System.Text.Json.JsonSerializer.Deserialize<Msg>(responseBody, options);

                if (msgTemp.Code == 0)
                {

                    apply.Status = PublicEnum.CirculationStatus.已完成取书;    //状态
                    pickupInfo.ReaderOpenId = openId;
                    pickupInfo.LockerNumber = apply.DestinationLocker?.LockerNumber;
                    pickupInfo.CellNumber = msgTemp.Message.Split('|')[1]; //格口
                    pickupInfo.Remark = remark;
                }
                else
                {
                    apply.Status = PublicEnum.CirculationStatus.流程异常;
                    pickupInfo.Remark = $"申请者取书返回异常：{msgTemp.Message}";
                    msg.Code = 6;
                    msg.Message += $"申请者取书返回异常：{msgTemp.Message}";
                    return Ok(msg);
                }
            }
            else
            {
                msg.Code = 7;
                msg.Message = $"申请者取书请求发送错误";
                return Ok(msg);
            }

            //将图书馆取书信息加入到流程节点
            apply.Pickup= pickupInfo;
            //更新事务
            var result = await _loanWork.ReplaceOneAsync(x => x.Id == applyId, apply);
            if (result.ModifiedCount != 1)
            {
                msg.Code = 8;
                msg.Message = "更新数据不成功";
            }
            else
            {
                msg.Code = 0;
                msg.Message = "申请者取书成功";
                msg.Data = new
                {
                    ReaderOpenId =await _loanWorkService.getReaderDetailAsync( pickupInfo.ReaderOpenId), 
                    Remark = pickupInfo.Remark,
                    PickupTime = pickupInfo.PickupTime,
                    LockerNumber = pickupInfo.LockerNumber,
                    CellNumber = pickupInfo.CellNumber
                };
            }

            return Ok(msg);
        }
    }
}
