//using KSEB.Models;
//using KSEB.Services;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;
//using System.Text.Json;

//namespace KSEB.Controllers
//{
//    [Route("[controller]")]
//    public class KSEBController : Controller
//    {
//        private readonly FormService _formService;
//        private readonly AuthService _authService;

//        public KSEBController(AuthService authService, FormService formService)
//        {
//            _authService = authService;
//            _formService = formService;
//        }

//        // GET: /KSEB or /KSEB/Index
//        [HttpGet]
//        [HttpGet("Index")]
//        public IActionResult Index()
//        {
//            return View();
//        }

//        // POST: /KSEB/Index (Login)
//        [HttpPost("Index")]
//        //public async Task<IActionResult> Index(LoginViewModel model)
//        //{
//        //    var userId = _authService.ValidateLogin(model.Email, model.Password);

//        //    if (userId == 0)
//        //    {
//        //        ViewBag.Error = "Invalid email or password";
//        //        return View(model);
//        //    }

//        //    var claims = new List<Claim>
//        //    {
//        //        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
//        //        new Claim(ClaimTypes.Email, model.Email),
//        //        new Claim(ClaimTypes.Name, model.Email)
//        //    };

//        //    var identity = new ClaimsIdentity(
//        //        claims,
//        //        CookieAuthenticationDefaults.AuthenticationScheme
//        //    );

//        //    await HttpContext.SignInAsync(
//        //        CookieAuthenticationDefaults.AuthenticationScheme,
//        //        new ClaimsPrincipal(identity),
//        //        new AuthenticationProperties
//        //        {
//        //            IsPersistent = true,
//        //            ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
//        //        }
//        //    );

//        //    // Store userId in session for easy access
//        //    HttpContext.Session.SetInt32("UserId", userId);
//        //    HttpContext.Session.SetString("UserEmail", model.Email);

//        //    // Get forms for sidebar
//        //    var forms = _formService.GetForms();

//        //    // Use TempData for redirect
//        //    TempData["SidebarForms"] = JsonSerializer.Serialize(forms);
//        //    TempData.Keep("SidebarForms");

//        //    return RedirectToAction("Dashboard", "KSEB");
//        //}

//        // GET: /KSEB/Dashboard (Protected)
//        [Authorize]
//        [HttpGet("Dashboard")]
//        public IActionResult Dashboard()
//        {
//            var userId = HttpContext.Session.GetInt32("UserId");
//            var userEmail = HttpContext.Session.GetString("UserEmail");

//            if (userId == null || string.IsNullOrEmpty(userEmail))
//            {
//                return RedirectToAction("Index");
//            }

//            ViewBag.UserId = userId;
//            ViewBag.UserEmail = userEmail;

//            // Get forms for display
//            var forms = _formService.GetForms();
//            return View(forms);
//        }

//        // GET: /KSEB/Forms
//        [Authorize]
//        [HttpGet("Forms")]
//        public IActionResult Forms()
//        {
//            var forms = _formService.GetForms();
//            return View(forms);
//        }

//        // GET: /KSEB/CreateForm
//        [Authorize]
//        [HttpGet("CreateForm")]
//        public IActionResult CreateForm()
//        {
//            return View();
//        }

//        // POST: /KSEB/CreateForm
//        [Authorize]
//        [HttpPost("CreateForm")]
//        public IActionResult CreateForm(CreateFormRequest request)
//        {
//            if (!ModelState.IsValid)
//            {
//                return View(request);
//            }

//            try
//            {
//                // Get current user from session
//                var createdBy = HttpContext.Session.GetString("UserEmail") ??
//                               User.Identity?.Name ?? "Anonymous";

//                // Call service
//                _formService.CreateForm(
//                    request.FormTitle,
//                    request.FormDescription ?? "",
//                    createdBy,
//                    request.Fields ?? new List<FormField>()
//                );

//                TempData["Message"] = "Form created successfully!";
//                return RedirectToAction("Forms");
//            }
//            catch (Exception ex)
//            {
//                ModelState.AddModelError("", $"Error creating form: {ex.Message}");
//                return View(request);
//            }
//        }

//        // GET: /KSEB/Logout
//        [HttpGet("Logout")]
//        public async Task<IActionResult> Logout()
//        {
//            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
//            HttpContext.Session.Clear();
//            return RedirectToAction("Index");
//        }

//        // GET: /KSEB/Test (For testing only)
//        [HttpGet("Test")]
//        public IActionResult Test()
//        {
//            return Content("✅ KSEB Controller is working!", "text/html");
//        }

//        // GET: /KSEB/DbCheck (Database test)
//        [HttpGet("DbCheck")]
//        public IActionResult DbCheck()
//        {
//            try
//            {
//                // Test database by getting forms count
//                var forms = _formService.GetForms();
//                return Content($"✅ Database connection is working!<br>" +
//                              $"Total forms in database: {forms.Count}<br>" +
//                              $"<a href='/KSEB'>Go to Login</a>", "text/html");
//            }
//            catch (Exception ex)
//            {
//                return Content($"❌ Database error: {ex.Message}", "text/html");
//            }
//        }
//    }
//}