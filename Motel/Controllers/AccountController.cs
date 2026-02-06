using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Motel.Models;
using Motel.Data;
using Motel.ViewModels.Account;
namespace Motel.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MotelDbContext _db;
        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, MotelDbContext db)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
        }

        // GET: AccountController
        public ActionResult Login(string? ReturnUrl = null)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }
        // POST: AccountController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(string email, string password, string? ReturnUrl)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu");
                return View();
            }
            var user = await _userManager.FindByNameAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại!");
                return View();
            }
            var result = await _signInManager.PasswordSignInAsync(
                user, password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Sai mật khẩu");
                return View();
            }
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return Redirect(ReturnUrl);
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) { return View(model); }
            var exist = await _userManager.FindByEmailAsync(model.Email);
            if (exist != null) { ModelState.AddModelError("", "Email da ton tai!"); return View(model); }

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.Fullname, PhoneNumber = model.PhoneNumber, EmailConfirmed = true, CreatedAt = DateTime.Now };
            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(model);
            }
            _db.Landlords.Add(new Landlord
            {
                UserId = user.Id,
                DisplayName = model.Fullname,
                Address = null,
                IsDeleted = false
            });

            await _db.SaveChangesAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }
    

    }
}
