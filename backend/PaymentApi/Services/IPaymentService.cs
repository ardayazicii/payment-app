using PaymentApi.Models;

namespace PaymentApi.Services;

public interface IPaymentService
{
    Task<PaymentStartResponse> Start3dsAsync(PaymentRequest request);

    Task<PaymentCompleteResponse> Complete3dsAsync(
        string paymentId,
        string conversationId,
        string? conversationData
    );
}