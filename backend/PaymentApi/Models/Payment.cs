namespace PaymentApi.Models;

public class Payment
{
    public int Id { get; set; }

    // Ürün / işlem esas tutarı
    public decimal Amount { get; set; }

    // Taksit/komisyon dahil çekilen toplam
    public decimal PaidPrice { get; set; }

    // 1 = tek çekim
    public int Installment { get; set; } = 1;

    // Taksit başına düşen tutar
    public decimal InstallmentPrice { get; set; }

    public string Status { get; set; } = "Pending";

    public string Provider { get; set; } = "Iyzico";

    public string Bin { get; set; } = string.Empty;

    public string LastFour { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string CardAssociation { get; set; } = string.Empty;

    public string CardFamily { get; set; } = string.Empty;

    public string CardType { get; set; } = string.Empty;

    public string ProviderPaymentId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}