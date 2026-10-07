using Microsoft.AspNetCore.Mvc;
using PaymentApi.Models;
using PaymentApi.Services;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/cards")]
public class CardsController : ControllerBase
{
    private readonly IBinService _binService;
    private readonly IInstallmentService _installmentService;

    public CardsController(
        IBinService binService,
        IInstallmentService installmentService)
    {
        _binService = binService;
        _installmentService = installmentService;
    }

    [HttpPost("bin-lookup")]
    public async Task<IActionResult> BinLookup(
        [FromBody] BinLookupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Bin) ||
            request.Bin.Length != 8 ||
            !request.Bin.All(char.IsDigit))
        {
            return BadRequest(new
            {
                message = "BIN 8 haneli sayısal bir değer olmalıdır."
            });
        }

        try
        {
            var response =
                await _binService.LookupAsync(request.Bin);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("installments")]
    public async Task<IActionResult> Installments(
        [FromBody] InstallmentLookupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Bin) ||
            request.Bin.Length != 8 ||
            !request.Bin.All(char.IsDigit))
        {
            return BadRequest(new
            {
                message = "BIN 8 haneli sayısal bir değer olmalıdır."
            });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Tutar 0'dan büyük olmalıdır."
            });
        }

        try
        {
            var result =
                await _installmentService.LookupAsync(
                    request.Bin,
                    request.Amount
                );

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
}
