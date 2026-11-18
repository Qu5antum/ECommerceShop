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

    [Fact]
    public async Task GetPaymentAsync_WhenDataIsInCacheAndValid_ReturnsCachedPaymentWithoutHittingDatabase()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: userId);

        var cachedPaymentDto = new PaymentResponseDto
        {
            Id = paymentId,
            OrderId = orderId,
            Amount = 1500,
            Status = PaymentStatus.Succeeded,
            ProviderPaymentId = "mock_cache_123",
            CreatedAt = DateTime.UtcNow
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(orderId))
            .ReturnsAsync(testOrder);

        _cacheMock.Setup(c => c.GetDataAsync<PaymentResponseDto>($"payment:{paymentId}:order:{orderId}"))
            .ReturnsAsync(cachedPaymentDto);

        var result = await _paymentService.GetPaymentAsync(userId, orderId, paymentId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedPaymentDto);

        _helper.Verify(h => h.GetUserOr404(userId), Times.Once);
        _helper.Verify(h => h.GetOrderOr404(orderId), Times.Once);
        _helper.Verify(h => h.GetPaymentOr404(It.IsAny<Guid>()), Times.Never);
        
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<PaymentResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentAsync_WhenCacheMissAndValidData_FetchesFromDbCachesAndReturnsPayment()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: userId);
        var testPayment = PaymentTestHelper.CreateTestPayment(
            Id: paymentId, 
            OrderId: orderId, 
            Amount: 3000,
            paymentStatus: PaymentStatus.Pending
        );

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(orderId))
            .ReturnsAsync(testOrder);

        _cacheMock.Setup(c => c.GetDataAsync<PaymentResponseDto>($"payment:{paymentId}:order:{orderId}"))
            .ReturnsAsync((PaymentResponseDto?)null);

        _helper.Setup(h => h.GetPaymentOr404(paymentId))
            .ReturnsAsync(testPayment);

        var result = await _paymentService.GetPaymentAsync(userId, orderId, paymentId);

        result.Should().NotBeNull();
        result.Id.Should().Be(paymentId);
        result.OrderId.Should().Be(orderId);
        result.Amount.Should().Be(3000);

        _helper.Verify(h => h.GetPaymentOr404(paymentId), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"payment:{paymentId}:order:{orderId}",
            It.Is<PaymentResponseDto>(dto => dto.Id == paymentId && dto.Amount == 3000),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetPaymentAsync_WhenOrderDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var foreignOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: otherUserId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(orderId))
            .ReturnsAsync(foreignOrder);

        Func<Task> action = async () => await _paymentService.GetPaymentAsync(userId, orderId, paymentId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Order does not belong to user");

        _cacheMock.Verify(c => c.GetDataAsync<PaymentResponseDto>(It.IsAny<string>()), Times.Never);
        _helper.Verify(h => h.GetPaymentOr404(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentAsync_WhenCachedPaymentDoesNotBelongToOrder_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var otherOrderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: userId);

        var cachedPaymentDto = new PaymentResponseDto
        {
            Id = paymentId,
            OrderId = otherOrderId,
            Amount = 1000
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(orderId))
            .ReturnsAsync(testOrder);

        _cacheMock.Setup(c => c.GetDataAsync<PaymentResponseDto>($"payment:{paymentId}:order:{orderId}"))
            .ReturnsAsync(cachedPaymentDto);

        Func<Task> action = async () => await _paymentService.GetPaymentAsync(userId, orderId, paymentId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Payment does not belong to order");

        _helper.Verify(h => h.GetPaymentOr404(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentAsync_WhenPaymentFromDbDoesNotBelongToOrder_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var otherOrderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: userId);
        var foreignPayment = PaymentTestHelper.CreateTestPayment(Id: paymentId, OrderId: otherOrderId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetOrderOr404(orderId))
            .ReturnsAsync(testOrder);

        _cacheMock.Setup(c => c.GetDataAsync<PaymentResponseDto>($"payment:{paymentId}:order:{orderId}"))
            .ReturnsAsync((PaymentResponseDto?)null);

        _helper.Setup(h => h.GetPaymentOr404(paymentId))
            .ReturnsAsync(foreignPayment);

        Func<Task> action = async () => await _paymentService.GetPaymentAsync(userId, orderId, paymentId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Payment does not belong to order");

        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<PaymentResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task ProccessWebhook_SuccessfullResponse_ReturnsOk()
    {
        var providerPaymentId = $"mock_{Guid.NewGuid()}";
        var testOrder = OrderTestHelper.CreateTestOrder();
        var testPayment = PaymentTestHelper.CreateTestPayment(OrderId: testOrder.Id, ProviderPaymentId: providerPaymentId);

        var testWebhookDto = new PaymentWebhookDto
        {
            ProviderPaymentId = providerPaymentId,
            Status = PaymentStatus.Succeeded
        };

        _paymentRepositoryMock.Setup(r => r.GetPaymentWithProviderId(testWebhookDto.ProviderPaymentId))
            .ReturnsAsync(testPayment);

        _helper.Setup(h => h.GetOrderOr404(testPayment.OrderId))
            .ReturnsAsync(testOrder);

        var result = await _paymentService.ProcessWebhookAsync(testWebhookDto);

        result.Should().BeTrue();
        testPayment.Status.Should().Be(PaymentStatus.Succeeded);

        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Exactly(6));
    }

    [Fact]
    public async Task ProccessWebhook_IfPaymentNotFoundByProvider_ThrowsNotFoundException()
    {
        var providerPaymentId = $"mock_{Guid.NewGuid()}";
        var testOrder = OrderTestHelper.CreateTestOrder();
        var testPayment = PaymentTestHelper.CreateTestPayment(OrderId: testOrder.Id);

        var testWebhookDto = new PaymentWebhookDto
        {
            ProviderPaymentId = providerPaymentId,
            Status = PaymentStatus.Succeeded
        };

        _paymentRepositoryMock.Setup(r => r.GetPaymentWithProviderId(testWebhookDto.ProviderPaymentId))
            .ReturnsAsync((Payment?)null);

        Func<Task> action = async () => await _paymentService.ProcessWebhookAsync(testWebhookDto);

        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WhenStatusTransitionIsInvalid_ThrowsBadRequestException()
    {
        var webhookDto = new PaymentWebhookDto
        {
            ProviderPaymentId = "mock_provider_123",
            Status = PaymentStatus.Pending 
        };

        var payment = PaymentTestHelper.CreateTestPayment(
            paymentStatus: PaymentStatus.Succeeded 
        );

        _paymentRepositoryMock.Setup(r => r.GetPaymentWithProviderId(webhookDto.ProviderPaymentId))
            .ReturnsAsync(payment);

        Func<Task> action = async () => await _paymentService.ProcessWebhookAsync(webhookDto);

        await action.Should().ThrowAsync<BadRequestException>();

        _helper.Verify(h => h.GetOrderOr404(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WhenDbUpdateExceptionOccurs_RollsBackAndThrowsDatabaseException()
    {
        var webhookDto = new PaymentWebhookDto
        {
            ProviderPaymentId = "mock_provider_123",
            Status = PaymentStatus.Succeeded
        };

        var orderId = Guid.NewGuid();
        var payment = PaymentTestHelper.CreateTestPayment(OrderId: orderId, paymentStatus: PaymentStatus.Pending);
        var order = OrderTestHelper.CreateTestOrder(Id: orderId, status: OrderStatus.Pending);

        _paymentRepositoryMock.Setup(r => r.GetPaymentWithProviderId(webhookDto.ProviderPaymentId))
            .ReturnsAsync(payment);

        _helper.Setup(h => h.GetOrderOr404(orderId))
            .ReturnsAsync(order);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("DB Error", new Exception()));

        Func<Task> action = async () => await _paymentService.ProcessWebhookAsync(webhookDto);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not process payment webhook in the database.");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsAdminAsync_WhenDataIsInCache_ReturnsCachedPaymentsWithoutHittingRepository()
    {
        var status = PaymentStatus.Succeeded;

        var cachedPayments = new List<PaymentResponseDto>
        {
            new PaymentResponseDto
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 1500,
                Status = PaymentStatus.Succeeded,
                ProviderPaymentId = "mock_provider_123",
                CreatedAt = DateTime.UtcNow
            }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<PaymentResponseDto>>($"payments:{status}"))
            .ReturnsAsync(cachedPayments);

        var result = await _paymentService.GetPaymentsAdminAsync(status);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedPayments);

        _paymentRepositoryMock.Verify(r => r.GetPaymetsByStatusAsync(It.IsAny<PaymentStatus?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<List<PaymentResponseDto>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentsAdminAsync_WhenCacheMiss_FetchesFromRepositoryAndCaches()
    {
        var dbPayments = new List<PaymentResponseDto>
        {
            new PaymentResponseDto
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Amount = 1500,
                Status = PaymentStatus.Pending,
                ProviderPaymentId = "mock_provider_123",
                CreatedAt = DateTime.UtcNow
            }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<PaymentResponseDto>>("payments:all"))
            .ReturnsAsync((List<PaymentResponseDto>?)null);

        _paymentRepositoryMock.Setup(r => r.GetPaymetsByStatusAsync())
            .ReturnsAsync(dbPayments);

        var result = await _paymentService.GetPaymentsAdminAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Amount.Should().Be(1500);
        result.First().Status.Should().Be(PaymentStatus.Pending);

        _paymentRepositoryMock.Verify(r => r.GetPaymetsByStatusAsync(), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            "payments:all",
            It.Is<List<PaymentResponseDto>>(list => list.Count == 1 && list[0].Amount == 1500),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetPaymentAdminAsync_WhenDataIsInCache_ReturnsCachedPaymentWithoutHittingDatabase()
    {
        var paymentId = Guid.NewGuid();

        var cachedPaymentDto = new PaymentResponseDto
        {
            Id = paymentId,
            OrderId = Guid.NewGuid(),
            Amount = 4500,
            Status = PaymentStatus.Succeeded,
            ProviderPaymentId = "mock_admin_123",
            CreatedAt = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.GetDataAsync<PaymentResponseDto>($"payment:{paymentId}"))
            .ReturnsAsync(cachedPaymentDto);

        var result = await _paymentService.GetPaymentAdminAsync(paymentId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedPaymentDto);

        _helper.Verify(h => h.GetPaymentOr404(It.IsAny<Guid>()), Times.Never);
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<PaymentResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentAdminAsync_WhenCacheMiss_FetchesFromDbCachesAndReturnsPayment()
    {
        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var testPayment = PaymentTestHelper.CreateTestPayment(
            Id: paymentId,
            OrderId: orderId,
            Amount: 5000,
            paymentStatus: PaymentStatus.Pending
        );

        _cacheMock.Setup(c => c.GetDataAsync<PaymentResponseDto>($"payment:{paymentId}"))
            .ReturnsAsync((PaymentResponseDto?)null);

        _helper.Setup(h => h.GetPaymentOr404(paymentId))
            .ReturnsAsync(testPayment);

        var result = await _paymentService.GetPaymentAdminAsync(paymentId);

        result.Should().NotBeNull();
        result.Id.Should().Be(paymentId);
        result.Amount.Should().Be(5000);

        _helper.Verify(h => h.GetPaymentOr404(paymentId), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"payment:{paymentId}",
            It.Is<PaymentResponseDto>(dto => dto.Id == paymentId && dto.Amount == 5000),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetPaymentAdminAsync_WhenPaymentNotFound_ThrowsNotFoundException()
    {
        var paymentId = Guid.NewGuid();

        _cacheMock.Setup(c => c.GetDataAsync<PaymentResponseDto>($"payment:{paymentId}"))
            .ReturnsAsync((PaymentResponseDto?)null);

        _helper.Setup(h => h.GetPaymentOr404(paymentId))
            .ThrowsAsync(new NotFoundException("Payment not found"));

        Func<Task> action = async () => await _paymentService.GetPaymentAdminAsync(paymentId);

        await action.Should().ThrowAsync<NotFoundException>();

        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<PaymentResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }
}