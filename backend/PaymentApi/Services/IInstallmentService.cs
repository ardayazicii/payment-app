using PaymentApi.Models;

namespace PaymentApi.Services;

public interface IInstallmentService
{
    Task<InstallmentLookupResponse> LookupAsync(
        string bin,
        decimal amount
    );
}