using CategorizeIt.Application.Interfaces;
using CategorizeIt.Domain.Enums;

namespace CategorizeIt.Application.Services;

public class MccCategoriser : IMccCategoriser
{
    private static readonly Dictionary<string, (string CategoryName, NeedWantSavings Classification)> _map = new()
    {
        { "5411", ("Food & Dining",   NeedWantSavings.Need) },
        { "5812", ("Food & Dining",   NeedWantSavings.Want) },
        { "5814", ("Food & Dining",   NeedWantSavings.Want) },
        { "5541", ("Transport",       NeedWantSavings.Need) },
        { "5542", ("Transport",       NeedWantSavings.Need) },
        { "4111", ("Transport",       NeedWantSavings.Need) },
        { "4121", ("Transport",       NeedWantSavings.Need) },
        { "4131", ("Transport",       NeedWantSavings.Need) },
        { "5311", ("Shopping",        NeedWantSavings.Want) },
        { "5331", ("Shopping",        NeedWantSavings.Want) },
        { "5399", ("Shopping",        NeedWantSavings.Want) },
        { "5912", ("Health",          NeedWantSavings.Need) },
        { "7832", ("Entertainment",   NeedWantSavings.Want) },
        { "7841", ("Entertainment",   NeedWantSavings.Want) },
        { "4899", ("Subscriptions",   NeedWantSavings.Want) },
        { "5968", ("Subscriptions",   NeedWantSavings.Want) },
        { "6011", ("Cash",            NeedWantSavings.Uncategorised) },
        { "4829", ("Transfer",        NeedWantSavings.Excluded) },
        { "6012", ("Transfer",        NeedWantSavings.Excluded) },
    };

    public (string CategoryName, NeedWantSavings Classification) Classify(string? mccCode)
    {
        if (mccCode != null && _map.TryGetValue(mccCode, out var result))
            return result;

        return ("Other", NeedWantSavings.Uncategorised);
    }
}