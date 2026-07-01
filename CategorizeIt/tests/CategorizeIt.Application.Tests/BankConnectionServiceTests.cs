using Xunit;
using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Models.EnableBanking;
using CategorizeIt.Application.Services;
using CategorizeIt.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CategorizeIt.Application.Tests;

public class BankConnectionServiceTests
{
    private readonly Mock<IEnableBankingClient>      _ebClient = new();
    private readonly Mock<IBankConnectionRepository> _connRepo = new();

    private BankConnectionService CreateSut() => new(_ebClient.Object, _connRepo.Object);

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ConnId = Guid.NewGuid();

    private static CreateSessionResponseDto MakeSession(string hash, string uid = "uid-1") => new()
    {
        SessionId = "sess-abc",
        Aspsp     = new AspspRefDto { Name = "BRD", Country = "RO" },
        PsuType   = "personal",
        Access    = new SessionAccessDto { ValidUntil = DateTime.UtcNow.AddDays(90) },
        Accounts  = new List<SessionAccountDto>
        {
            new() { Uid = uid, IdentificationHash = hash, Name = "Main", Currency = "RON" }
        }
    };

    // ── InitiateAuthAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task InitiateAuthAsync_ReturnsUrlAndState()
    {
        _ebClient.Setup(c => c.StartAuthorizationAsync(
                     "BRD", "RO", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StartAuthResponseDto { Url = "https://auth.example.com/go" });

        var result = await CreateSut().InitiateAuthAsync("BRD", "RO", CancellationToken.None);

        result.Url.Should().Be("https://auth.example.com/go");
        result.State.Should().NotBeNullOrEmpty();
    }

    // ── HandleCallbackAsync — CREATE path ─────────────────────────────────────

    [Fact]
    public async Task HandleCallbackAsync_NoExistingAccounts_CreatesNewConnection()
    {
        var session = MakeSession("hash-new");
        _ebClient.Setup(c => c.CreateSessionAsync("code-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(session);
        _connRepo.Setup(r => r.GetAccountsByIdentificationHashesForUserAsync(UserId, It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync(new List<BankAccount>());

        var result = await CreateSut().HandleCallbackAsync(UserId, "code-1", CancellationToken.None);

        _connRepo.Verify(r => r.CreateAsync(It.Is<BankConnection>(c =>
            c.UserId == UserId &&
            c.Status == "AUTHORIZED" &&
            c.BankAccounts.Count == 1)), Times.Once);
        result.AspspName.Should().Be("BRD");
        result.Status.Should().Be("AUTHORIZED");
    }

    // ── HandleCallbackAsync — REACTIVATE path ─────────────────────────────────

    [Fact]
    public async Task HandleCallbackAsync_ExistingAccountByHash_ReactivatesConnection()
    {
        const string hash = "hash-existing";
        var existingConn = new BankConnection
        {
            Id = ConnId, UserId = UserId, Status = "DISCONNECTED",
            AspspName = "BRD", AspspCountry = "RO",
            BankAccounts = new List<BankAccount>()
        };
        var existingAccount = new BankAccount
        {
            Id = Guid.NewGuid(), BankConnectionId = ConnId,
            IdentificationHash = hash, Uid = "old-uid", BankConnection = existingConn
        };
        existingConn.BankAccounts = new List<BankAccount> { existingAccount };

        var session = MakeSession(hash, uid: "new-uid");
        _ebClient.Setup(c => c.CreateSessionAsync("code-reactivate", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(session);
        _connRepo.Setup(r => r.GetAccountsByIdentificationHashesForUserAsync(UserId, It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync(new List<BankAccount> { existingAccount });

        var result = await CreateSut().HandleCallbackAsync(UserId, "code-reactivate", CancellationToken.None);

        _connRepo.Verify(r => r.UpdateAsync(It.Is<BankConnection>(c => c.Status == "AUTHORIZED")), Times.Once);
        _connRepo.Verify(r => r.CreateAsync(It.IsAny<BankConnection>()), Times.Never);
        existingAccount.Uid.Should().Be("new-uid");
        result.Status.Should().Be("AUTHORIZED");
    }

    // ── HandleCallbackAsync — REACTIVATE: new account added to existing conn ──

    [Fact]
    public async Task HandleCallbackAsync_IncomingAccountHashNotMatchingExisting_AddsNewAccountToConnection()
    {
        const string existingHash = "hash-existing";
        const string newHash      = "hash-brand-new";

        var existingConn = new BankConnection
        {
            Id = ConnId, UserId = UserId, Status = "DISCONNECTED",
            AspspName = "BRD", AspspCountry = "RO",
            BankAccounts = new List<BankAccount>()
        };
        var existingAccount = new BankAccount
        {
            Id = Guid.NewGuid(), BankConnectionId = ConnId,
            IdentificationHash = existingHash, BankConnection = existingConn
        };
        existingConn.BankAccounts = new List<BankAccount> { existingAccount };

        var session = new CreateSessionResponseDto
        {
            SessionId = "sess-x", PsuType = "personal",
            Aspsp  = new AspspRefDto { Name = "BRD", Country = "RO" },
            Access = new SessionAccessDto { ValidUntil = DateTime.UtcNow.AddDays(90) },
            Accounts = new List<SessionAccountDto>
            {
                new() { Uid = "uid-1", IdentificationHash = existingHash, Currency = "RON" },
                new() { Uid = "uid-2", IdentificationHash = newHash,      Currency = "RON" }
            }
        };

        _ebClient.Setup(c => c.CreateSessionAsync("code-x", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(session);
        _connRepo.Setup(r => r.GetAccountsByIdentificationHashesForUserAsync(UserId, It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync(new List<BankAccount> { existingAccount });

        await CreateSut().HandleCallbackAsync(UserId, "code-x", CancellationToken.None);

        existingConn.BankAccounts.Should().Contain(a => a.IdentificationHash == newHash);
    }

    // ── GetConnectionsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetConnectionsAsync_ReturnsAllConnectionsAsDtos()
    {
        var conn = new BankConnection
        {
            Id = ConnId, UserId = UserId, AspspName = "BRD", AspspCountry = "RO",
            Status = "AUTHORIZED", ValidUntil = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            BankAccounts = new List<BankAccount>()
        };
        _connRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<BankConnection> { conn });

        var result = (await CreateSut().GetConnectionsAsync(UserId)).ToList();

        result.Should().HaveCount(1);
        result[0].AspspName.Should().Be("BRD");
    }

    // ── DeleteConnectionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteConnectionAsync_ValidOwner_DeletesAndReturnsTrue()
    {
        var conn = new BankConnection { Id = ConnId, UserId = UserId };
        _connRepo.Setup(r => r.GetByIdAsync(ConnId)).ReturnsAsync(conn);

        var result = await CreateSut().DeleteConnectionAsync(UserId, ConnId);

        result.Should().BeTrue();
        _connRepo.Verify(r => r.DeleteAsync(conn), Times.Once);
    }

    [Fact]
    public async Task DeleteConnectionAsync_ConnectionNotFound_ReturnsFalse()
    {
        _connRepo.Setup(r => r.GetByIdAsync(ConnId)).ReturnsAsync((BankConnection?)null);

        var result = await CreateSut().DeleteConnectionAsync(UserId, ConnId);

        result.Should().BeFalse();
        _connRepo.Verify(r => r.DeleteAsync(It.IsAny<BankConnection>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConnectionAsync_WrongUser_ReturnsFalse()
    {
        var conn = new BankConnection { Id = ConnId, UserId = Guid.NewGuid() };
        _connRepo.Setup(r => r.GetByIdAsync(ConnId)).ReturnsAsync(conn);

        var result = await CreateSut().DeleteConnectionAsync(UserId, ConnId);

        result.Should().BeFalse();
    }
}