using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class Asset
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama aset wajib diisi")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Nilai wajib diisi")]
        [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "Nilai tidak boleh negatif")]
        public decimal CurrentValue { get; set; }

        public string? Notes { get; set; }

        public int UserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<AssetValueHistory> ValueHistory { get; set; } = new List<AssetValueHistory>();
    }
}