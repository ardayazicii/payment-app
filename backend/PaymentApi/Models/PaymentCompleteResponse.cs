namespace PaymentApi.Models;

public class PaymentCompleteResponse
{
    public bool Success { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}