using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; 

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;


public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ISellerProfileRepository> _sellerProfileRepositoryMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly Mock<IFileStorageService> _fileServiceMock;
    private readonly Mock<IHelperService> _helper;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _sellerProfileRepositoryMock = new Mock<ISellerProfileRepository>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _fileServiceMock = new Mock<IFileStorageService>();
        _helper = new Mock<IHelperService>();

        _productService = new ProductService(
            _productRepositoryMock.Object,
            _sellerProfileRepositoryMock.Object,
            _notificationRepositoryMock.Object,
            _fileServiceMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }
}