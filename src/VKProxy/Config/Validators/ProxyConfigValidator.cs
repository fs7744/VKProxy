namespace VKProxy.Config.Validators;

public class ProxyConfigValidator : IValidator<IProxyConfig>
{
    private readonly IEnumerable<IValidator<ListenConfig>> listenConfigValidators;
    private readonly IEnumerable<IValidator<SniConfig>> sniConfigValidators;

    public ProxyConfigValidator(IEnumerable<IValidator<ListenConfig>> listenConfigValidators,
        IEnumerable<IValidator<SniConfig>> sniConfigValidators)
    {
        this.listenConfigValidators = Enumerable.Reverse(listenConfigValidators).ToArray();
        this.sniConfigValidators = Enumerable.Reverse(sniConfigValidators).ToArray();
    }

    public async Task<bool> ValidateAsync(IProxyConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        var r = true;

        if (value != null)
        {
            if (value.Listen != null)
            {
                foreach (var l in value.Listen)
                {
                    var ll = l.Value;
                    foreach (var v in listenConfigValidators)
                    {
                        if (!(await v.ValidateAsync(ll, exceptions, cancellationToken)))
                        {
                            r = false;
                        }
                    }
                    if (ll.ListenEndPointOptions == null || ll.ListenEndPointOptions.Count == 0)
                    {
                        value.RemoveListen(l.Key);
                    }
                }
            }

            if (value.Sni != null)
            {
                foreach (var l in value.Sni)
                {
                    var ll = l.Value;
                    foreach (var v in sniConfigValidators)
                    {
                        if (!(await v.ValidateAsync(ll, exceptions, cancellationToken)))
                        {
                            r = false;
                        }
                    }
                    if (ll.Certificate == null)
                    {
                        value.RemoveListen(l.Key);
                    }
                }
            }
        }
        return r;
    }
}