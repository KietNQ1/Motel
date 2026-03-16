using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interface;

namespace Motel.Services;

/// <summary>
/// Service implementation for tax estimation operations
/// Handles business logic for rental income tax calculation
/// </summary>
public class TaxService : ITaxService
{
    private readonly ITaxRepository _repo;

    public TaxService(ITaxRepository repo) => _repo = repo;

    /// <summary>
    /// Tính toán và lưu ước tính thuế cho một chủ nhà trong một năm cụ thể
    /// Calculate and save tax estimation for a landlord for a specific year
    /// 
    /// Các điểm quan trọng trong Business Logic:
    /// 
    /// 1. Cách tính Doanh thu:
    ///    - Dựa trên GIÁ THUÊ TRONG HỢP ĐỒNG (Room.RentPrice) + các PHÍ DỊCH VỤ cố định theo phòng
    ///      (FeeSettings có CalculationMethod khác "meter", ví dụ Internet, Rác), KHÔNG phải tiền thực tế đã thu
    ///    - KHÔNG tính các khoản THU HỘ theo chỉ số (điện, nước, ... – FeeSettings có CalculationMethod = "meter")
    ///    - Tính theo số tháng đầy đủ overlap với năm dương lịch, không tính theo ngày
    ///    - Ví dụ: 02/03/2024 → 31/10/2024 = 8 tháng (Tháng 3, 4, 5, 6, 7, 8, 9, 10)
    /// 
    /// 2. Ngưỡng Miễn thuế (100 Triệu VND):
    ///    - Tính theo NĂM DƯƠNG LỊCH (01/01 - 31/12), không theo thời gian hợp đồng
    ///    - Nếu tổng doanh thu năm <= ngưỡng, MIỄN THUẾ hoàn toàn
    /// 
    /// 3. Thuế suất (Thông tư 40/2021/TT-BTC của Việt Nam):
    ///    - VAT: 5% trên doanh thu chịu thuế
    ///    - PIT: 5% trên doanh thu chịu thuế
    ///    - Tổng: 10% thuế suất hiệu quả
    /// 
    /// 4. Nhiều Nhà trọ:
    ///    - Thuế tính trên TỔNG doanh thu từ TẤT CẢ nhà trọ của chủ nhà
    ///    - Không tính riêng từng nhà trọ
    /// 
    /// 5. Đây chỉ là ƯỚC TÍNH:
    ///    - Không phải kê khai thuế chính thức
    ///    - Kê khai chính thức phải làm qua hệ thống Tổng cục Thuế Việt Nam
    /// </summary>
    public async Task<TaxEstimation> CalculateTaxEstimationAsync(int landlordId, int year, CancellationToken ct = default)
    {
        // Bước 1: Kiểm tra landlord có tồn tại không
        var landlord = await _repo.GetLandlordAsync(landlordId, ct);
        if (landlord == null)
            throw new InvalidOperationException($"Không tìm thấy chủ nhà với ID {landlordId}.");

        // Bước 2: Lấy quy định thuế đang áp dụng cho năm này
        var taxRule = await _repo.GetActiveTaxRuleAsync(new DateOnly(year, 1, 1), ct);
        if (taxRule == null)
            throw new InvalidOperationException($"Không tìm thấy quy định thuế cho năm {year}.");

        // Bước 3: Lấy tất cả hợp đồng của chủ nhà có overlap với năm này
        var contracts = await _repo.GetLandlordContractsForYearAsync(landlordId, year, ct);

        // Bước 4: Tính tổng doanh thu trong năm (theo số tháng đầy đủ)
        var totalRevenue = CalculateYearlyRevenue(contracts, year);

        // Bước 5: Kiểm tra có được miễn thuế không (dưới ngưỡng 100 triệu)
        var isExempt = totalRevenue <= taxRule.RevenueThreshold;

        // Bước 6: Tính số tiền thuế
        decimal taxableRevenue = isExempt ? 0 : totalRevenue;
        decimal vatAmount = taxableRevenue * taxRule.VatRate;      // VAT = 5%
        decimal pitAmount = taxableRevenue * taxRule.PitRate;      // PIT = 5%
        decimal totalTaxAmount = vatAmount + pitAmount;            // Tổng = 10%

        // Bước 7: Tạo đối tượng TaxEstimation để lưu vào DB
        var estimation = new TaxEstimation
        {
            LandlordId = landlordId,
            Year = year,
            TotalRevenue = totalRevenue,
            TaxableRevenue = taxableRevenue,
            VatAmount = vatAmount,
            PitAmount = pitAmount,
            TotalTaxAmount = totalTaxAmount,
            TaxRuleId = taxRule.TaxRuleId,
            IsExempt = isExempt,
            Notes = BuildEstimationNotes(totalRevenue, taxRule.RevenueThreshold, isExempt, contracts.Count),
            CalculatedAt = DateTime.Now
        };

        // Bước 8: Lưu vào database (update nếu đã có, insert nếu chưa)
        await _repo.SaveTaxEstimationAsync(estimation, ct);

        // Bước 9: Load lại từ DB để có đầy đủ thông tin TaxRule
        var saved = await _repo.GetTaxEstimationAsync(landlordId, year, ct);
        return saved ?? estimation;
    }

