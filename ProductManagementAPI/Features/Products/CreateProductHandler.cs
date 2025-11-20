using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Common.Logging;
using Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Features.Products;

public class CreateProductHandler : IRequestHandler<CreateProductProfileRequest, ProductProfileDto>
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateProductHandler> _logger;

    private const string CacheKeyAllProducts = "all_products";

    public CreateProductHandler(
        ApplicationContext context,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<CreateProductHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductProfileDto> Handle(CreateProductProfileRequest request, CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var overallStopwatch = Stopwatch.StartNew();
        var validationStopwatch = new Stopwatch();
        var dbStopwatch = new Stopwatch();

        using var scope = _logger.BeginScope("ProductCreation {OperationId}", operationId);

        _logger.LogInformation(
            new EventId(LogEvents.ProductCreationStarted, nameof(LogEvents.ProductCreationStarted)),
            "Starting product creation. Name={Name}, Brand={Brand}, Category={Category}, SKU={SKU}",
            request.Name, request.Brand, request.Category, request.SKU);

        try
        {
            _logger.LogInformation(
                new EventId(LogEvents.SKUValidationPerformed, nameof(LogEvents.SKUValidationPerformed)),
                "Performing SKU validation for {SKU}", request.SKU);

            validationStopwatch.Start();

            var exists = await _context.Products
                .AnyAsync(p => p.SKU == request.SKU, cancellationToken);

            if (exists)
            {
                validationStopwatch.Stop();

                _logger.LogWarning(
                    new EventId(LogEvents.ProductValidationFailed, nameof(LogEvents.ProductValidationFailed)),
                    "SKU validation failed for {SKU}. Product already exists.", request.SKU);

                throw new ValidationException($"Product with SKU '{request.SKU}' already exists.");
            }

            _logger.LogInformation(
                new EventId(LogEvents.StockValidationPerformed, nameof(LogEvents.StockValidationPerformed)),
                "Stock validation performed for SKU {SKU} with quantity {Qty}",
                request.SKU, request.StockQuantity);

            validationStopwatch.Stop();

            dbStopwatch.Start();

            _logger.LogInformation(
                new EventId(LogEvents.DatabaseOperationStarted, nameof(LogEvents.DatabaseOperationStarted)),
                "Starting database operation for SKU {SKU}", request.SKU);

            var product = _mapper.Map<Product>(request);
            await _context.Products.AddAsync(product, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                new EventId(LogEvents.DatabaseOperationCompleted, nameof(LogEvents.DatabaseOperationCompleted)),
                "Database operation completed for ProductId {ProductId}", product.Id);

            dbStopwatch.Stop();

            _logger.LogInformation(
                new EventId(LogEvents.CacheOperationPerformed, nameof(LogEvents.CacheOperationPerformed)),
                "Invalidating cache key {CacheKey}", CacheKeyAllProducts);

            _cache.Remove(CacheKeyAllProducts);

            var dto = _mapper.Map<ProductProfileDto>(product);

            overallStopwatch.Stop();

            var metrics = new ProductCreationMetrics(
                OperationId: operationId,
                ProductName: product.Name,
                SKU: product.SKU,
                Category: product.Category,
                ValidationDuration: validationStopwatch.Elapsed,
                DatabaseSaveDuration: dbStopwatch.Elapsed,
                TotalDuration: overallStopwatch.Elapsed,
                Success: true,
                ErrorReason: null);

            _logger.LogProductCreationMetrics(metrics);

            _logger.LogInformation(
                new EventId(LogEvents.ProductCreationCompleted, nameof(LogEvents.ProductCreationCompleted)),
                "Product creation completed successfully for ProductId {ProductId}", product.Id);

            return dto;
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();

            var metrics = new ProductCreationMetrics(
                OperationId: operationId,
                ProductName: request.Name,
                SKU: request.SKU,
                Category: request.Category,
                ValidationDuration: validationStopwatch.Elapsed,
                DatabaseSaveDuration: dbStopwatch.Elapsed,
                TotalDuration: overallStopwatch.Elapsed,
                Success: false,
                ErrorReason: ex.Message);

            _logger.LogProductCreationMetrics(metrics);

            _logger.LogError(
                new EventId(LogEvents.ProductValidationFailed, nameof(LogEvents.ProductValidationFailed)),
                ex,
                "Error during product creation for SKU {SKU}", request.SKU);

            // Re-throw for global handler
            throw;
        }
    }
}

// Custom ValidationException simplă, dacă nu folosești FluentValidation aici:
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
