using Microsoft.Extensions.Logging;

namespace ProductModule
{
    public static class LoggingExtensions
    {
        public static void LogProductCreationMetrics(this ILogger logger, ProductCreationMetrics m)
        {
            logger.Log(
                logLevel: LogLevel.Information,
                eventId: new EventId(ProductLogEvents.ProductCreationCompleted, nameof(ProductLogEvents.ProductCreationCompleted)),
                exception: null,
                message: "[Products] OpId={OperationId} Name={ProductName} SKU={SKU} Category={Category} ValidationMs={ValidationMs} DbMs={DbMs} TotalMs={TotalMs} Success={Success} Error={Error}",
                args: new object?[]
                {
                    m.OperationId,
                    m.ProductName,
                    m.SKU,
                    m.Category,
                    (int)m.ValidationDuration.TotalMilliseconds,
                    (int)m.DatabaseSaveDuration.TotalMilliseconds,
                    (int)m.TotalDuration.TotalMilliseconds,
                    m.Success,
                    m.ErrorReason ?? string.Empty
                }
            );
        }
    }
}

