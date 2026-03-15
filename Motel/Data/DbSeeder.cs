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

            // --- 0. SEED FEE TYPES ---
            Console.WriteLine("=> Seeding Fee Types...");
            await EnsureFeeTypeAsync(context, "Rent", "tháng", "Tiền phòng", true);
            await EnsureFeeTypeAsync(context, "Electricity", "kWh", "Tiền điện", true);
            await EnsureFeeTypeAsync(context, "Water", "khối", "Tiền nước", true);
            await EnsureFeeTypeAsync(context, "Internet", "tháng", "Tiền mạng", true);
            await EnsureFeeTypeAsync(context, "Trash", "tháng", "Tiền rác", true);

            // --- 1. SEED ROLES ---
            Console.WriteLine("=> Seeding Roles...");
            var roles = new[] { "Admin", "Landlord" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                }
            }

            // --- 2. SEED USERS ---
            Console.WriteLine("=> Seeding Admin User...");
            var adminUser = await EnsureUserAsync(userManager, "admin@motel.local", "System Admin", "0900000000", "Admin");

            Console.WriteLine("=> Seeding Landlords...");
            var landlordUser1 = await EnsureUserAsync(userManager, "landlord1@motel.local", "Vũ Huy Hoàng", "0911111111", "Landlord");
            var landlordUser2 = await EnsureUserAsync(userManager, "landlord2@motel.local", "Trần Thu Hằng", "0922222222", "Landlord");

            // --- 3. SEED LANDLORD PROFILES ---
            Console.WriteLine("=> Seeding Landlord Profiles...");
            var landlord1 = await EnsureLandlordProfileAsync(context, landlordUser1.Id, "Vũ Huy Hoàng",  "123 Đường Cộng Hòa, Tân Bình, HCM", "012345678901", "45 Nguyễn Trãi, Phường 2, Q5, TP.HCM");
            var landlord2 = await EnsureLandlordProfileAsync(context, landlordUser2.Id, "Trần Thu Hằng", "456 Đường Nguyễn Trãi, Thanh Xuân, HN",  "098765432109", "23 Lê Lợi, Hoàn Kiếm, Hà Nội");

            // --- 4. SEED PROPERTIES ---
            Console.WriteLine("=> Seeding Properties...");
            var prop1 = await EnsurePropertyAsync(context, landlord1.LandlordId, "Nhà Trọ Sinh Viên", "12 Hẻm 4, Đường A, Thủ Đức", "Khu trọ sinh viên giá rẻ");
            var prop2 = await EnsurePropertyAsync(context, landlord1.LandlordId, "Chung Cư Mini Cao Cấp", "34 Mặt tiền B, Quận 1", "Chung cư có tháng máy, bảo vệ 24/7");
            var prop3 = await EnsurePropertyAsync(context, landlord2.LandlordId, "Nhà Xưởng Công Nhân", "Khu Công Nghiệp C, Bình Dương", "Dành cho công nhân thuê");

            // --- 5. SEED ROOMS & FURNITURES ---
            Console.WriteLine("=> Seeding Rooms & Furnitures...");
            // Property 1 (Nhà Trọ Sinh Viên) - Setup rooms
            var p1r1 = await EnsureRoomAsync(context, prop1.PropertyId, "P101", 2500000, 2);
            var p1r2 = await EnsureRoomAsync(context, prop1.PropertyId, "P102", 2500000, 2);
            var p1r3 = await EnsureRoomAsync(context, prop1.PropertyId, "P103", 2800000, 3);
            var p1r4 = await EnsureRoomAsync(context, prop1.PropertyId, "P104", 3000000, 4);

            await EnsureFurnitureAsync(context, p1r1.RoomId, "Máy lạnh", 1, "Daikin 1.5HP");
            await EnsureFurnitureAsync(context, p1r1.RoomId, "Giường", 1, "Giường gỗ 1m6");
            await EnsureFurnitureAsync(context, p1r3.RoomId, "Quạt trần", 1, "Vinawind");

            // Property 2 (Chung Cư Mini)
            var p2r1 = await EnsureRoomAsync(context, prop2.PropertyId, "C201", 6000000, 2);
            var p2r2 = await EnsureRoomAsync(context, prop2.PropertyId, "C202", 6500000, 3);
            await EnsureFurnitureAsync(context, p2r1.RoomId, "Máy lạnh", 1, "Panasonic Inverter");
            await EnsureFurnitureAsync(context, p2r1.RoomId, "Tủ lạnh", 1, "Aqua 150L");
            await EnsureFurnitureAsync(context, p2r1.RoomId, "Tủ quần áo", 1, "Gỗ ép");

            // Property 3 (Nhà Xưởng)
            var p3r1 = await EnsureRoomAsync(context, prop3.PropertyId, "A1", 1500000, 4);
            var p3r2 = await EnsureRoomAsync(context, prop3.PropertyId, "A2", 1500000, 4);

            // --- 6. SEED TENANTS ---
            Console.WriteLine("=> Seeding Tenants...");
            var tenant1 = await EnsureTenantAsync(context, landlord1.LandlordId, "011111111111", "Trịnh Xuân T",  "0933333333", "tenant1@motel.local", new DateOnly(1999, 5, 12),  "12 Trần Phú, Quận 5, TP.HCM");
            var tenant2 = await EnsureTenantAsync(context, landlord1.LandlordId, "022222222222", "Lê Văn L",    "0944444444", "tenant2@motel.local", new DateOnly(2000, 8, 20),  "78 Lê Văn Việt, Q9, TP.HCM");
            var tenant3 = await EnsureTenantAsync(context, landlord1.LandlordId, "033333333333", "Mai Thị M",  "0955555555", "tenant3@motel.local", new DateOnly(1998, 3, 15),  "5 Nguyễn Huệ, Q1, TP.HCM");
            var tenant4 = await EnsureTenantAsync(context, landlord2.LandlordId, "044444444444", "Hoàng Anh H", "0966666666", "tenant4@motel.local", new DateOnly(2001, 11, 7), "99 Trường Chinh, Bình Dương");

            var today = DateOnly.FromDateTime(DateTime.Today);

            // --- 7. SEED CONTRACTS & UTILITIES & INVOICES ---
            Console.WriteLine("=> Seeding Contracts & Flow data...");

            // Tenant 1 -> P101
            await SetupFullRentalFlow(context, p1r1, tenant1, landlordUser1.Id, today.AddMonths(-2), 2500000, 3500, 15000, 50000, 20000, 100, 150, 20, 30);
            
            // Tenant 2 -> P103
            await SetupFullRentalFlow(context, p1r3, tenant2, landlordUser1.Id, today.AddMonths(-1), 2800000, 3500, 15000, 50000, 20000, 500, 600, 40, 55);

            // Tenant 3 -> C201
            await SetupFullRentalFlow(context, p2r1, tenant3, landlordUser1.Id, today.AddMonths(-5), 6000000, 4000, 20000, 100000, 30000, 1200, 1450, 100, 115);

            // Tenant 4 -> A1
            await SetupFullRentalFlow(context, p3r1, tenant4, landlordUser2.Id, today.AddMonths(-3), 1500000, 3000, 12000, 0, 10000, 20, 50, 5, 12);

            // --- 8. SEED TAX RULES ---
            Console.WriteLine("=> Seeding TaxRules...");
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
                    Notes = "Luật thuế cho thuê tài sản. TN > 100m/năm chịu VAT 5% và TNCN 5%."
                };
                context.TaxRules.Add(taxRule);
                await context.SaveChangesAsync();
            }
            
            Console.WriteLine("=> [SEEDED SUCCESSFULLY]");
        }

        // --- HELPER METHODS ---

        private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, string phone, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, PhoneNumber = phone };
                var res = await userManager.CreateAsync(user, "123456");
                if (res.Succeeded) await userManager.AddToRoleAsync(user, role);
            }
            return user;
        }

        private static async Task<Landlord> EnsureLandlordProfileAsync(
            MotelDbContext context, int userId, string displayName, string address,
            string? identityNo = null, string? permanentAddress = null)
        {
            var profile = await context.Landlords.FirstOrDefaultAsync(l => l.UserId == userId && !l.IsDeleted);
            if (profile == null)
            {
                profile = new Landlord
                {
                    UserId = userId, DisplayName = displayName, Address = address,
                    IdentityNo = identityNo, PermanentAddress = permanentAddress,
                    IsDeleted = false
                };
                context.Landlords.Add(profile);
                await context.SaveChangesAsync();
            }
            return profile;
        }

        private static async Task<Property> EnsurePropertyAsync(MotelDbContext context, int landlordId, string name, string address, string desc)
        {
            var prop = await context.Properties.FirstOrDefaultAsync(p => p.LandlordId == landlordId && p.Name == name && !p.IsDeleted);
            if (prop == null)
            {
                prop = new Property { LandlordId = landlordId, Name = name, Address = address, Description = desc, IsDeleted = false };
                context.Properties.Add(prop);
                await context.SaveChangesAsync();
            }
            return prop;
        }

        private static async Task<Room> EnsureRoomAsync(MotelDbContext context, int propId, string name, decimal price, int maxOcc)
        {
            var room = await context.Rooms.FirstOrDefaultAsync(r => r.PropertyId == propId && r.RoomName == name && !r.IsDeleted);
            if (room == null)
            {
                room = new Room { PropertyId = propId, RoomName = name, RentPrice = price, Status = "available", MaxOccupants = maxOcc, IsDeleted = false };
                context.Rooms.Add(room);
                await context.SaveChangesAsync();
            }
            return room;
        }

        private static async Task EnsureFurnitureAsync(MotelDbContext context, int roomId, string name, int qty, string desc)
        {
            // Ensure the catalog exists for this furniture name
            var catalog = await context.FurnitureCatalogs
                .FirstOrDefaultAsync(c => c.Name == name);

            if (catalog == null)
            {
                catalog = new FurnitureCatalog { Name = name, IsActive = true };
                context.FurnitureCatalogs.Add(catalog);
                await context.SaveChangesAsync();
            }

            // Default status: try to find "Tốt" otherwise use any existing status
            var status = await context.FurnitureStatuses
                .FirstOrDefaultAsync(s => s.Name == "Tốt")
                ?? await context.FurnitureStatuses.FirstOrDefaultAsync();

            // Add furniture if not exists in this room for this catalog
            if (!await context.RoomFurnitures.AnyAsync(f => f.RoomId == roomId && f.FurnitureCatalogId == catalog.FurnitureCatalogId && !f.IsDeleted))
            {
                context.RoomFurnitures.Add(new RoomFurniture
                {
                    RoomId = roomId,
                    FurnitureCatalogId = catalog.FurnitureCatalogId,
                    Quantity = qty,
                    Description = desc,
                    FurnitureStatusId = status?.FurnitureStatusId ?? 0,
                    IsDeleted = false
                });
                await context.SaveChangesAsync();
            }
        }

        private static async Task<Tenant> EnsureTenantAsync(
            MotelDbContext context, int landlordId, string identityNo, string name, string phone, string email,
            DateOnly? dateOfBirth = null, string? permanentAddress = null)
        {
            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.LandlordId == landlordId && t.IdentityNo == identityNo && !t.IsDeleted);
            if (tenant == null)
            {
                tenant = new Tenant
                {
                    LandlordId = landlordId, IdentityNo = identityNo, FullName = name,
                    Phone = phone, Email = email,
                    DateOfBirth = dateOfBirth, PermanentAddress = permanentAddress,
                    IsDeleted = false
                };
                context.Tenants.Add(tenant);
                await context.SaveChangesAsync();
            }
            return tenant;
        }

        private static async Task SetupFullRentalFlow(
            MotelDbContext context, Room room, Tenant tenant, int landlordUserId, DateOnly startDate, 
            decimal deposit, decimal ePrice, decimal wPrice, decimal iFee, decimal tFee, 
            int eOld, int eNew, int wOld, int wNew)
        {
            // 1. Contract
            var contract = await context.Contracts.FirstOrDefaultAsync(c => c.RoomId == room.RoomId && c.Status == "active");
            if (contract == null)
            {
                contract = new Contract { RoomId = room.RoomId, TenantId = tenant.TenantId, DepositAmount = deposit, StartDate = startDate, EndDate = startDate.AddMonths(12), Status = "active" };
                context.Contracts.Add(contract);
                room.Status = "occupied";
                context.Rooms.Update(room);
            }

            // 2. Occupancy
            if (!await context.RoomOccupancies.AnyAsync(o => o.RoomId == room.RoomId && o.Status == "active"))
                context.RoomOccupancies.Add(new RoomOccupancy { RoomId = room.RoomId, TenantId = tenant.TenantId, MoveInDate = startDate, IsPrimary = true, Status = "active" });

            // 3. Utility Settings
            var feeTypeRent = await context.FeeTypes.FirstOrDefaultAsync(f => f.Name == "Rent");
            var feeTypeElec = await context.FeeTypes.FirstOrDefaultAsync(f => f.Name == "Electricity");
            var feeTypeWater = await context.FeeTypes.FirstOrDefaultAsync(f => f.Name == "Water");
            var feeTypeNet = await context.FeeTypes.FirstOrDefaultAsync(f => f.Name == "Internet");
            var feeTypeTrash = await context.FeeTypes.FirstOrDefaultAsync(f => f.Name == "Trash");

            if (!await context.FeeSettings.AnyAsync(u => u.RoomId == room.RoomId && u.EffectiveTo == null))
            {
                context.FeeSettings.AddRange(new List<FeeSetting>
                {
                    new FeeSetting { RoomId = room.RoomId, FeeTypeId = feeTypeElec!.FeeTypeId, CalculationMethod = "meter", UnitPrice = ePrice, EffectiveFrom = startDate },
                    new FeeSetting { RoomId = room.RoomId, FeeTypeId = feeTypeWater!.FeeTypeId, CalculationMethod = "meter", UnitPrice = wPrice, EffectiveFrom = startDate },
                    new FeeSetting { RoomId = room.RoomId, FeeTypeId = feeTypeNet!.FeeTypeId, CalculationMethod = "per_room", BaseAmount = iFee, EffectiveFrom = startDate },
                    new FeeSetting { RoomId = room.RoomId, FeeTypeId = feeTypeTrash!.FeeTypeId, CalculationMethod = "per_room", BaseAmount = tFee, EffectiveFrom = startDate }
                });
            }

            await context.SaveChangesAsync(); // Save to generate IDs

            // 4. Meter Reading (Current Month)
            int period = DateOnly.FromDateTime(DateTime.Today).Year * 100 + DateOnly.FromDateTime(DateTime.Today).Month;
            var meter = await context.MeterReadings.FirstOrDefaultAsync(m => m.RoomId == room.RoomId && m.PeriodMonth == period);
            if (meter == null)
            {
                meter = new MeterReading { RoomId = room.RoomId, PeriodMonth = period, ElectricOld = eOld, ElectricNew = eNew, WaterOld = wOld, WaterNew = wNew, RecordedByUserId = landlordUserId };
                context.MeterReadings.Add(meter);
                await context.SaveChangesAsync();
            }

            // 5. Invoice
            if (!await context.Invoices.AnyAsync(i => i.RoomId == room.RoomId && i.PeriodMonth == period))
            {
                decimal eCost = (eNew - eOld) * ePrice;
                decimal wCost = (wNew - wOld) * wPrice;
                decimal total = room.RentPrice + eCost + wCost + iFee + tFee;

                var invoice = new Invoice { ContractId = contract.ContractId, RoomId = room.RoomId, PeriodMonth = period, TotalAmount = total, Status = "unpaid", DueDate = startDate.AddDays(7) };
                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();

                context.InvoiceLines.AddRange(new List<InvoiceLine> {
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, FeeTypeId = feeTypeRent!.FeeTypeId, Description = "Tiền phòng", Quantity = 1, UnitPrice = room.RentPrice },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, FeeTypeId = feeTypeElec!.FeeTypeId, Description = "Điện", Quantity = (eNew - eOld), UnitPrice = ePrice },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, FeeTypeId = feeTypeWater!.FeeTypeId, Description = "Nước", Quantity = (wNew - wOld), UnitPrice = wPrice },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, FeeTypeId = feeTypeNet!.FeeTypeId, Description = "Internet", Quantity = 1, UnitPrice = iFee },
                    new InvoiceLine { InvoiceId = invoice.InvoiceId, FeeTypeId = feeTypeTrash!.FeeTypeId, Description = "Rác", Quantity = 1, UnitPrice = tFee }
                });
                await context.SaveChangesAsync();
            }
        }
        private static async Task<FeeType> EnsureFeeTypeAsync(MotelDbContext context, string name, string unit, string desc, bool isSystem)
        {
            var f = await context.FeeTypes.FirstOrDefaultAsync(x => x.Name == name);
            if (f == null)
            {
                f = new FeeType { Name = name, Unit = unit, Description = desc, IsSystem = isSystem };
                context.FeeTypes.Add(f);
                await context.SaveChangesAsync();
            }

            if (!context.FurnitureCatalogs.Any())
            {
                context.FurnitureCatalogs.AddRange(
                    new FurnitureCatalog { Name = "Giường" },
                    new FurnitureCatalog { Name = "Tủ quần áo" },
                    new FurnitureCatalog { Name = "Bàn" },
                    new FurnitureCatalog { Name = "Ghế" },
                    new FurnitureCatalog { Name = "Điều hòa" },
                    new FurnitureCatalog { Name = "Máy giặt" },
                    new FurnitureCatalog { Name = "Tủ lạnh" },
                    new FurnitureCatalog { Name = "Quạt" }
                );

                await context.SaveChangesAsync();
            }

            if (!context.FurnitureStatuses.Any())
            {
                context.FurnitureStatuses.AddRange(
                    new FurnitureStatus { Name = "Tốt" },
                    new FurnitureStatus { Name = "Hỏng" },
                    new FurnitureStatus { Name = "Bảo trì" }
                );

                await context.SaveChangesAsync();
            }

            return f;
        }
    }
}
