using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentApi.Data;
using PaymentApi.Models;
using PaymentApi.Options;
using PaymentApi.Services;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly AppDbContext _db;
    private readonly IyzicoOptions _iyzicoOptions;

    public PaymentsController(
        IPaymentService paymentService,
        AppDbContext db,
        IOptions<IyzicoOptions> iyzicoOptions)
    {
        _paymentService = paymentService;
        _db = db;
        _iyzicoOptions = iyzicoOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments()
    {
        var payments = await _db.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(payments);
    }

    [HttpPost("3ds/start")]
    public async Task<IActionResult> Start3ds(
        [FromBody] PaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Tutar 0'dan büyük olmalıdır."
            });
        }

        if (string.IsNullOrWhiteSpace(request.CardHolderName))
        {
            return BadRequest(new
            {
                message = "Kart sahibi adı zorunludur."
            });
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            return BadRequest(new
            {
                message = "Kart numarası zorunludur."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ExpireMonth) ||
            string.IsNullOrWhiteSpace(request.ExpireYear))
        {
            return BadRequest(new
            {
                message = "Son kullanma tarihi zorunludur."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Cvc))
        {
            return BadRequest(new
            {
                message = "CVV zorunludur."
            });
        }

        try
        {
            var result =
                await _paymentService.Start3dsAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("3ds/callback")]
    public async Task<IActionResult> Callback(
        [FromForm] string? status,
        [FromForm] string? paymentId,
        [FromForm] string? conversationId,
        [FromForm] string? conversationData,
        [FromForm] string? mdStatus,
        [FromForm] string? signature)
    {
        Console.WriteLine(
            $"3DS Callback => " +
            $"status={status}, " +
            $"paymentId={paymentId}, " +
            $"conversationId={conversationId}, " +
            $"mdStatus={mdStatus}"
        );

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return HtmlPage(
                "Ödeme Başarısız",
                "Payment ID alınamadı.",
                false
            );
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            return HtmlPage(
                "Ödeme Başarısız",
                "Callback imzası bulunamadı.",
                false
            );
        }

        var signatureValid =
            ValidateCallbackSignature(
                conversationData,
                conversationId,
                mdStatus,
                paymentId,
                status,
                signature
            );

        if (!signatureValid)
        {
            return HtmlPage(
                "Ödeme Başarısız",
                "Callback imzası doğrulanamadı.",
                false
            );
        }

        if (!string.Equals(
                status,
                "success",
                StringComparison.OrdinalIgnoreCase) ||
            mdStatus != "1")
        {
            return HtmlPage(
                "Ödeme Başarısız",
                $"3D Secure doğrulaması tamamlanamadı. mdStatus: {mdStatus}",
                false
            );
        }

        try
        {
            var result =
                await _paymentService.Complete3dsAsync(
                    paymentId,
                    conversationId ?? string.Empty,
                    conversationData
                );

            if (!result.Success)
            {
                return HtmlPage(
                    "Ödeme Başarısız",
                    result.Message,
                    false
                );
            }

            return HtmlPage(
                "Ödeme Başarılı",
                $"3D Secure ödeme başarıyla tamamlandı. Payment ID: {result.PaymentId}",
                true
            );
        }
        catch (Exception ex)
        {
            return HtmlPage(
                "Ödeme Başarısız",
                ex.Message,
                false
            );
        }
    }

    private bool ValidateCallbackSignature(
        string? conversationData,
        string? conversationId,
        string? mdStatus,
        string? paymentId,
        string? status,
        string signature)
    {
        var dataToSign =
            $"{conversationData ?? string.Empty}:" +
            $"{conversationId ?? string.Empty}:" +
            $"{mdStatus ?? string.Empty}:" +
            $"{paymentId ?? string.Empty}:" +
            $"{status ?? string.Empty}";

        var keyBytes =
            Encoding.UTF8.GetBytes(
                _iyzicoOptions.SecretKey
            );

        var dataBytes =
            Encoding.UTF8.GetBytes(
                dataToSign
            );

        using var hmac =
            new HMACSHA256(keyBytes);

        var hash =
            hmac.ComputeHash(dataBytes);

        var calculatedSignature =
            Convert
                .ToHexString(hash)
                .ToLowerInvariant();

        return string.Equals(
            calculatedSignature,
            signature,
            StringComparison.OrdinalIgnoreCase
        );
    }

    private ContentResult HtmlPage(
        string title,
        string message,
        bool success)
    {
        var icon = success ? "✅" : "❌";

        var html = $"""
        <!DOCTYPE html>
        <html lang="tr">

        <head>
            <meta charset="utf-8">

            <meta
                name="viewport"
                content="width=device-width, initial-scale=1"
            >

            <title>{title}</title>
        </head>

        <body style="
            margin: 0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: #f4f5f7;
            font-family: Arial, sans-serif;
        ">

            <div style="
                width: 420px;
                max-width: calc(100vw - 40px);
                background: white;
                padding: 40px;
                border-radius: 20px;
                text-align: center;
                box-shadow: 0 18px 50px rgba(0,0,0,.08);
            ">

                <div style="
                    font-size: 50px;
                    margin-bottom: 20px;
                ">
                    {icon}
                </div>

                <h1>
                    {title}
                </h1>

                <p style="
                    line-height: 1.6;
                    color: #555;
                ">
                    {message}
                </p>

                <p style="
                    margin-top: 30px;
                    font-size: 13px;
                    color: #999;
                ">
                    Bu pencereyi kapatabilirsiniz.
                </p>

            </div>

        </body>
        </html>
        """;

        return Content(
            html,
            "text/html; charset=utf-8"
        );
    }
}
