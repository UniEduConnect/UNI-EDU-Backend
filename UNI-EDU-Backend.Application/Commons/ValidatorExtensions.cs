using FluentValidation;
using ValidationException = UNI_EDU_Backend.Application.Exceptions.ValidationException;

namespace UNI_EDU_Backend.Application.Commons;

public static class ValidatorExtensions
{
    public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid) return;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage,
                (propertyName, messages) => new { Key = propertyName, Value = messages.Distinct().ToArray() })
            .ToDictionary(x => x.Key, x => x.Value);

        throw new ValidationException(errors);
    }
}
