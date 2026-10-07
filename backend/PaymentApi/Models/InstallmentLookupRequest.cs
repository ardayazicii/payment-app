namespace PaymentApi.Models;

public class InstallmentLookupRequest
{
    public string Bin { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}