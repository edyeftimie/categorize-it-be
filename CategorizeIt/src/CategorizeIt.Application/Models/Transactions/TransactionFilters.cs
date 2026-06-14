namespace CategorizeIt.Application.Models.Transactions;

public class TransactionFilters
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public bool? IsExpense { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}