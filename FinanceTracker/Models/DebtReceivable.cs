using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class DebtReceivable
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tipe wajib dipilih")]
        public string Type { get; set; } // "Utang" atau "Piutang"

        [Required(ErrorMessage = "Nama pihak terkait wajib diisi")]
        public string PersonName { get; set; }

        [Required(ErrorMessage = "Jumlah wajib diisi")]
        [Range(typeof(decimal), "1", "1000000000000", ErrorMessage = "Jumlah harus lebih dari 0")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Tanggal jatuh tempo wajib diisi")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Kategori wajib dipilih")]
        [Range(1, int.MaxValue, ErrorMessage = "Kategori wajib dipilih")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int UserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<DebtPayment> Payments { get; set; } = new List<DebtPayment>();
    }
}