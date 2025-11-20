using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Common.Logging;
using Common.Mapping;
using Features.Products;
using Features.Products.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CreateProductHandler>> _loggerMock;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: $"ProductsDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationContext(options);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdvancedProductMappingProfile>();
        });

        _mapper = mapperConfig.CreateMapper();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CreateProductHandler>>();

        _handler = new CreateProductHandler(_context, _mapper, _cache, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        var request = new CreateProductProfileRequest
        {
            Name = "Smart 4K TV",
            Brand = "Super Brand",
            SKU = "TV-12345",
            Category = ProductCategory.Electronics,
            Price = 1000m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-6),
            ImageUrl = "https://example.com/tv.jpg",
            StockQuantity = 10
        };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Electronics & Technology", result.CategoryDisplayName);
        Assert.Equal("SB", result.BrandInitials);
        Assert.False(string.IsNullOrWhiteSpace(result.ProductAge));
        Assert.StartsWith(
            System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol,
            result.FormattedPrice);
        Assert.Equal("In Stock", result.AvailabilityStatus);

        _loggerMock.VerifyLog(LogLevel.Information, LogEvents.ProductCreationStarted, Times.Once());
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        var existing = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Product",
            Brand = "Brand",
            SKU = "DUP-12345",
            Category = ProductCategory.Electronics,
            Price = 100m,
            ReleaseDate = DateTime.UtcNow.AddYears(-1),
            CreatedAt = DateTime.UtcNow,
            IsAvailable = true,
            StockQuantity = 5
        };
        _context.Products.Add(existing);
        await _context.SaveChangesAsync();

        var request = new CreateProductProfileRequest
        {
            Name = "New Product",
            Brand = "Brand",
            SKU = "DUP-12345",
            Category = ProductCategory.Electronics,
            Price = 200m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-3),
            StockQuantity = 5
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(request, CancellationToken.None));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        _loggerMock.VerifyLog(LogLevel.Warning, LogEvents.ProductValidationFailed, Times.AtLeastOnce());
    }

    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        var request = new CreateProductProfileRequest
        {
            Name = "Nice Home Lamp",
            Brand = "Home Brand",
            SKU = "HOME-999",
            Category = ProductCategory.Home,
            Price = 100m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-2),
            ImageUrl = "https://example.com/lamp.jpg",
            StockQuantity = 3
        };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.Equal("Home & Garden", result.CategoryDisplayName);
        Assert.Equal(90m, result.Price);
        Assert.Null(result.ImageUrl);

        _loggerMock.VerifyLog(LogLevel.Information, LogEvents.ProductCreationCompleted, Times.Once());
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }
}

public static class LoggerMoqExtensions
{
    public static void VerifyLog(
        this Mock<ILogger<CreateProductHandler>> logger,
        LogLevel level,
        int eventId,
        Times times)
    {
        logger.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == level),
                It.Is<EventId>(e => e.Id == eventId),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()!),
            times);
    }
}
