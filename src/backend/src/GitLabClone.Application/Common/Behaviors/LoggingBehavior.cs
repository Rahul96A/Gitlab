using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Application.Common.Behaviors;

/// <summary>
/// Logs every MediatR request with duration. Warns if a request takes > 500ms.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
        {
            logger.LogWarning("Long-running request: {RequestName} ({ElapsedMs}ms)", requestName, sw.ElapsedMilliseconds);
        }
        else
        {
            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
