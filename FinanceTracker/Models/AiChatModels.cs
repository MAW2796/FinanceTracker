namespace FinanceTracker.Models
{
    public class ChatRequest
    {
        public string Message { get; set; }
    }

    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; }
        public string Error { get; set; }
    }
}