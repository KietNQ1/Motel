using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Helpers;

[Route("api/property")]
[ApiController]
public class PropertyApiController : ControllerBase
{
    private readonly MotelDbContext _context;
    private readonly LandlordHelper _landlordHelper;

    public PropertyApiController(MotelDbContext context, LandlordHelper landlordHelper)
    {
        _context = context;
        _landlordHelper = landlordHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetProperties()
    {
        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        
        var properties = await _context.Properties
            .Where(p => p.LandlordId == landlordId && !p.IsDeleted)
            .Select(p => new
            {
                p.PropertyId,
                p.Name
            })
            .ToListAsync();

        return Ok(properties);
    }
}