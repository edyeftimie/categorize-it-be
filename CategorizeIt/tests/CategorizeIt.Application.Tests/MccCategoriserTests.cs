using Xunit;
using CategorizeIt.Application.Services;
using CategorizeIt.Domain.Enums;
using FluentAssertions;

namespace CategorizeIt.Application.Tests;

public class MccCategoriserTests
{
    private readonly MccCategoriser _sut = new();

    [Fact]
    public void Classify_NullMccCode_ReturnsOtherUncategorised()
    {
        var result = _sut.Classify(null);

        result.CategoryName.Should().Be("Other");
        result.Classification.Should().Be(NeedWantSavings.Uncategorised);
    }

    [Fact]
    public void Classify_UnknownCode_ReturnsOtherUncategorised()
    {
        var result = _sut.Classify("9999");

        result.CategoryName.Should().Be("Other");
        result.Classification.Should().Be(NeedWantSavings.Uncategorised);
    }

    [Fact]
    public void Classify_EmptyString_ReturnsOtherUncategorised()
    {
        var result = _sut.Classify("");

        result.CategoryName.Should().Be("Other");
        result.Classification.Should().Be(NeedWantSavings.Uncategorised);
    }

    // ── Food & Dining ─────────────────────────────────────────────────────────

    [Fact]
    public void Classify_5411_ReturnsFoodDiningNeed()
    {
        var (cat, cls) = _sut.Classify("5411");
        cat.Should().Be("Food & Dining");
        cls.Should().Be(NeedWantSavings.Need);
    }

    [Fact]
    public void Classify_5812_ReturnsFoodDiningWant()
    {
        var (cat, cls) = _sut.Classify("5812");
        cat.Should().Be("Food & Dining");
        cls.Should().Be(NeedWantSavings.Want);
    }

    [Fact]
    public void Classify_5814_ReturnsFoodDiningWant()
    {
        var (cat, cls) = _sut.Classify("5814");
        cat.Should().Be("Food & Dining");
        cls.Should().Be(NeedWantSavings.Want);
    }

    // ── Transport ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("5541")]
    [InlineData("5542")]
    [InlineData("4111")]
    [InlineData("4121")]
    [InlineData("4131")]
    public void Classify_TransportCode_ReturnsTransportNeed(string mcc)
    {
        var (cat, cls) = _sut.Classify(mcc);
        cat.Should().Be("Transport");
        cls.Should().Be(NeedWantSavings.Need);
    }

    // ── Shopping ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("5311")]
    [InlineData("5331")]
    [InlineData("5399")]
    public void Classify_ShoppingCode_ReturnsShoppingWant(string mcc)
    {
        var (cat, cls) = _sut.Classify(mcc);
        cat.Should().Be("Shopping");
        cls.Should().Be(NeedWantSavings.Want);
    }

    // ── Health ────────────────────────────────────────────────────────────────

    [Fact]
    public void Classify_5912_ReturnsHealthNeed()
    {
        var (cat, cls) = _sut.Classify("5912");
        cat.Should().Be("Health");
        cls.Should().Be(NeedWantSavings.Need);
    }

    // ── Entertainment ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("7832")]
    [InlineData("7841")]
    public void Classify_EntertainmentCode_ReturnsEntertainmentWant(string mcc)
    {
        var (cat, cls) = _sut.Classify(mcc);
        cat.Should().Be("Entertainment");
        cls.Should().Be(NeedWantSavings.Want);
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("4899")]
    [InlineData("5968")]
    public void Classify_SubscriptionCode_ReturnsSubscriptionsWant(string mcc)
    {
        var (cat, cls) = _sut.Classify(mcc);
        cat.Should().Be("Subscriptions");
        cls.Should().Be(NeedWantSavings.Want);
    }

    // ── Cash ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Classify_6011_ReturnsCashUncategorised()
    {
        var (cat, cls) = _sut.Classify("6011");
        cat.Should().Be("Cash");
        cls.Should().Be(NeedWantSavings.Uncategorised);
    }

    // ── Transfer ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("4829")]
    [InlineData("6012")]
    public void Classify_TransferCode_ReturnsTransferExcluded(string mcc)
    {
        var (cat, cls) = _sut.Classify(mcc);
        cat.Should().Be("Transfer");
        cls.Should().Be(NeedWantSavings.Excluded);
    }
}