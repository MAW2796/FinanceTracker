using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class Goal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama goal wajib diisi")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Target wajib diisi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Target harus lebih dari 0")]
        public decimal TargetAmount { get; set; }

        public decimal CurrentAmount { get; set; } = 0;

        [DataType(DataType.Date)]
        public DateTime? TargetDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public int UserId { get; set; }

        public ICollection<GoalContribution> Contributions { get; set; } = new List<GoalContribution>();
    }
}