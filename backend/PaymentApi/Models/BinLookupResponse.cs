namespace PaymentApi.Models;

public class BinLookupResponse
{
    public string BankName { get; set; } = string.Empty;
    public string CardAssociation { get; set; } = string.Empty;
    public string CardType { get; set; } = string.Empty;
    public string CardFamily { get; set; } = string.Empty;
}
