using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int ContractId { get; set; }

    public int RoomId { get; set; }

    public int PeriodMonth { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();

    public virtual ICollection<PaymentIntent> PaymentIntents { get; set; } = new List<PaymentIntent>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
