using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class DebtPayment
    {
        public int Id { get; set; }

        [Required]
        public int DebtReceivableId { get; set; }

        public DebtReceivable? DebtReceivable { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Nominal harus lebih dari 0")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // relasi ke transaksi yang otomatis dibuat saat pembayaran ini dicatat
        public int? TransactionId { get; set; }
    }
}