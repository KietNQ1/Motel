using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class PaymentIntent
{
    public int PaymentIntentId { get; set; }

    public int InvoiceId { get; set; }

    public string Provider { get; set; } = null!;

    public string? ProviderIntentId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
