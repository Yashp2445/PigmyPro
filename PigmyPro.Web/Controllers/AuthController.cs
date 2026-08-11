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
        private readonly IBankRepository _bankRepo;
        private readonly IConfiguration _configuration;
        private readonly PigmyPro.Data.Context.DapperContext _dapperContext;

        public AuthController(IUserRepository userRepo, IBankRepository bankRepo, IConfiguration configuration, PigmyPro.Data.Context.DapperContext dapperContext)
        {
            _userRepo = userRepo;
            _bankRepo = bankRepo;
            _configuration = configuration;
            _dapperContext = dapperContext;
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
            
            int maxAttempts = 3;
            int blockTimeSeconds = 300;
            
            try
            {
                using var connection = _dapperContext.CreateConnection();
                var pwdSettings = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<dynamic>(connection, "SELECT TOP 1 Wrong_Login_Attempt, Wrong_Login_Block_Time FROM passward_Setting");
                if (pwdSettings != null)
                {
                    maxAttempts = pwdSettings.Wrong_Login_Attempt != null ? Convert.ToInt32(pwdSettings.Wrong_Login_Attempt) : maxAttempts;
                    blockTimeSeconds = pwdSettings.Wrong_Login_Block_Time != null ? Convert.ToInt32(pwdSettings.Wrong_Login_Block_Time) : blockTimeSeconds;
                }
            }
            catch { } 

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
                    return await SignInUser(
                        username: admin.Value.Username,
                        role: AppRoles.SuperAdmin,
                        displayName: "Super Admin",
                        userId: 0,
                        bankId: 0,
                        branchId: 0,
                        hasCBS: 'N',
                        rememberMe: vm.RememberMe
                    );
                }
            }

            var user = await _userRepo.GetByUsernameAsync(vm.Username);

            if (user != null)
            {
                if (user.Lock_Expiry_Time.HasValue)
                {
                    if (user.Lock_Expiry_Time.Value > DateTime.Now)
                    {
                        ViewBag.LockedUsername = vm.Username;
                        ViewBag.LockExpiryTime = user.Lock_Expiry_Time.Value.ToString("o");
                        var remainingTime = user.Lock_Expiry_Time.Value - DateTime.Now;
                        string timeString = remainingTime.TotalMinutes >= 1 ? $"{(int)Math.Ceiling(remainingTime.TotalMinutes)} minute(s)" : $"{remainingTime.Seconds} second(s)";
                        ModelState.AddModelError("Locked", $"Account temporarily locked due to too many failed attempts. Please try again in {timeString}.");
                        return View(vm);
                    }
                    else
                    {
                        user.Wrong_Login_Attempt = 0;
                        user.Lock_Expiry_Time = null;
                        await _userRepo.UpdateLoginAttemptAsync(user.UserID, 0, null);
                    }
                }

                bool isPasswordValid = false;

                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(vm.Password, user.PasswordHash);
                }
                catch { }

                if (!isPasswordValid)
                {
                    int currentAttempts = (user.Wrong_Login_Attempt ?? 0) + 1;
                    if (currentAttempts >= maxAttempts)
                    {
                        var lockTime = DateTime.Now.AddSeconds(blockTimeSeconds);
                        await _userRepo.UpdateLoginAttemptAsync(user.UserID, currentAttempts, lockTime);
                        ViewBag.LockedUsername = vm.Username;
                        ViewBag.LockExpiryTime = lockTime.ToString("o");
                        string timeString = blockTimeSeconds >= 60 ? $"{blockTimeSeconds / 60} minute(s)" : $"{blockTimeSeconds} second(s)";
                        ModelState.AddModelError("Locked", $"Account temporarily locked due to too many failed attempts. Please try again in {timeString}.");
                        return View(vm);
                    }
                    else
                    {
                        await _userRepo.UpdateLoginAttemptAsync(user.UserID, currentAttempts, null);
                        int attemptsRemaining = maxAttempts - currentAttempts;
                        TempData["ToasterMessage"] = $"Invalid Username or Password. {attemptsRemaining} attempt(s) remaining.";
                        ModelState.AddModelError("", $"Invalid Username or Password. {attemptsRemaining} attempt(s) remaining.");
                        return View(vm);
                    }
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "Your account has been deactivated.");
                    return View(vm);
                }

                if ((user.Wrong_Login_Attempt ?? 0) > 0)
                {
                    await _userRepo.UpdateLoginAttemptAsync(user.UserID, 0, null);
                }

                char hasCBS = 'N';
                if (user.BankID > 0)
                {
                    var bank = await _bankRepo.GetByIdAsync(user.BankID);
                    if (bank != null)
                    {
                        if (!bank.ActiveYN)
                        {
                            ModelState.AddModelError("", "Your bank has been deactivated. Please contact support.");
                            return View(vm);
                        }
                        hasCBS = bank.hasCBS;
                    }
                }

                return await SignInUser(
                    username: user.Username,
                    role: user.Role,
                    displayName: user.Name ?? user.Username,
                    userId: user.UserID,
                    bankId: user.BankID,
                    branchId: user.BranchID ?? 0,
                    hasCBS: hasCBS,
                    rememberMe: vm.RememberMe
                );
            }

            ModelState.AddModelError("", "Invalid Username or Password.");
            return View(vm);
        }

        
        private async Task<IActionResult> SignInUser(
            string username,
            string role,
            string displayName,
            int userId,
            int bankId,
            int branchId,
            char hasCBS,
            bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("UserID", userId.ToString()),
                new Claim("BankID", bankId.ToString()),
                new Claim("BranchID", branchId.ToString()),
                new Claim("HasCBS", hasCBS.ToString()),
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
            HttpContext.Session.SetString("HasCBS", hasCBS.ToString());

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

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateResetOtp([FromForm] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Json(new { success = false, message = "Username is required." });

            var (msg, otp) = await _userRepo.GenerateResetOtpAsync(username);

            if (msg == "Mobile Number Not Found")
            {
                return Json(new { success = false, message = "Mobile number not found. Please contact your branch administrator to register your mobile number." });
            }

            if (msg == "OTP already sent, please wait")
            {
                return Json(new { success = false, message = "OTP already sent. Please wait 60 seconds before requesting a new one." });
            }

            // Successfully generated, clear any previous session tracking
            HttpContext.Session.Remove($"OtpAttempts_{username}");
            HttpContext.Session.Remove($"OtpUsed_{username}");

            return Json(new { success = true, message = "If this username exists, an OTP has been sent to the registered mobile number." });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyResetOtp([FromForm] string username, [FromForm] string otp)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(otp))
                return Json(new { success = false, message = "Username and OTP are required." });

            var isUsed = HttpContext.Session.GetInt32($"OtpUsed_{username}") == 1;
            if (isUsed)
                return Json(new { success = false, message = "OTP has already been used." });

            var attempts = HttpContext.Session.GetInt32($"OtpAttempts_{username}") ?? 0;
            if (attempts >= 5)
                return Json(new { success = false, message = "Too many failed attempts. Please request a new OTP." });

            var (msg, valid) = await _userRepo.VerifyResetOtpAsync(username, otp);

            if (!valid)
            {
                attempts++;
                HttpContext.Session.SetInt32($"OtpAttempts_{username}", attempts);

                if (msg == "No OTP request found" || msg == "OTP has expired")
                {
                    return Json(new { success = false, message = msg + ". Please request a new one." });
                }
                
                return Json(new { success = false, message = "Invalid OTP." });
            }

            // Success - give a short-lived token to allow step 3, mark as used in session
            HttpContext.Session.SetInt32($"OtpUsed_{username}", 1);
            string token = Guid.NewGuid().ToString("N");
            TempData["ResetToken_" + username] = token;

            return Json(new { success = true, token = token });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordWithOtp([FromForm] string username, [FromForm] string newPassword, [FromForm] string confirmPassword, [FromForm] string token)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
                return Json(new { success = false, message = "Invalid input or passwords do not match." });

            var expectedToken = TempData["ResetToken_" + username]?.ToString();
            if (string.IsNullOrEmpty(expectedToken) || expectedToken != token)
                return Json(new { success = false, message = "Invalid or expired session. Please verify your OTP again." });

            // Keep the token alive in case of validation failure below
            TempData["ResetToken_" + username] = expectedToken;

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
                return Json(new { success = false, message = "New password cannot be the same as the old password." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.Entry_Date = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            await _userRepo.LogPasswordChangeAsync(username, "Self-service OTP reset", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            TempData.Remove("ResetToken_" + username);

            return Json(new { success = true });
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            var username = User.Identity?.Name;
            ViewBag.Username = username;
            
            return View(new ChangePasswordVM());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");
            
            ViewBag.Username = username;

            if (!ModelState.IsValid) return View(vm);

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null) return RedirectToAction("Login");

            // Prevent reuse
            if (BCrypt.Net.BCrypt.Verify(vm.NewPassword, user.PasswordHash))
            {
                ModelState.AddModelError("NewPassword", "New password cannot be the same as the old password.");
                return View(vm);
            }

            // Update user
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            user.Entry_Date = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            // Log the change reason
            await _userRepo.LogPasswordChangeAsync(username, vm.Reason, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
            TempData["SuccessMessage"] = "Password changed successfully! Please log in with your new password.";
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult KeepAlive() => Ok();
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CheckDbConnection()
        {
            try
            {
                using var connection = _dapperContext.CreateConnection();
                if (connection is System.Data.Common.DbConnection dbConnection)
                {
                    await dbConnection.OpenAsync();
                }
                else
                {
                    connection.Open();
                }
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }
    }
}
