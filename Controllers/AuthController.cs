using KSEB.Models;
using KSEB.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace KSEB.Controllers
{
    [Route("")]  // Changed from "[controller]" to "" - this removes /Auth/ prefix
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: / (root) - Show login page
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            // If already logged in, redirect to Dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect("/Dashboard");
            }

            return View("~/Views/KSEB/Index.cshtml");
        }

        // GET: /Login - Show login form (alias for Index)
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return Redirect("/");
        }

        // POST: /Login - Process login
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("~/Views/KSEB/Index.cshtml", model);
                }

                // Validate credentials
                var userId = _authService.ValidateLogin(model.Email, model.Password);

                if (userId > 0)
                {
                    // Get user details
                    var user = _authService.GetUserByEmail(model.Email);
                    if (user == null)
                    {
                        ViewBag.Error = "User details not found!";
                        return View("~/Views/KSEB/Index.cshtml", model);
                    }

                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Role, user.RoleName ?? "User"),
                        new Claim("UserId", user.UserId.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    // Sign in
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation($"User {user.Email} logged in successfully.");

                    // Redirect to /Dashboard
                    return Redirect("/Dashboard");
                }
                else
                {
                    ViewBag.Error = "Invalid email or password!";
                    return View("~/Views/KSEB/Index.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", model.Email);
                ViewBag.Error = $"Login error: {ex.Message}";
                return View("~/Views/KSEB/Index.cshtml", model);
            }
        }

        // GET: /Dashboard - Protected dashboard page
        [Authorize]
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            // Get user info from claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Pass data to view
            ViewBag.UserId = userId;
            ViewBag.UserName = userName;
            ViewBag.UserEmail = userEmail;
            ViewBag.UserRole = userRole;

            return View("~/Views/KSEB/Dashboard.cshtml");
        }

        // GET: /Logout
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out.");
            return Redirect("/");
        }

        // GET: /Register - Show registration form
        [HttpGet("Register")]
        public IActionResult Register()
        {
            var model = new RegisterModel
            {
                Roles = _authService.GetActiveRoles()
            };
            return View("~/Views/KSEB/NewUser.cshtml", model);
        }

        // POST: /Register - Process registration
        [HttpPost("Register")]
        public IActionResult Register(RegisterModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.Roles = _authService.GetActiveRoles();
                    return View("~/Views/KSEB/NewUser.cshtml", model);
                }

                if (model.RoleId <= 0)
                {
                    ViewBag.Error = "Please select a role!";
                    model.Roles = _authService.GetActiveRoles();
                    return View("~/Views/KSEB/NewUser.cshtml", model);
                }

                var createdBy = User.Identity?.Name ?? "Anonymous";
                var userId = _authService.RegisterUser(model, createdBy);

                if (userId > 0)
                {
                    ViewBag.Message = $"✅ User registered successfully! User ID: {userId}";

                    var newModel = new RegisterModel
                    {
                        Roles = _authService.GetActiveRoles()
                    };
                    return View("~/Views/KSEB/NewUser.cshtml", newModel);
                }
                else
                {
                    ViewBag.Error = "❌ Registration failed!";
                    model.Roles = _authService.GetActiveRoles();
                    return View("~/Views/KSEB/NewUser.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"❌ Error: {ex.Message}";
                model.Roles = _authService.GetActiveRoles();
                return View("~/Views/KSEB/NewUser.cshtml", model);
            }
        }
    }
}