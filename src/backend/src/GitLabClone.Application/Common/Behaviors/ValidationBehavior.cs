using FluentValidation;
using MediatR;

namespace GitLabClone.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all FluentValidation validators
/// for a request before the handler executes. If any validation fails,
/// a ValidationException is thrown — never reaches the handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new Exceptions.ValidationException(failures);

        return await next(cancellationToken);
    }
}
