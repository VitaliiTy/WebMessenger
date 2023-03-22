using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebMessenger.Data;
using WebMessenger.Models;

namespace WebMessenger.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        public IActionResult Login(string returnUrl)
        {
            ViewData["returnUrl"] = returnUrl;
            return View();
        }

        [HttpPost("login")]
        public async Task<IActionResult> ValidateLogin(string username, string password, string returnUrl)
        {
            WebMessengerContext db = new WebMessengerContext();
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/Home/Index";
            }
            ViewData["returnUrl"] = returnUrl;


            if (CredentialsAreValid(username, password, db))
            {
                var claims = new List<Claim>();
                claims.Add(new Claim("username", username));
                claims.Add(new Claim(ClaimTypes.Name, username));
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimPrinceple = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrinceple);
                return Redirect(returnUrl);
            }


            TempData["Error"] = "Username or Password is invailid";
            return View("login");
        }

        public bool CredentialsAreValid(string username, string password, WebMessengerContext db)
        {
            var people = db.AspNetUsers.Where(a => a.UserName == username).ToList();
            foreach (var user in people)
            {
                if (user.PasswordHash == password)
                {
                    return true;
                }
            }
            return false;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("register")]
        public async Task<IActionResult> ValidateRegister(string email, string username, string password, string confirmedPassword)
        {
            WebMessengerContext db = new WebMessengerContext();

            if (password != confirmedPassword)
            {
                TempData["Error"] = "Passwords do not match";
                return View("register");
            }
            
            if (ExistingEmail(email, db))
            {
                TempData["Error"] = "User is already register";
                return View("register");
            }
            
            if (ExistingUsername(username, db))
            {
                TempData["Error"] = "Username already exist";
                return View("register");
            }

            db.AspNetUsers.Add(new AspNetUser()
            {
                Id = db.AspNetUsers.Max(a => a.Id) + 1,
                UserName = username,
                Email = email,
                PasswordHash = password,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 10
            });
            db.SaveChanges();

            var claims = new List<Claim>();
            claims.Add(new Claim("username", username));
            claims.Add(new Claim(ClaimTypes.Name, username));
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrinceple = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrinceple);
            return Redirect("/");
        }

        private bool ExistingUsername(string username, WebMessengerContext db)
        {
            var people = db.AspNetUsers.Where(a => a.UserName == username).ToList();
            if (people.Any())
            {
                return true;
            }
            return false;
        }

        private bool ExistingEmail(string email, WebMessengerContext db)
        {
            var people = db.AspNetUsers.Where(a => a.Email == email).ToList();
            if (people.Any())
            {
                return true;
            }
            return false;
        }



    }
}
