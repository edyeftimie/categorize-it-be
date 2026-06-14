namespace CategorizeIt.Application.Models.Dashboard;

public class CategorySpendingDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public string? CategoryIcon { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class NeedWantSplitDto
{
    public decimal NeedAmount { get; set; }
    public decimal WantAmount { get; set; }
    public decimal SavingsAmount { get; set; }
    public decimal UncategorisedAmount { get; set; }
}

public class MonthlyAmountDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Total { get; set; }
}

public class DashboardDto
{
    public decimal TotalBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public List<CategorySpendingDto> TopCategories { get; set; } = new();
    public NeedWantSplitDto NeedWantSplit { get; set; } = new();
}