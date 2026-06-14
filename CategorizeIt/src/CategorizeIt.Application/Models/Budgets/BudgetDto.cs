namespace CategorizeIt.Application.Models.Budgets;

public class BudgetDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal AmountSpent { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class CreateBudgetRequest
{
    public Guid CategoryId { get; set; }
    public decimal MonthlyLimit { get; set; }
    public string Currency { get; set; } = "RON";
}

public class UpdateBudgetRequest
{
    public decimal MonthlyLimit { get; set; }
}