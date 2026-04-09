using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using System.Security.Claims;

namespace Motel.Helpers
{
    /// <summary>
    /// Helper class for Landlord-related operations
    /// Provides reusable methods for getting landlord information from authenticated users
    /// </summary>
    public class LandlordHelper
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MotelDbContext _context;

        public LandlordHelper(UserManager<ApplicationUser> userManager, MotelDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Get current landlord ID from authenticated user
        /// 
        /// Logic:
        /// 1. Check if user is authenticated
        /// 2. Get current user from UserManager
        /// 3. Query Landlords table to find landlord by userId
        /// 4. Only return active landlords (IsDeleted = false)
        /// 5. Return 0 if user is not a landlord
        /// </summary>
        /// <param name="user">ClaimsPrincipal from controller (User property)</param>
        /// <returns>LandlordId if found, 0 otherwise</returns>
        public async Task<int> GetCurrentLandlordIdAsync(ClaimsPrincipal user)
        {
            // Step 1: Check if user is authenticated
            if (user.Identity?.IsAuthenticated != true)
                return 0;

            // Step 2: Get current user from Identity
            var currentUser = await _userManager.GetUserAsync(user);
            if (currentUser == null)
                return 0;

            // Step 3: Find landlord with matching userId
            // Note: A user can be a landlord, tenant, or just a regular user
            var landlord = await _context.Landlords
                .AsNoTracking()
                .Where(l => l.UserId == currentUser.Id && !l.IsDeleted)
                .FirstOrDefaultAsync();

            // Step 4: Return landlordId, or 0 if user is not a landlord
            return landlord?.LandlordId ?? 0;
        }

        /// <summary>
        /// Check if current user is a landlord (has an active landlord profile)
        /// </summary>
        /// <param name="user">ClaimsPrincipal from controller</param>
        /// <returns>True if user is a landlord, false otherwise</returns>
        public async Task<bool> IsLandlordAsync(ClaimsPrincipal user)
        {
            var landlordId = await GetCurrentLandlordIdAsync(user);
            return landlordId > 0;
        }
    }
}
