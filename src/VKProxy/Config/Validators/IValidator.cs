namespace VKProxy.Config.Validators;

public interface IValidator<T>
{
    ValueTask<bool> ValidateAsync(T? value, List<Exception> exceptions, CancellationToken cancellationToken);
}