    /// <summary>
    /// Get tax estimation history for a landlord
    /// </summary>
    public async Task<List<TaxEstimation>> GetTaxEstimationHistoryAsync(int landlordId, CancellationToken ct = default)
    {
        return await _repo.GetLandlordTaxEstimationsAsync(landlordId, ct);
    }

    /// <summary>
    /// Get tax estimation for a specific year (if exists)
    /// </summary>
    public async Task<TaxEstimation?> GetTaxEstimationForYearAsync(int landlordId, int year, CancellationToken ct = default)
    {
        return await _repo.GetTaxEstimationAsync(landlordId, year, ct);
    }

    // ============================================
    // Private Helper Methods
    // ============================================

    /// <summary>
    /// Tính tổng doanh thu cho thuê trong một năm dương lịch
    /// Calculate total rental revenue for a specific calendar year
    /// 
    /// Điểm quan trọng / Key Points:
    /// - Chỉ tính tiền thuê phòng + phí dịch vụ cố định, không tính điện nước theo chỉ số (Only counts rent + fixed service fees, not metered utilities)
    /// - Tính theo số tháng đầy đủ, không tính theo ngày (Counts complete months, not prorated by days)
    /// - Xử lý hợp đồng bắt đầu/kết thúc giữa năm (Handles mid-year contracts)
    /// - Xử lý hợp đồng kéo dài nhiều năm (Handles multi-year contracts)
    /// 
    /// Logic tính tháng:
    /// - Ví dụ: 02/03/2024 → 31/10/2024 = 8 tháng (Tháng 3, 4, 5, 6, 7, 8, 9, 10)
    /// - Công thức: (EndYear - StartYear) × 12 + (EndMonth - StartMonth) + 1
    /// </summary>
    private decimal CalculateYearlyRevenue(List<Contract> contracts, int year)
    {
        // Xác định khoảng thời gian của năm (01/01 → 31/12)
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        decimal totalRevenue = 0;

        foreach (var contract in contracts)
        {
            // Tìm khoảng thời gian overlap giữa hợp đồng và năm dương lịch
            // Ví dụ: Hợp đồng 15/10/2024 → 15/04/2025, Năm 2024
            //   → Overlap: 15/10/2024 → 31/12/2024
            var overlapStart = contract.StartDate > yearStart ? contract.StartDate : yearStart;
            var overlapEnd = contract.EndDate < yearEnd ? contract.EndDate : yearEnd;

            // Kiểm tra có overlap không
            if (overlapEnd < overlapStart)
                continue; // Không có overlap (safety check)

            // Lấy giá thuê tháng từ Room
            var monthlyRent = contract.Room?.RentPrice ?? 0;

            // Lấy các phí dịch vụ cố định theo phòng (không phải phí thu hộ theo chỉ số)
            // Rule:
            // - FeeSetting.CalculationMethod != "meter" => tính vào doanh thu (ví dụ: Internet, Rác, dịch vụ vệ sinh...)
            // - FeeSetting.CalculationMethod == "meter" => coi là phí thu hộ (Electricity, Water...), KHÔNG tính vào doanh thu chịu thuế
            // - Chỉ lấy các FeeSetting còn hiệu lực hiện tại (EffectiveTo == null) để đơn giản hóa ước tính
            decimal monthlyServiceFee = 0;
            var feeSettings = contract.Room?.FeeSettings
                ?.Where(fs => fs.EffectiveTo == null && !string.Equals(fs.CalculationMethod, "meter", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (feeSettings != null && feeSettings.Count > 0)
            {
                monthlyServiceFee = feeSettings.Sum(fs => fs.BaseAmount);
            }

            // Tính số tháng đầy đủ trong khoảng overlap
            // Công thức: (EndYear - StartYear) × 12 + (EndMonth - StartMonth) + 1
            // Ví dụ: 02/03/2024 → 31/10/2024 = (2024-2024)×12 + (10-3) + 1 = 8 tháng
            int startYear = overlapStart.Year;
            int startMonth = overlapStart.Month;
            int endYear = overlapEnd.Year;
            int endMonth = overlapEnd.Month;

            int numberOfMonths = (endYear - startYear) * 12 + (endMonth - startMonth) + 1;

            // Tính doanh thu theo số tháng đầy đủ
            // Công thức: (Tiền thuê tháng + Phí dịch vụ cố định) × Số tháng
            var revenue = (monthlyRent + monthlyServiceFee) * numberOfMonths;

            totalRevenue += revenue;
        }

        return Math.Round(totalRevenue, 2);
    }

    /// <summary>
    /// Build human-readable notes for tax estimation
    /// </summary>
    private string BuildEstimationNotes(decimal totalRevenue, decimal threshold, bool isExempt, int contractCount)
    {
        var notes = $"Tính toán dựa trên {contractCount} hợp đồng. ";

        if (isExempt)
        {
            notes += $"Tổng doanh thu {totalRevenue:N0} VND không vượt ngưỡng {threshold:N0} VND nên được MIỄN THUẾ. ";
        }
        else
        {
            notes += $"Tổng doanh thu {totalRevenue:N0} VND vượt ngưỡng {threshold:N0} VND nên phải nộp thuế. ";
        }

        notes += "Đây chỉ là ước tính, kê khai chính thức thực hiện qua hệ thống của Tổng cục Thuế.";

        return notes;
    }
}
