using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Motel.Models;
using Motel.Data;
using Motel.ViewModels.Account;
using System.Text;
using System.Text.Encodings.Web;
using System.Security.Claims;

namespace Motel.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MotelDbContext _db;
        private readonly IEmailSender _emailSender;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            MotelDbContext db,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
            _emailSender = emailSender;
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]     
        public async Task<IActionResult> Login(string email, string password, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa bởi quản trị viên.");
                return View();
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Redirect theo role
            if (roles.Contains("Admin"))
                return RedirectToAction("Index", "User");

            if (roles.Contains("Landlord"))
                return RedirectToAction("Index", "Dashboard");

            return RedirectToAction("Index", "Home");
        }

        // ================= PROFILE =================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var roles = await _userManager.GetRolesAsync(user);
            var landlord = await _db.Landlords.FirstOrDefaultAsync(l => l.UserId == user.Id && !l.IsDeleted);

            var vm = new ProfileViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault(),
                LandlordDisplayName = landlord?.DisplayName
            };
            return View(vm);
        }

        // ================= LOGOUT =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ================= REGISTER =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var exist = await _userManager.FindByEmailAsync(model.Email);
            if (exist != null)
            {
                ModelState.AddModelError("", "Email đã tồn tại!");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Fullname,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(model);
            }

            // ADD ROLE
            await _userManager.AddToRoleAsync(user, "Landlord");

            // tạo landlord
            _db.Landlords.Add(new Landlord
            {
                UserId = user.Id,
                DisplayName = model.Fullname,
                IsDeleted = false
            });

            await _db.SaveChangesAsync();

            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("Index", "Dashboard");
        }
        // ================= FORGOT PASSWORD =================

        [HttpGet]
        public IActionResult Forgot()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var encodedToken = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(token));

                var resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { userId = user.Id, token = encodedToken },
                    protocol: Request.Scheme);

                var html = $@"
                <p>Bạn vừa yêu cầu đặt lại mật khẩu.</p>
                <p>Nhấn vào link sau để đặt lại mật khẩu:</p>
                <p><a href='{HtmlEncoder.Default.Encode(resetLink!)}'>Reset mật khẩu</a></p>
                <p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";

                await _emailSender.SendEmailAsync(user.Email!, "Reset mật khẩu Motel", html);
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ================= RESET PASSWORD =================

        [HttpGet]
        public IActionResult ResetPassword(int userId, string token)
        {
            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                token = token
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());

            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(model.token));

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                model.password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(model);
            }

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return Challenge(properties, provider);
        }
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError("", "Google login error");
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
                return RedirectToAction(nameof(Login));

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false);

            if (signInResult.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa bởi quản trị viên.");
                return View("Login");
            }

            if (signInResult.Succeeded)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Lấy thông tin từ Google
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (email == null)
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByEmailAsync(email);

            // Nếu user đã bị khóa
            if (user != null && await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa bởi quản trị viên.");
                return View("Login");
            }

            // Nếu user chưa tồn tại
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name ?? email,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                    return RedirectToAction(nameof(Login));

                // ADD ROLE
                await _userManager.AddToRoleAsync(user, "Landlord");

                _db.Landlords.Add(new Landlord
                {
                    UserId = user.Id,
                    DisplayName = user.FullName,
                    IsDeleted = false
                });

                await _db.SaveChangesAsync();
            }

            // gắn login google
            await _userManager.AddLoginAsync(user, info);

            // login hệ thống
            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("Index", "Dashboard");
        }
    }
}