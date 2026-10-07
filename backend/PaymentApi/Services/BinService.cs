using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;
using PaymentApi.Models;
using PaymentApi.Options;

namespace PaymentApi.Services;

public class BinService : IBinService
{
    private readonly IyzicoOptions _settings;

    public BinService(IOptions<IyzicoOptions> settings)
    {
        _settings = settings.Value;
    }

    public async Task<BinLookupResponse> LookupAsync(string bin)
    {
        var options = new Iyzipay.Options
        {
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey,
            BaseUrl = _settings.BaseUrl
        };

        var request = new RetrieveBinNumberRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = Guid.NewGuid().ToString(),
            BinNumber = bin
        };

        var result = await BinNumber.Retrieve(request, options);

        if (result.Status != Status.SUCCESS.ToString())
        {
            throw new Exception(
                result.ErrorMessage ?? "iyzico BIN sorgusu başarısız."
            );
        }

        return new BinLookupResponse
        {
            BankName = result.BankName ?? string.Empty,
            CardAssociation = result.CardAssociation ?? string.Empty,
            CardType = result.CardType ?? string.Empty,
            CardFamily = result.CardFamily ?? string.Empty
        };
    }
}