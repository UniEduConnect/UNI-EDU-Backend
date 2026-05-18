using FluentValidation;
using MediatR;
using ValidationException = UNI_EDU_Backend.Application.Exceptions.ValidationException;

namespace UNI_EDU_Backend.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : class, IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errorsDictionary = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .GroupBy(
                x => x.PropertyName,
                x => x.ErrorMessage,
                (propertyName, errorMessage) => new
                {
                    Key = propertyName,
                    Value = errorMessage.Distinct().ToArray()
                }
            ).ToDictionary(x => x.Key, x => x.Value);

        if (errorsDictionary.Count != 0)
            throw new ValidationException(errorsDictionary);

        return await next(cancellationToken);
    }
}
