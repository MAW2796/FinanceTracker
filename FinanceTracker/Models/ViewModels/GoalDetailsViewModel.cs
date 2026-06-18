namespace FinanceTracker.Models.ViewModels
{
    public class GoalDetailsViewModel
    {
        public Goal Goal { get; set; }
        public List<GoalContribution> Contributions { get; set; } = new();
    }
}