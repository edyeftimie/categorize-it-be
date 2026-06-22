using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionSyncService _syncService;

    public TransactionsController(ITransactionService transactionService, ITransactionSyncService syncService)
    {
        _transactionService = transactionService;
        _syncService = syncService;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        var userId = GetUserId();
        var count = await _syncService.SyncAllForUserAsync(userId, ct);
        return Ok(new { newTransactions = count });
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilters filters)
    {
        var result = await _transactionService.GetTransactionsAsync(GetUserId(), filters);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var id = await _transactionService.CreateTransactionAsync(GetUserId(), request);
        return Ok(new { Id = id });
    }

    [HttpPatch("{id}/category")]
    public async Task<IActionResult> Recategorise(Guid id, [FromBody] RecategoriseRequest request)
    {
        var found = await _transactionService.RecategoriseAsync(GetUserId(), id, request.CategoryId);
        return found ? NoContent() : NotFound();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}