using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Models;
using Motel.ViewModels.User;


namespace Motel.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // ================= LIST =================

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10;

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.PhoneNumber.Contains(search));
            }

            int totalItems = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var vm = new UserListViewModel
            {
                Users = users,
                Search = search,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View(vm);
        }

        // ================= EDIT =================

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            var vm = new UserEditViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber!,
                CreatedAt = user.CreatedAt,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null) return NotFound();

            // Update phone
            user.PhoneNumber = model.PhoneNumber;
            // Update lock status
            if (model.IsLocked)
                user.LockoutEnd = DateTimeOffset.MaxValue;
            else
                user.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(model);
            }

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!passResult.Succeeded)
                {
                    foreach (var e in passResult.Errors)
                        ModelState.AddModelError("", e.Description);

                    return View(model);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}