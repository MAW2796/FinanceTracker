using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class MonthlyPlanning
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Judul wajib diisi")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Jumlah wajib diisi")]
        [Range(typeof(decimal), "1", "1000000000000", ErrorMessage = "Jumlah harus lebih dari 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Tanggal jatuh tempo wajib diisi")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime? PaidDate { get; set; }

        // 🔥 SAMAIN DENGAN TRANSACTION
        [Required(ErrorMessage = "Kategori wajib dipilih")]
        [Range(1, int.MaxValue, ErrorMessage = "Kategori wajib dipilih")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int UserId { get; set; }

        // 🔥 OPTIONAL (future scaling)
        public bool IsRecurring { get; set; } = false;
        public int? RecurringDay { get; set; }
    }
}