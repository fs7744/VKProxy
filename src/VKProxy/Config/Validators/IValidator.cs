namespace VKProxy.Config.Validators;

public interface IValidator<T>
{
    Task<bool> ValidateAsync(T? value, List<Exception> exceptions, CancellationToken cancellationToken);
}