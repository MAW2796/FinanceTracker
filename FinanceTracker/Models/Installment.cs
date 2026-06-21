using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class Installment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama cicilan wajib diisi")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Plafon wajib diisi")]
        [Range(typeof(decimal), "1", "1000000000000", ErrorMessage = "Plafon harus lebih dari 0")]
        public decimal PlafondAmount { get; set; }

        [Required(ErrorMessage = "Jumlah cicilan per bulan wajib diisi")]
        [Range(typeof(decimal), "1", "1000000000000", ErrorMessage = "Jumlah harus lebih dari 0")]
        public decimal MonthlyAmount { get; set; }

        [Required(ErrorMessage = "Tenor wajib diisi")]
        [Range(1, 360, ErrorMessage = "Tenor harus antara 1-360 bulan")]
        public int TenorMonths { get; set; }

        [Required(ErrorMessage = "Tanggal mulai wajib diisi")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Kategori wajib dipilih")]
        [Range(1, int.MaxValue, ErrorMessage = "Kategori wajib dipilih")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int UserId { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<MonthlyPlanning> GeneratedPlannings { get; set; } = new List<MonthlyPlanning>();
    }
}