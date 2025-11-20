using System;
using Microsoft.Extensions.Logging;

namespace Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(this ILogger logger, ProductCreationMetrics metrics)
    {
        var eventId = new EventId(LogEvents.ProductCreationCompleted, nameof(LogEvents.ProductCreationCompleted));

        logger.LogInformation(
            eventId,
            "Product creation metrics for {ProductName} (SKU: {SKU}, Category: {Category}) | " +
            "Validation: {ValidationMs} ms | DB: {DbMs} ms | Total: {TotalMs} ms | Success: {Success} | Error: {Error}",
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? "None");
    }
}