using System.Text.Json.Serialization;

namespace CategorizeIt.Application.Models.EnableBanking;

public class TransactionsPageDto
{
    [JsonPropertyName("transactions")]
    public List<EnableBankingTransactionDto> Transactions { get; set; } = new();

    [JsonPropertyName("continuation_key")]
    public string? ContinuationKey { get; set; }
}

public class EnableBankingTransactionDto
{
    [JsonPropertyName("entry_reference")]
    public string? EntryReference { get; set; }

    [JsonPropertyName("merchant_category_code")]
    public string? MerchantCategoryCode { get; set; }

    [JsonPropertyName("transaction_amount")]
    public TransactionAmountDto TransactionAmount { get; set; } = new();

    [JsonPropertyName("creditor")]
    public PartyDto? Creditor { get; set; }

    [JsonPropertyName("debtor")]
    public PartyDto? Debtor { get; set; }

    [JsonPropertyName("credit_debit_indicator")]
    public string CreditDebitIndicator { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("booking_date")]
    public string BookingDate { get; set; } = string.Empty;

    [JsonPropertyName("remittance_information")]
    public List<string>? RemittanceInformation { get; set; }
}

public class TransactionAmountDto
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;
}

public class PartyDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}