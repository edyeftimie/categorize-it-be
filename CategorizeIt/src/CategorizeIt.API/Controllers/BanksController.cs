using CategorizeIt.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BanksController : ControllerBase
{
    private readonly IEnableBankingClient _enableBanking;

    public BanksController(IEnableBankingClient enableBanking)
    {
        _enableBanking = enableBanking;
    }

    [HttpGet]
    public async Task<IActionResult> GetRomanianBanks(CancellationToken ct)
    {
        var banks = await _enableBanking.GetAspspsAsync("RO", ct);
        return Ok(banks);
    }
}