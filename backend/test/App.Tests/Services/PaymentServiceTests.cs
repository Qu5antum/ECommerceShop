using Moq;
using Microsoft.Extensions.Logging;

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;
using App.Enum;
using Microsoft.EntityFrameworkCore;


public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _helper = new Mock<IHelperService>();
        
        _paymentService = new PaymentService(
            _paymentRepositoryMock.Object,
            _notificationRepositoryMock.Object,
            _helper.Object,
            _loggerMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task CreatePayment_SuccessfullResponseWithOrder()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testOrder = OrderTestHelper.CreateTestOrder(userId: testUser.Id, status: OrderStatus.Pending, TotalAmount: 500);

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(testOrder.Id))
            .ReturnsAsync(testOrder);

        var result = await _paymentService.CreatePaymentAsync(testUser.Id, testOrder.Id);

        result.Should().NotBeNull();
        result.OrderId.Should().Be(testOrder.Id);
        result.Status.Should().Be(PaymentStatus.Pending);
        result.Amount.Should().Be(500);

        _paymentRepositoryMock.Verify(r => r.CreateAsync(
            It.Is<Payment>(p => p.OrderId == testOrder.Id && p.Amount == 500 && p.Status == PaymentStatus.Pending)
        ), Times.Once);
        
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Exactly(7));
    }

    [Fact]
    public async Task CreatePayment_WhenOrderDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var foreignOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: otherUserId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(orderId))
            .ReturnsAsync(foreignOrder);

        Func<Task> action = async () => await _paymentService.CreatePaymentAsync(userId, orderId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Order does not belong to user");

        _paymentRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreatePayment_WhenOrderIsCancelled_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testOrder = OrderTestHelper.CreateTestOrder(userId: testUser.Id, status: OrderStatus.Cancelled);

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(testOrder.Id))
            .ReturnsAsync(testOrder);

        Func<Task> action = async () => await _paymentService.CreatePaymentAsync(testUser.Id, testOrder.Id);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Can't pay for order, order cancelled");

        _paymentRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreatePayment_WhenOrderAlreadyPaid_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testOrder = OrderTestHelper.CreateTestOrder(userId: testUser.Id, status: OrderStatus.Paid);

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(testOrder.Id))
            .ReturnsAsync(testOrder);

        Func<Task> action = async () => await _paymentService.CreatePaymentAsync(testUser.Id, testOrder.Id);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Order already paid");

        _paymentRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreatePayment_WhenDbUpdateExceptionOccurs_RollsBackAndThrowsDatabaseException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testOrder = OrderTestHelper.CreateTestOrder(userId: testUser.Id, status: OrderStatus.Pending);

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(testOrder.Id))
            .ReturnsAsync(testOrder);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database error", new Exception()));

        Func<Task> action = async () => await _paymentService.CreatePaymentAsync(testUser.Id, testOrder.Id);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not save the payment to the database.");

        _paymentRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }
}