namespace FinanceTracker.Models.ViewModels
{
    public class GoalIndexViewModel
    {
        public List<Goal> Goals { get; set; } = new();

        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }

        public decimal TotalSaved { get; set; }
        public decimal TotalTarget { get; set; }
    }
}