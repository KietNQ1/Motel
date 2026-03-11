namespace Motel.ViewModels.Tax;

/// <summary>
/// ViewModel for Tax Estimation calculation form
/// </summary>
public class TaxEstimationCalculateViewModel
{
    public int Year { get; set; } = DateTime.Now.Year;

    public List<int> AvailableYears { get; set; } = new();
}
