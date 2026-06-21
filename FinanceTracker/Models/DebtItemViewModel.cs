namespace FinanceTracker.Models
{
    public class DebtItemViewModel
    {
        public DebtReceivable Item { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
    }
}