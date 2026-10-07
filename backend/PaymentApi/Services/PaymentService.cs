using System.Globalization;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentApi.Data;
using PaymentApi.Models;
using PaymentApi.Options;

using PaymentEntity = PaymentApi.Models.Payment;

namespace PaymentApi.Services;

public class PaymentService : IPaymentService
{
    private readonly IyzicoOptions _settings;
    private readonly AppDbContext _db;
    private readonly IInstallmentService _installmentService;

    public PaymentService(
        IOptions<IyzicoOptions> settings,
        AppDbContext db,
        IInstallmentService installmentService)
    {
        _settings = settings.Value;
        _db = db;
        _installmentService = installmentService;
    }

    public async Task<PaymentStartResponse> Start3dsAsync(
        PaymentRequest paymentRequest)
    {
        var options = CreateIyzicoOptions();

        var cleanCardNumber =
            new string(
                paymentRequest.CardNumber
                    .Where(char.IsDigit)
                    .ToArray()
            );

        if (cleanCardNumber.Length < 12)
        {
            throw new Exception(
                "Geçerli bir kart numarası giriniz."
            );
        }

        var bin = cleanCardNumber[..8];
        var lastFour = cleanCardNumber[^4..];

        // Kullanıcı 0 vb. gönderirse tek çekim kabul et
        var requestedInstallment =
            paymentRequest.Installment <= 0
                ? 1
                : paymentRequest.Installment;

        var installmentInfo =
            await _installmentService.LookupAsync(
                bin,
                paymentRequest.Amount
            );

        var selectedInstallment =
            installmentInfo.Installments
                .FirstOrDefault(x =>
                    x.InstallmentNumber ==
                    requestedInstallment
                );

        if (selectedInstallment == null)
        {
            throw new Exception(
                $"{requestedInstallment} taksit bu kart için kullanılamıyor."
            );
        }

        var payment = new PaymentEntity
        {
            Amount =
                paymentRequest.Amount,

            PaidPrice =
                selectedInstallment.TotalPrice,

            Installment =
                requestedInstallment,

            InstallmentPrice =
                selectedInstallment.InstallmentPrice,

            Status =
                "Pending",

            Provider =
                "Iyzico",

            Bin =
                bin,

            LastFour =
                lastFour,

            BankName =
                installmentInfo.BankName,

            CardAssociation =
                installmentInfo.CardAssociation,

            CardFamily =
                installmentInfo.CardFamily,

            CardType =
                installmentInfo.CardType,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow
        };

        _db.Payments.Add(payment);

        await _db.SaveChangesAsync();

        // Orijinal ürün fiyatı
        var amount =
            paymentRequest.Amount.ToString(
                "0.00",
                CultureInfo.InvariantCulture
            );

        // Taksit/komisyon dahil gerçek tahsilat
        var paidPrice =
            selectedInstallment.TotalPrice.ToString(
                "0.00",
                CultureInfo.InvariantCulture
            );

        var conversationId =
            Guid.NewGuid().ToString();

        var request = new CreatePaymentRequest
        {
            Locale =
                Locale.TR.ToString(),

            ConversationId =
                conversationId,

            Price =
                amount,

            PaidPrice =
                paidPrice,

            Currency =
                Currency.TRY.ToString(),

            Installment =
                requestedInstallment,

            BasketId =
                Guid.NewGuid().ToString(),

            PaymentChannel =
                PaymentChannel.WEB.ToString(),

            PaymentGroup =
                PaymentGroup.PRODUCT.ToString(),

            CallbackUrl =
                "http://localhost:5228/api/payments/3ds/callback"
        };

        request.PaymentCard =
            new PaymentCard
            {
                CardHolderName =
                    paymentRequest.CardHolderName,

                CardNumber =
                    cleanCardNumber,

                ExpireMonth =
                    paymentRequest.ExpireMonth,

                ExpireYear =
                    paymentRequest.ExpireYear,

                Cvc =
                    paymentRequest.Cvc,

                RegisterCard = 0
            };

        request.Buyer =
            new Buyer
            {
                Id = "BY001",

                Name = "Test",

                Surname = "User",

                GsmNumber =
                    "+905350000000",

                Email =
                    "test@example.com",

                IdentityNumber =
                    "74300864791",

                RegistrationAddress =
                    "Istanbul",

                Ip =
                    "127.0.0.1",

                City =
                    "Istanbul",

                Country =
                    "Turkey",

                ZipCode =
                    "34000"
            };

        var address =
            new Address
            {
                ContactName =
                    "Test User",

                City =
                    "Istanbul",

                Country =
                    "Turkey",

                Description =
                    "Test Address",

                ZipCode =
                    "34000"
            };

        request.ShippingAddress =
            address;

        request.BillingAddress =
            address;

        request.BasketItems =
            new List<BasketItem>
            {
                new BasketItem
                {
                    Id =
                        "BI001",

                    Name =
                        "Test Payment",

                    Category1 =
                        "Test",

                    ItemType =
                        BasketItemType
                            .VIRTUAL
                            .ToString(),

                    // Burada orijinal ürün fiyatı kalıyor.
                    Price =
                        amount
                }
            };

        try
        {
            var result =
                await ThreedsInitialize.Create(
                    request,
                    options
                );

            if (result.Status !=
                Status.SUCCESS.ToString())
            {
                payment.Status =
                    "Failed";

                payment.UpdatedAt =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync();

                throw new Exception(
                    result.ErrorMessage
                    ?? "3D Secure başlatılamadı."
                );
            }

            payment.ProviderPaymentId =
                result.PaymentId
                ?? string.Empty;

            payment.UpdatedAt =
                DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new PaymentStartResponse
            {
                PaymentId =
                    result.PaymentId
                    ?? string.Empty,

                HtmlContent =
                    result.HtmlContent
                    ?? string.Empty
            };
        }
        catch
        {
            if (payment.Status == "Pending")
            {
                payment.Status =
                    "Failed";

                payment.UpdatedAt =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            throw;
        }
    }

    public async Task<PaymentCompleteResponse> Complete3dsAsync(
        string paymentId,
        string conversationId,
        string? conversationData)
    {
        var options =
            CreateIyzicoOptions();

        var request =
            new CreateThreedsPaymentRequest
            {
                Locale =
                    Locale.TR.ToString(),

                ConversationId =
                    conversationId,

                PaymentId =
                    paymentId,

                ConversationData =
                    conversationData
            };

        var result =
            await ThreedsPayment.Create(
                request,
                options
            );

        var payment =
            await _db.Payments
                .FirstOrDefaultAsync(
                    p =>
                        p.ProviderPaymentId ==
                        paymentId
                );

        if (result.Status !=
            Status.SUCCESS.ToString())
        {
            if (payment != null)
            {
                payment.Status =
                    "Failed";

                payment.UpdatedAt =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            return new PaymentCompleteResponse
            {
                Success =
                    false,

                PaymentId =
                    paymentId,

                Message =
                    result.ErrorMessage
                    ?? "Ödeme tamamlanamadı."
            };
        }

        if (payment != null)
        {
            payment.Status =
                "Success";

            payment.UpdatedAt =
                DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        return new PaymentCompleteResponse
        {
            Success =
                true,

            PaymentId =
                result.PaymentId
                ?? paymentId,

            Message =
                "Ödeme başarıyla tamamlandı."
        };
    }

    private Iyzipay.Options CreateIyzicoOptions()
    {
        return new Iyzipay.Options
        {
            ApiKey =
                _settings.ApiKey,

            SecretKey =
                _settings.SecretKey,

            BaseUrl =
                _settings.BaseUrl
        };
    }
}
