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
}