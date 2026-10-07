namespace PaymentApi.Models;

public class PaymentRequest
{
    public decimal Amount { get; set; }

    public string CardHolderName { get; set; } = string.Empty;

    public string CardNumber { get; set; } = string.Empty;

    public string ExpireMonth { get; set; } = string.Empty;

    public string ExpireYear { get; set; } = string.Empty;

    public string Cvc { get; set; } = string.Empty;

    // Kullanıcının seçtiği taksit
    // Tek çekim için 1
    public int Installment { get; set; } = 1;
}