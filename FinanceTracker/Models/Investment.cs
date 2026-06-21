using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class Investment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama investasi wajib diisi")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Jenis investasi wajib diisi")]
        public string Type { get; set; } // contoh: "Saham", "Emas", "Reksadana", "Deposito", "Crypto"

        [Required(ErrorMessage = "Label unit wajib diisi")]
        public string UnitLabel { get; set; } // contoh: "Lot", "Gram", "Unit"

        [Column(TypeName = "decimal(18,6)")]
        public decimal TotalUnits { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AvgBuyPrice { get; set; } = 0;

        [Required(ErrorMessage = "Harga saat ini wajib diisi")]
        [Range(typeof(decimal), "0", "1000000000000")]
        public decimal CurrentPrice { get; set; }

        public int UserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<InvestmentTransaction> Transactions { get; set; } = new List<InvestmentTransaction>();
    }
}