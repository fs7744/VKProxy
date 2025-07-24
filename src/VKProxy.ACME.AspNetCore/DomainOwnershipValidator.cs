using Microsoft.Extensions.Logging;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME.AspNetCore;

public abstract class DomainOwnershipValidator
{
    public abstract Task ValidateOwnershipAsync(string domainName, AcmeStateContext context, IAuthorizationContext authzContext, CancellationToken cancellationToken);

    protected async Task WaitForChallengeResultAsync(string domainName, AcmeStateContext context, IAuthorizationContext authorizationContext, CancellationToken cancellationToken)
    {
        var retries = 60;
        var delay = TimeSpan.FromSeconds(2);

        while (retries > 0)
        {
            retries--;

            cancellationToken.ThrowIfCancellationRequested();

            var authorization = await authorizationContext.GetResourceAsync(cancellationToken);

            context.Logger.LogDebug("GetAuthorization {domainName}", domainName);

            switch (authorization.Status)
            {
                case AuthorizationStatus.Valid:
                    return;

                case AuthorizationStatus.Pending:
                    await Task.Delay(delay, cancellationToken);
                    continue;
                case AuthorizationStatus.Invalid:
                    throw InvalidAuthorizationError(authorization, context);
                case AuthorizationStatus.Revoked:
                    throw new AcmeException(
                        $"The authorization to verify domainName '{domainName}' has been revoked.");
                case AuthorizationStatus.Expired:
                    throw new AcmeException(
                        $"The authorization to verify domainName '{domainName}' has expired.");
                case AuthorizationStatus.Deactivated:
                default:
                    throw new AcmeException("Unexpected response from server while validating domain ownership.");
            }
        }

        throw new TimeoutException("Timed out waiting for domain ownership validation.");
    }

    private Exception InvalidAuthorizationError(Authorization authorization, AcmeStateContext context)
    {
        var reason = "unknown";
        var domainName = authorization.Identifier.Value;
        try
        {
            var errors = authorization.Challenges.Where(a => a.Error != null).Select(a => a.Error)
                .Select(error => $"{error.Type}: {error.Detail}, Code = {error.Status}");
            reason = string.Join("; ", errors);
        }
        catch
        {
            context.Logger.LogTrace("Could not determine reason why validation failed. Response: {resp}", authorization);
        }

        context.Logger.LogError("Failed to validate ownership of domainName '{domainName}'. Reason: {reason}", domainName,
            reason);

        return new AcmeException($"Failed to validate ownership of domainName '{domainName}'");
    }
}
