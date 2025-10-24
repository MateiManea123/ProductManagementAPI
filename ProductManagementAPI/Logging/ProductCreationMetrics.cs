using System;

namespace ProductModule
{
    public sealed record ProductCreationMetrics(
        string OperationId,
        string ProductName,
        string SKU,
        ProductCategory Category,
        TimeSpan ValidationDuration,
        TimeSpan DatabaseSaveDuration,
        TimeSpan TotalDuration,
        bool Success,
        string? ErrorReason
    );
}

