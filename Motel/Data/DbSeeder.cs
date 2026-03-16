using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Motel.Models;

namespace Motel.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MotelDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // Apply pending migrations automatically
            if (context.Database.IsSqlServer())
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Seed Roles
            Console.WriteLine("=> Seeding Roles...");
            var roles = new[] { "Admin", "Landlord" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                }
            }

            // Seed Admin User
            Console.WriteLine("=> Seeding Admin User...");
            var adminEmail = "admin@motel.local";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    PhoneNumber = "0900000000"
                };
                var result = await userManager.CreateAsync(admin, "123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Seed Landlord User
            Console.WriteLine("=> Seeding Landlord User...");
            var landlordEmail = "landlord1@motel.local";
            var landlordUser = await userManager.FindByEmailAsync(landlordEmail);
            if (landlordUser == null)
            {
                landlordUser = new ApplicationUser
                {
                    UserName = landlordEmail,
                    Email = landlordEmail,
                    FullName = "Landlord 1",
                    PhoneNumber = "0911111111"
                };
                var result = await userManager.CreateAsync(landlordUser, "123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(landlordUser, "Landlord");
                }
                else
                {
                    Console.WriteLine("=> FAILED to create Landlord User. Errors: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // At this point, ensure we have the landlord profile
            Console.WriteLine($"=> Seeding Landlord Profile for UserId={landlordUser.Id}...");
            var landlordProfile = await context.Landlords.FirstOrDefaultAsync(l => l.UserId == landlordUser.Id && !l.IsDeleted);
            if (landlordProfile == null)
            {
                landlordProfile = new Landlord
                {
                    UserId = landlordUser.Id,
                    DisplayName = "Chủ trọ A",
                    Address = "TP. Hồ Chí Minh",
                    IsDeleted = false
                };
                context.Landlords.Add(landlordProfile);
                await context.SaveChangesAsync();
            }

            // Seed demo Property
            Console.WriteLine("=> Seeding Demo Property...");
            var propertyName = "Nhà trọ A";
            var property = await context.Properties.FirstOrDefaultAsync(p => p.LandlordId == landlordProfile.LandlordId && p.Name == propertyName && !p.IsDeleted);
            if (property == null)
            {
                property = new Property
                {
                    LandlordId = landlordProfile.LandlordId,
                    Name = propertyName,
                    Address = "123 Nguyễn Văn A, Quận 1, TP.HCM",
                    Description = "Demo property (DbSeeder)",
                    IsDeleted = false
                };
                context.Properties.Add(property);
                await context.SaveChangesAsync();
            }

            // Seed Rooms
            Console.WriteLine("=> Seeding Demo Rooms...");
            var roomA101 = await context.Rooms.FirstOrDefaultAsync(r => r.PropertyId == property.PropertyId && r.RoomName == "A101" && !r.IsDeleted);
            if (roomA101 == null)
            {
                roomA101 = new Room { PropertyId = property.PropertyId, RoomName = "A101", RentPrice = 2500000, Status = "occupied", MaxOccupants = 2, IsDeleted = false };
                context.Rooms.Add(roomA101);
                await context.SaveChangesAsync();
            }
            else
            {
                roomA101.Status = "occupied";
                context.Rooms.Update(roomA101);
            }

            var roomA102 = await context.Rooms.FirstOrDefaultAsync(r => r.PropertyId == property.PropertyId && r.RoomName == "A102" && !r.IsDeleted);
            if (roomA102 == null)
            {
                roomA102 = new Room { PropertyId = property.PropertyId, RoomName = "A102", RentPrice = 2800000, Status = "available", MaxOccupants = 3, IsDeleted = false };
                context.Rooms.Add(roomA102);
            }

            await context.SaveChangesAsync();

            // Seed Tenant
            var tenantIdentity = "0123456789";
            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.LandlordId == landlordProfile.LandlordId && t.IdentityNo == tenantIdentity && !t.IsDeleted);
            if (tenant == null)
            {
                tenant = new Tenant
                {
                    LandlordId = landlordProfile.LandlordId,
                    FullName = "Nguyễn Văn Thuê",
                    Phone = "0922222222",
                    Email = "tenant1@motel.local",
                    IdentityNo = tenantIdentity,
                    IsDeleted = false
                };
                context.Tenants.Add(tenant);
                await context.SaveChangesAsync();
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Seed Contract
            var contract = await context.Contracts.FirstOrDefaultAsync(c => c.RoomId == roomA101.RoomId && c.Status == "active" && !c.IsDeleted);
            if (contract == null)
            {
                contract = new Contract
                {
                    RoomId = roomA101.RoomId,
                    TenantId = tenant.TenantId,
                    DepositAmount = 1000000,
                    StartDate = today,
                    EndDate = today.AddMonths(6),
                    Status = "active",
                    IsDeleted = false
                };
                context.Contracts.Add(contract);
                await context.SaveChangesAsync();
            }

            // Seed Occupancy
            var occupancy = await context.RoomOccupancies.FirstOrDefaultAsync(o => o.RoomId == roomA101.RoomId && o.IsPrimary && o.Status == "active");
            if (occupancy == null)
            {
                occupancy = new RoomOccupancy
                {
                    RoomId = roomA101.RoomId,
                    TenantId = tenant.TenantId,
                    MoveInDate = today,
                    IsPrimary = true,
                    Status = "active"
                };
                context.RoomOccupancies.Add(occupancy);
            }

            // Seed Utility Settings
            var utilitySetting = await context.RoomUtilitySettings.FirstOrDefaultAsync(u => u.RoomId == roomA101.RoomId && u.EffectiveTo == null);
            if (utilitySetting == null)
            {
                utilitySetting = new RoomUtilitySetting
                {
                    RoomId = roomA101.RoomId,
                    ElectricUnitPrice = 3500,
                    WaterUnitPrice = 15000,
                    InternetFee = 100000,
                    TrashFee = 30000,
                    EffectiveFrom = today
                };
                context.RoomUtilitySettings.Add(utilitySetting);
                await context.SaveChangesAsync();
            }

            // Seed Meter Readings
            int periodMonth = today.Year * 100 + today.Month;
            var meterReading = await context.MeterReadings.FirstOrDefaultAsync(m => m.RoomId == roomA101.RoomId && m.PeriodMonth == periodMonth);
            if (meterReading == null)
            {
                meterReading = new MeterReading
                {
                    RoomId = roomA101.RoomId,
                    PeriodMonth = periodMonth,
                    ElectricOld = 1000,
                    ElectricNew = 1050,
                    WaterOld = 200,
                    WaterNew = 205,
                    RecordedByUserId = landlordUser.Id
                };
                context.MeterReadings.Add(meterReading);
                await context.SaveChangesAsync();
            }

            // Seed Invoice & Lines
            var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.RoomId == roomA101.RoomId && i.PeriodMonth == periodMonth);
            
            int electQty = meterReading.ElectricNew - meterReading.ElectricOld;
            int waterQty = meterReading.WaterNew - meterReading.WaterOld;
            decimal electCost = electQty * utilitySetting.ElectricUnitPrice;
            decimal waterCost = waterQty * utilitySetting.WaterUnitPrice;
            decimal totalAmount = roomA101.RentPrice + electCost + waterCost + utilitySetting.InternetFee + utilitySetting.TrashFee;

            if (invoice == null)
            {
                invoice = new Invoice
                {
                    ContractId = contract.ContractId,
                    RoomId = roomA101.RoomId,
                    PeriodMonth = periodMonth,
                    TotalAmount = totalAmount,
                    Status = "unpaid",
                    DueDate = today.AddDays(7)
                };
                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();

                var lines = new List<InvoiceLine>
                {
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, ItemType = "rent", Description = "Tiền phòng", Quantity = 1, UnitPrice = roomA101.RentPrice },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, ItemType = "electric", Description = "Tiền điện", Quantity = electQty, UnitPrice = utilitySetting.ElectricUnitPrice },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, ItemType = "water", Description = "Tiền nước", Quantity = waterQty, UnitPrice = utilitySetting.WaterUnitPrice },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, ItemType = "internet", Description = "Internet", Quantity = 1, UnitPrice = utilitySetting.InternetFee },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, ItemType = "trash", Description = "Rác", Quantity = 1, UnitPrice = utilitySetting.TrashFee }
                };
                context.InvoiceLines.AddRange(lines);
                await context.SaveChangesAsync();
            }
            else
            {
                invoice.TotalAmount = totalAmount;
                context.Invoices.Update(invoice);
                await context.SaveChangesAsync();
            }

            // Seed Initial Tax Rule (Circular 40/2021/TT-BTC)
            var ruleName = "Thông tư 40/2021/TT-BTC";
            var taxRule = await context.TaxRules.FirstOrDefaultAsync(r => r.RuleName == ruleName);
            if (taxRule == null)
            {
                taxRule = new TaxRule
                {
                    RuleName = ruleName,
                    VatRate = 0.0500m,
                    PitRate = 0.0500m,
                    RevenueThreshold = 100000000.00m,
                    EffectiveDate = new DateOnly(2021, 6, 7),
                    EndDate = null,
                    IsActive = true,
                    Notes = "Luật thuế hiện hành cho cho thuê tài sản. Doanh thu trên 100 triệu/năm chịu thuế VAT 5% và thuế TNCN 5%."
                };
                context.TaxRules.Add(taxRule);
                await context.SaveChangesAsync();
            }
        }
    }
}

