using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Features.FidelityPoints.Services;
using RestaurantSystem.Api.Features.FidelityPoints.Interfaces;
using RestaurantSystem.Infrastructure.Data;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Api.Common.Interfaces;

namespace RestaurantSystem.IntegrationTests.Features.FidelityPoints;

[Collection("Database")]
public class FidelityPointsServiceTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private ApplicationDbContext _context = null!;
    private FidelityPointsService _service = null!;
    private Mock<IPointEarningRuleService> _ruleServiceMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Guid _testUserId;

    public FidelityPointsServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateContext();
        _ruleServiceMock = new Mock<IPointEarningRuleService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _testUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(_testUserId);

        _service = new FidelityPointsService(
            _context,
            _ruleServiceMock.Object,
            _currentUserServiceMock.Object
        );

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CalculatePointsForOrderAsync_WithApplicableRule_ReturnsCorrectPoints()
    {
        // Arrange
        var orderTotal = 50m;
        var expectedPoints = 100;
        var rule = new PointEarningRule
        {
            Id = Guid.NewGuid(),
            Name = "Test Rule",
            MinOrderAmount = 20m,
            MaxOrderAmount = 100m,
            PointsAwarded = expectedPoints,
            IsActive = true,
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };

        _ruleServiceMock
            .Setup(x => x.FindApplicableRuleAsync(orderTotal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var points = await _service.CalculatePointsForOrderAsync(orderTotal);

        // Assert
        Assert.Equal(expectedPoints, points);
    }

    [Fact]
    public async Task CalculatePointsForOrderAsync_WithNoApplicableRule_ReturnsZero()
    {
        // Arrange
        var orderTotal = 5m;

        _ruleServiceMock
            .Setup(x => x.FindApplicableRuleAsync(orderTotal, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PointEarningRule?)null);

        // Act
        var points = await _service.CalculatePointsForOrderAsync(orderTotal);

        // Assert
        Assert.Equal(0, points);
    }

    [Fact]
    public async Task AwardPointsAsync_CreatesTransactionAndUpdatesBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var points = 100;
        var orderTotal = 50m;

        // Act
        var transaction = await _service.AwardPointsAsync(userId, orderId, points, orderTotal);

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(userId, transaction.UserId);
        Assert.Equal(orderId, transaction.OrderId);
        Assert.Equal(points, transaction.Points);
        Assert.Equal(TransactionType.Earned, transaction.TransactionType);

        // Verify balance was created/updated
        var balance = await _context.FidelityPointBalances
            .FirstOrDefaultAsync(b => b.UserId == userId);

        Assert.NotNull(balance);
        Assert.Equal(points, balance.CurrentPoints);
        Assert.Equal(points, balance.TotalEarnedPoints);
        Assert.Equal(0, balance.TotalRedeemedPoints);
    }

    [Fact]
    public async Task AwardPointsAsync_MultipleAwards_AccumulatesBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var points1 = 100;
        var points2 = 50;

        // Act
        await _service.AwardPointsAsync(userId, orderId1, points1, 50m);
        await _service.AwardPointsAsync(userId, orderId2, points2, 25m);

        // Assert
        var balance = await _context.FidelityPointBalances
            .FirstOrDefaultAsync(b => b.UserId == userId);

        Assert.NotNull(balance);
        Assert.Equal(points1 + points2, balance.CurrentPoints);
        Assert.Equal(points1 + points2, balance.TotalEarnedPoints);
    }

    [Fact]
    public async Task RedeemPointsAsync_WithSufficientPoints_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var availablePoints = 200;
        var pointsToRedeem = 100;

        // First award points
        await _service.AwardPointsAsync(userId, Guid.NewGuid(), availablePoints, 100m);

        // Act
        var result = await _service.RedeemPointsAsync(userId, orderId, pointsToRedeem);

        // Assert
        Assert.NotNull(result.Transaction);
        Assert.Equal(-pointsToRedeem, result.Transaction.Points);
        Assert.Equal(TransactionType.Redeemed, result.Transaction.TransactionType);
        Assert.Equal(1m, result.DiscountAmount); // 100 points = $1

        // Verify balance
        var balance = await _context.FidelityPointBalances
            .FirstOrDefaultAsync(b => b.UserId == userId);

        Assert.NotNull(balance);
        Assert.Equal(availablePoints - pointsToRedeem, balance.CurrentPoints);
        Assert.Equal(pointsToRedeem, balance.TotalRedeemedPoints);
    }

    [Fact]
    public async Task RedeemPointsAsync_WithInsufficientPoints_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var availablePoints = 50;
        var pointsToRedeem = 100;

        // First award points
        await _service.AwardPointsAsync(userId, Guid.NewGuid(), availablePoints, 25m);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RedeemPointsAsync(userId, orderId, pointsToRedeem)
        );
    }

    [Fact]
    public async Task GetUserBalanceAsync_ReturnsCorrectBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var points = 150;

        await _service.AwardPointsAsync(userId, Guid.NewGuid(), points, 75m);

        // Act
        var balance = await _service.GetUserBalanceAsync(userId);

        // Assert
        Assert.NotNull(balance);
        Assert.Equal(userId, balance.UserId);
        Assert.Equal(points, balance.CurrentPoints);
    }

    [Fact]
    public async Task GetPointsHistoryAsync_ReturnsTransactionsOrderedByDate()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await _service.AwardPointsAsync(userId, Guid.NewGuid(), 100, 50m);
        await Task.Delay(100); // Ensure different timestamps
        await _service.AwardPointsAsync(userId, Guid.NewGuid(), 50, 25m);
        await Task.Delay(100);
        await _service.RedeemPointsAsync(userId, Guid.NewGuid(), 30);

        // Act
        var history = await _service.GetPointsHistoryAsync(userId);

        // Assert
        Assert.Equal(3, history.Count);
        Assert.True(history[0].CreatedAt >= history[1].CreatedAt);
        Assert.True(history[1].CreatedAt >= history[2].CreatedAt);
    }

    [Fact]
    public async Task AdjustPointsAsync_CreatesAdjustmentTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adjustmentPoints = 50;
        var reason = "Promotional bonus";

        // Act
        var transaction = await _service.AdjustPointsAsync(userId, adjustmentPoints, reason);

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(userId, transaction.UserId);
        Assert.Equal(adjustmentPoints, transaction.Points);
        Assert.Equal(TransactionType.AdminAdjustment, transaction.TransactionType);
        Assert.Equal(reason, transaction.Description);
    }

    [Fact]
    public async Task AdjustPointsAsync_NegativeAdjustment_CannotGoBelowZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialPoints = 30;
        var negativeAdjustment = -50;

        await _service.AwardPointsAsync(userId, Guid.NewGuid(), initialPoints, 15m);

        // Act
        await _service.AdjustPointsAsync(userId, negativeAdjustment, "Correction");

        // Assert
        var balance = await _context.FidelityPointBalances
            .FirstOrDefaultAsync(b => b.UserId == userId);

        Assert.NotNull(balance);
        Assert.Equal(0, balance.CurrentPoints); // Should not go below 0
    }

    [Theory]
    [InlineData(100, 1.0)]
    [InlineData(200, 2.0)]
    [InlineData(50, 0.5)]
    [InlineData(150, 1.5)]
    public void CalculateDiscountFromPoints_ReturnsCorrectAmount(int points, decimal expectedDiscount)
    {
        // Act
        var discount = _service.CalculateDiscountFromPoints(points);

        // Assert
        Assert.Equal(expectedDiscount, discount);
    }

    [Theory]
    [InlineData(1.0, 100)]
    [InlineData(2.5, 250)]
    [InlineData(0.5, 50)]
    [InlineData(1.99, 199)]
    public void CalculatePointsForDiscount_ReturnsCorrectPoints(decimal discountAmount, int expectedPoints)
    {
        // Act
        var points = _service.CalculatePointsForDiscount(discountAmount);

        // Assert
        Assert.Equal(expectedPoints, points);
    }

    [Fact]
    public async Task GetSystemAnalyticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        // User 1: Earn 200, redeem 50
        await _service.AwardPointsAsync(user1, Guid.NewGuid(), 200, 100m);
        await _service.RedeemPointsAsync(user1, Guid.NewGuid(), 50);

        // User 2: Earn 300
        await _service.AwardPointsAsync(user2, Guid.NewGuid(), 300, 150m);

        // Act
        var analytics = await _service.GetSystemAnalyticsAsync();

        // Assert
        Assert.Equal(500, analytics.TotalPointsIssued); // 200 + 300
        Assert.Equal(50, analytics.TotalPointsRedeemed);
        Assert.Equal(2, analytics.TotalActiveUsers);
        Assert.Equal(450, analytics.TotalPointsOutstanding); // 150 + 300
        Assert.Equal(225m, analytics.AveragePointsPerUser); // 450 / 2
        Assert.Equal(0.5m, analytics.TotalDiscountGiven); // 50 points = $0.50
    }
}
