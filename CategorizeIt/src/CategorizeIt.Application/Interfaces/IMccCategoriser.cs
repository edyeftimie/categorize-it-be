using CategorizeIt.Domain.Enums;

namespace CategorizeIt.Application.Interfaces;

public interface IMccCategoriser
{
    (string CategoryName, NeedWantSavings Classification) Classify(string? mccCode);
}