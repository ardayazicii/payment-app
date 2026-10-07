using PaymentApi.Models;

namespace PaymentApi.Services;

public interface IBinService
{
    Task<BinLookupResponse> LookupAsync(string bin);
}