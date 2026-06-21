using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class InvestmentTransaction
    {
        public int Id { get; set; }

        [Required]
        public int InvestmentId { get; set; }

        public Investment? Investment { get; set; }

        [Required]
        public string Type { get; set; } // "Buy" atau "Sell"

        [Required]
        [Range(0.00001, double.MaxValue, ErrorMessage = "Unit harus lebih dari 0")]
        
        [Column(TypeName = "decimal(18,6)")]
        public decimal Units { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "1000000000000")]
        public decimal PricePerUnit { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;
    }
}