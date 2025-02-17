using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;

namespace SolidarityBookCatalog.Controllers
{
    public class ManagerController : Controller
    {
        private readonly IMongoCollection<Manager> _managers;
        private readonly IMongoCollection<User> _users;
        private readonly ILogger<ManagerController> _logger;
        private readonly IConfiguration _configuration;
        public ManagerController(IConfiguration configuration,IMongoClient mongoClient,ILogger<ManagerController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            var database = mongoClient.GetDatabase("BookReShare");
            _managers = database.GetCollection<Manager>("managers");
            _users = database.GetCollection<User>("users");
        }
        [HttpGet]
        [Route("fun")]
        public async  Task<ActionResult> fun(string act,string pars)
        {
            Msg msg= new Msg();
            switch (act)
            {
                case "index":
                    // 唯一用户名索引（组合索引）
                    var userNameIndex = new CreateIndexModel<Manager>(
                        Builders<Manager>.IndexKeys.Ascending(u => u.UserName),
                        new CreateIndexOptions { Unique = true }
                    );
                    await _managers.Indexes.CreateOneAsync(userNameIndex);
                    break;
            }
            return Ok(msg);
        }

        // GET: ManagerController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ManagerController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ManagerController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ManagerController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ManagerController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ManagerController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ManagerController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
