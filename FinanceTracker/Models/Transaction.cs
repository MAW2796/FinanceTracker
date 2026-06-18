using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tanggal wajib diisi")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Jumlah wajib diisi")]
        [Range(typeof(decimal), "1", "1000000000000", ErrorMessage = "Jumlah harus lebih dari 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Kategori wajib dipilih")]
        [Range(1, int.MaxValue, ErrorMessage = "Kategori wajib dipilih")]
        public int CategoryId { get; set; }

        public string Description { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public int UserId { get; set; }

        // 🔥 RELASI KE MONTHLY PLANNING
        public int? MonthlyPlanningId { get; set; }

        [ForeignKey("MonthlyPlanningId")]
        public MonthlyPlanning? MonthlyPlanning { get; set; }
    }
}