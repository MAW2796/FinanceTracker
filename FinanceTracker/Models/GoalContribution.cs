using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class GoalContribution
    {
        public int Id { get; set; }

        [Required]
        public int GoalId { get; set; }

        public Goal Goal { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Nominal harus lebih dari 0")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}