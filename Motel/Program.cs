using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories;
using Motel.Repositories.Interface;
using Motel.Services;
using Motel.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<MotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =======================
// Identity (INT KEY)
// =======================
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<MotelDbContext>()
    .AddDefaultTokenProviders();

// Cookie config
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// Repositories
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
// Services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();


// Session (optional – KHÔNG bắt buộc cho Identity)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// =======================
// HTTP PIPELINE
// =======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseSession();        // optional
app.UseAuthentication(); // Identity
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
