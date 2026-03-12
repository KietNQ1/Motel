namespace Motel.ViewModels.Tax;

/// <summary>
/// ViewModel for displaying tax estimation history list
/// </summary>
public class TaxEstimationHistoryViewModel
{
    public List<TaxEstimationDetailViewModel> Estimations { get; set; } = new();

    public string LandlordName { get; set; } = string.Empty;
}
