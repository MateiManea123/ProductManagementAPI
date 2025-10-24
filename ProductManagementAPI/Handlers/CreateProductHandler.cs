using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace ProductModule
{
    public sealed class CreateProductHandler
    {
        private readonly IProductRepository _repo;
        private readonly ICacheService _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateProductHandler> _logger;

        public CreateProductHandler(IProductRepository repo, ICacheService cache, IMapper mapper, ILogger<CreateProductHandler> logger)
        {
            _repo = repo;
            _cache = cache;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProductProfileDto> HandleAsync(CreateProductProfileRequest request, CancellationToken ct = default)
        {
            var opId = GenerateOperationId();
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["OperationId"] = opId,
                ["SKU"] = request.SKU,
                ["Category"] = request.Category.ToString()
            });

            var totalSw = Stopwatch.StartNew();

            _logger.LogInformation(new EventId(ProductLogEvents.ProductCreationStarted, nameof(ProductLogEvents.ProductCreationStarted)),
                "Starting product creation. Name={Name}, Brand={Brand}, SKU={SKU}, Category={Category}",
                request.Name, request.Brand, request.SKU, request.Category);

            TimeSpan validationDuration = TimeSpan.Zero;
            TimeSpan dbDuration = TimeSpan.Zero;

            try
            {
                // Validation phase timing
                var validationSw = Stopwatch.StartNew();

                _logger.LogInformation(new EventId(ProductLogEvents.SKUValidationPerformed, nameof(ProductLogEvents.SKUValidationPerformed)),
                    "Validating SKU uniqueness. SKU={SKU}", request.SKU);

                if (string.IsNullOrWhiteSpace(request.SKU))
                {
                    throw new ArgumentException("SKU is required", nameof(request.SKU));
                }

                if (await _repo.SkuExistsAsync(request.SKU, ct))
                {
                    _logger.LogWarning(new EventId(ProductLogEvents.ProductValidationFailed, nameof(ProductLogEvents.ProductValidationFailed)),
                        "SKU already exists. SKU={SKU}", request.SKU);
                    throw new InvalidOperationException($"Product with SKU '{request.SKU}' already exists.");
                }

                _logger.LogInformation(new EventId(ProductLogEvents.StockValidationPerformed, nameof(ProductLogEvents.StockValidationPerformed)),
                    "Validating stock. StockQuantity={Stock}", request.StockQuantity);

                if (request.StockQuantity < 0)
                {
                    _logger.LogWarning(new EventId(ProductLogEvents.ProductValidationFailed, nameof(ProductLogEvents.ProductValidationFailed)),
                        "Invalid stock quantity. StockQuantity={Stock}", request.StockQuantity);
                    throw new ArgumentOutOfRangeException(nameof(request.StockQuantity), "Stock quantity cannot be negative.");
                }

                validationSw.Stop();
                validationDuration = validationSw.Elapsed;

                // Map to Product using advanced mapping
                var product = _mapper.Map<Product>(request);

                // DB operations timing
                var dbSw = Stopwatch.StartNew();
                _logger.LogInformation(new EventId(ProductLogEvents.DatabaseOperationStarted, nameof(ProductLogEvents.DatabaseOperationStarted)),
                    "Adding product to database. SKU={SKU}", product.SKU);

                await _repo.AddAsync(product, ct);
                await _repo.SaveChangesAsync(ct);

                dbSw.Stop();
                dbDuration = dbSw.Elapsed;

                _logger.LogInformation(new EventId(ProductLogEvents.DatabaseOperationCompleted, nameof(ProductLogEvents.DatabaseOperationCompleted)),
                    "Database operation completed. ProductId={ProductId}", product.Id);

                // Cache invalidation
                await _cache.RemoveAsync("all_products", ct);
                _logger.LogInformation(new EventId(ProductLogEvents.CacheOperationPerformed, nameof(ProductLogEvents.CacheOperationPerformed)),
                    "Cache invalidated. Key={Key}", "all_products");

                // Map to DTO with conditional mapping/resolvers
                var dto = _mapper.Map<ProductProfileDto>(product);

                totalSw.Stop();
                var metrics = new ProductCreationMetrics(
                    OperationId: opId,
                    ProductName: product.Name,
                    SKU: product.SKU,
                    Category: product.Category,
                    ValidationDuration: validationDuration,
                    DatabaseSaveDuration: dbDuration,
                    TotalDuration: totalSw.Elapsed,
                    Success: true,
                    ErrorReason: null
                );

                _logger.LogProductCreationMetrics(metrics);

                return dto;
            }
            catch (Exception ex)
            {
                totalSw.Stop();
                var metrics = new ProductCreationMetrics(
                    OperationId: opId,
                    ProductName: request.Name,
                    SKU: request.SKU,
                    Category: request.Category,
                    ValidationDuration: validationDuration,
                    DatabaseSaveDuration: dbDuration,
                    TotalDuration: totalSw.Elapsed,
                    Success: false,
                    ErrorReason: ex.Message
                );

                _logger.LogError(new EventId(ProductLogEvents.ProductValidationFailed, nameof(ProductLogEvents.ProductValidationFailed)), ex,
                    "Product creation failed. Name={Name}, SKU={SKU}, Category={Category}, Reason={Reason}",
                    request.Name, request.SKU, request.Category, ex.Message);

                _logger.LogProductCreationMetrics(metrics);
                throw;
            }
        }

        private static string GenerateOperationId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            var sb = new StringBuilder(8);
            foreach (var b in bytes)
            {
                sb.Append(chars[b % chars.Length]);
            }
            return sb.ToString();
        }
    }
}
using System;

namespace ProductModule
{
    public enum ProductCategory
    {
        Electronics = 0,
        Clothing = 1,
        Books = 2,
        Home = 3
    }
}

