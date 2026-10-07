using System.Globalization;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;
using PaymentApi.Models;
using PaymentApi.Options;

namespace PaymentApi.Services;

public class InstallmentService : IInstallmentService
{
    private readonly IyzicoOptions _settings;

    public InstallmentService(
        IOptions<IyzicoOptions> settings)
    {
        _settings = settings.Value;
    }

    public async Task<InstallmentLookupResponse> LookupAsync(
        string bin,
        decimal amount)
    {
        var options = new Iyzipay.Options
        {
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey,
            BaseUrl = _settings.BaseUrl
        };

        var request = new RetrieveInstallmentInfoRequest
        {
            Locale = Locale.TR.ToString(),

            ConversationId =
                Guid.NewGuid().ToString(),

            BinNumber = bin,

            Price = amount.ToString(
                "0.00",
                CultureInfo.InvariantCulture
            )
        };

        var result =
            await InstallmentInfo.Retrieve(
                request,
                options
            );

        if (result.Status != Status.SUCCESS.ToString())
        {
            throw new Exception(
                result.ErrorMessage
                ?? "Taksit bilgileri alınamadı."
            );
        }

        var detail =
            result.InstallmentDetails?
                .FirstOrDefault();

        if (detail == null)
        {
            throw new Exception(
                "Bu kart için taksit seçeneği bulunamadı."
            );
        }

        var response =
            new InstallmentLookupResponse
            {
                BankName =
                    detail.BankName
                    ?? string.Empty,

                CardAssociation =
                    detail.CardAssociation
                    ?? string.Empty,

                CardFamily =
                    detail.CardFamilyName
                    ?? string.Empty,

                CardType =
                    detail.CardType
                    ?? string.Empty
            };

        if (detail.InstallmentPrices != null)
        {
            foreach (var price
                     in detail.InstallmentPrices)
            {
                var installmentNumber =
                    price.InstallmentNumber ?? 1;

                response.Installments.Add(
                    new InstallmentOptionResponse
                    {
                        InstallmentNumber =
                            installmentNumber,

                        InstallmentPrice =
                            ToDecimal(
                                price.Price
                            ),

                        TotalPrice =
                            ToDecimal(
                                price.TotalPrice
                            )
                    }
                );
            }
        }

        return response;
    }

    private static decimal ToDecimal(
        object? value)
    {
        var text =
            Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            );

        return decimal.TryParse(
            text,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result
        )
            ? result
            : 0;
    }
}