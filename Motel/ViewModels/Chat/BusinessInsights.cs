using System;
using System.Collections.Generic;

namespace Motel.ViewModels.Chat;

public sealed class PaymentInsightDto
{
    public int TotalPayments { get; set; }
    public decimal TotalAmount { get; set; }
    public int SucceededCount { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> RecentPaymentSamples { get; set; } = new();
}

public sealed class PaymentIntentInsightDto
{
    public int TotalIntents { get; set; }
    public int PendingIntents { get; set; }
    public int AwaitingLandlordIntents { get; set; }
    public int ExpiredIntents { get; set; }
    public List<string> RecentIntentSamples { get; set; } = new();
}

public sealed class InvoiceLineFeeTypeBreakdownDto
{
    public string FeeTypeName { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class InvoiceLineInsightDto
{
    public int TotalLines { get; set; }
    public decimal TotalAmount { get; set; }
    public List<InvoiceLineFeeTypeBreakdownDto> TopFeeTypes { get; set; } = new();
}
