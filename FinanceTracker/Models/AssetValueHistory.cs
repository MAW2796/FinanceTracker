using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class AssetValueHistory
    {
        public int Id { get; set; }

        [Required]
        public int AssetId { get; set; }

        public Asset? Asset { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "1000000000000")]
        public decimal Value { get; set; }

        public DateTime RecordedDate { get; set; } = DateTime.Now;
    }
}