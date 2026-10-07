namespace PaymentApi.Models;

public class InstallmentOptionResponse
{
    public int InstallmentNumber { get; set; }

    public decimal InstallmentPrice { get; set; }

    public decimal TotalPrice { get; set; }
}