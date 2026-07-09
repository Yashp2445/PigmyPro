using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> _loginAttempts = new();

        public AuthController(IUserRepository userRepo, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View(new LoginVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var usernameKey = vm.Username.ToLowerInvariant();
            if (_loginAttempts.TryGetValue(usernameKey, out var attempt))
            {
                if (attempt.Count >= 5 && (DateTime.UtcNow - attempt.LastAttempt).TotalMinutes < 15)
                {
                    ModelState.AddModelError("", "Account temporarily locked due to too many failed attempts. Please try again later.");
                    return View(vm);
                }
            }

            
            var admin = await _userRepo.GetAdminCredentialsAsync(vm.Username);

            if (admin != null)
            {
                bool isValid = false;

                try
                {
                    isValid = BCrypt.Net.BCrypt.Verify(vm.Password, admin.Value.PasswordHash);
                }
                catch { }

                if (isValid)
                {
                    _loginAttempts.TryRemove(usernameKey, out _);
                    return await SignInUser(
                        username: admin.Value.Username,
                        role: AppRoles.SuperAdmin,
                        displayName: "Super Admin",
                        userId: 0,
                        bankId: 0,
                        branchId: 0,
                        rememberMe: vm.RememberMe
                    );
                }
            }

            
            var user = await _userRepo.GetByUsernameAsync(vm.Username);

            if (user != null)
            {
                bool isPasswordValid = false;

                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(vm.Password, user.PasswordHash);
                }
                catch { }

                if (!isPasswordValid)
                {
                    HandleFailedLogin(usernameKey);
                    ModelState.AddModelError("", "Invalid Username or Password.");
                    return View(vm);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "Your account has been deactivated.");
                    return View(vm);
                }

                _loginAttempts.TryRemove(usernameKey, out _);
                return await SignInUser(
                    username: user.Username,
                    role: user.Role,
                    displayName: user.Name ?? user.Username,
                    userId: user.UserID,
                    bankId: user.BankID,
                    branchId: user.BranchID ?? 0,
                    rememberMe: vm.RememberMe
                );
            }

            HandleFailedLogin(usernameKey);
            ModelState.AddModelError("", "Invalid Username or Password.");
            return View(vm);
        }

        private void HandleFailedLogin(string usernameKey)
        {
            _loginAttempts.AddOrUpdate(usernameKey, 
                _ => (1, DateTime.UtcNow), 
                (_, current) => (current.Count >= 5 && (DateTime.UtcNow - current.LastAttempt).TotalMinutes >= 15 ? 1 : current.Count + 1, DateTime.UtcNow));
        }

        
        private async Task<IActionResult> SignInUser(
            string username,
            string role,
            string displayName,
            int userId,
            int bankId,
            int branchId,
            bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("UserID", userId.ToString()),
                new Claim("BankID", bankId.ToString()),
                new Claim("BranchID", branchId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("DisplayName", displayName)
            };

            var keyString = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing from config");
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(keyString));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var expires = rememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(30);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = expires
            };

            Response.Cookies.Append("PigmyPro.Token", tokenString, cookieOptions);

            
            HttpContext.Session.SetInt32("BankID", bankId);
            HttpContext.Session.SetInt32("BranchID", branchId);
            HttpContext.Session.SetString("UserRole", role);

            return role switch
            {
                AppRoles.SuperAdmin => RedirectToAction("Index", "Dashboard"),
                AppRoles.BankAdmin => RedirectToAction("Index", "Dashboard"),
                AppRoles.BranchAdmin => RedirectToAction("Index", "Dashboard"),
                _ => RedirectToAction("Index", "Dashboard")
            };
        }

        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("PigmyPro.Token");
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult KeepAlive() => Ok();
    }
}