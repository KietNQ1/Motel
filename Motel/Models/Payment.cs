using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public int PaymentIntentId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderTxnId { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    public string Status { get; set; } = null!;

    public string? RawCallbackJson { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual PaymentIntent PaymentIntent { get; set; } = null!;
}
