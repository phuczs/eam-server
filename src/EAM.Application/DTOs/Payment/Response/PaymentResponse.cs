using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.DTOs.Payment.Response
{

    public class PaymentResponse
    {
        public Guid Id { get; set; }

        public string PaymentNo { get; set; } = null!;
        public decimal Amount { get; set; }

        // "BankTransfer", "CreditCard"
        public string PaymentMethod { get; set; } = null!;

        //  "pending", "completed", "failed"
        public string Status { get; set; } = null!;

        //  "VNPay", "Stripe"
        public string? GatewayName { get; set; }

        public string? ReceiptNo { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
