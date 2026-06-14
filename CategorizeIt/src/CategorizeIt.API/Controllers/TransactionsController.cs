using System.Security.Claims;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.Transactions;
using CategorizeIt.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CategorizeIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactions;

    public TransactionsController(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilters filters)
    {
        var userId = GetUserId();
        var transactions = await _transactions.GetByUserIdAsync(userId, filters);

        var result = transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            BankAccountId = t.BankAccountId,
            EntryReference = t.EntryReference,
            Amount = t.Amount,
            Currency = t.Currency,
            IsExpense = t.IsExpense,
            BookingDate = t.BookingDate,
            MerchantName = t.MerchantName,
            Description = t.Description,
            CategoryId = t.CategoryId,
            CategoryName = t.Category?.Name,
            CategoryColor = t.Category?.Color,
            CategoryIcon = t.Category?.Icon,
            IsManual = t.IsManual,
            IsRecurring = t.IsRecurring,
            CreatedAt = t.CreatedAt
        });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var userId = GetUserId();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = request.Amount,
            Currency = request.Currency,
            IsExpense = request.IsExpense,
            BookingDate = request.BookingDate,
            MerchantName = request.MerchantName,
            Description = request.Description,
            CategoryId = request.CategoryId,
            IsManual = true
        };

        await _transactions.CreateAsync(transaction);
        return Ok(new { transaction.Id });
    }

    [HttpPatch("{id}/category")]
    public async Task<IActionResult> Recategorise(Guid id, [FromBody] RecategoriseRequest request)
    {
        var userId = GetUserId();
        var transaction = await _transactions.GetByIdAsync(id);

        if (transaction == null || transaction.UserId != userId)
            return NotFound();

        transaction.CategoryId = request.CategoryId;
        await _transactions.UpdateAsync(transaction);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}