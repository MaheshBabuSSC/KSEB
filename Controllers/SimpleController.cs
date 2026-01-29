using KSEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KSEB.Controllers
{
    [Route("[controller]")]
    public class SimpleController : Controller
    {
        private readonly AppDbContext _context;

        public SimpleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Simple
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Simple/Create
        [HttpPost("Create")]
        public IActionResult Create(string name, string email)
        {
            try
            {
                // Simple validation
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    ViewBag.Error = "Name and Email are required!";
                    return View("Index");
                }

                // Insert into database
                var sql = "INSERT INTO simple_users (name, email) VALUES ({0}, {1}) RETURNING id";

                var userId = _context.Database
                    .SqlQueryRaw<int>(sql, name, email)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (userId > 0)
                {
                    ViewBag.Message = $"✅ User created successfully! ID: {userId}";
                }
                else
                {
                    ViewBag.Error = "❌ Failed to create user";
                }

                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"❌ Error: {ex.Message}";
                return View("Index");
            }
        }


    }
}