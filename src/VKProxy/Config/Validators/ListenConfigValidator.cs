using System.Net;

namespace VKProxy.Config.Validators;

public class ListenConfigValidator : IValidator<ListenConfig>
{
    private readonly IEndPointConvertor[] endPointConvertors;

    public ListenConfigValidator(IEnumerable<IEndPointConvertor> endPointConvertors)
    {
        this.endPointConvertors = Enumerable.Reverse(endPointConvertors).ToArray();
    }

    public async Task<bool> ValidateAsync(ListenConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        var r = true;

        if (value != null)
        {
            if (value.Address == null || value.Address.Length == 0)
            {
                exceptions.Add(new ArgumentException($"Listen ({value.Key}) Address can not be empty."));
                r = false;
            }
            else if (value.SniId == null && value.Protocols.HasFlag(GatewayProtocols.HTTP3))
            {
                exceptions.Add(new ArgumentException($"Listen ({value.Key}) use HTTP3 but no sniId. (Quic no support sni)"));
                r = false;
            }
            else
            {
                IEnumerable<EndPoint> all = Enumerable.Empty<EndPoint>();
                foreach (var address in value.Address.Where(i => !string.IsNullOrWhiteSpace(i)))
                {
                    IEnumerable<EndPoint> endpoints = Enumerable.Empty<EndPoint>();
                    foreach (var item in endPointConvertors)
                    {
                        if (item.TryConvert(address, value.Protocols, out var endPoints))
                        {
                            endpoints = endpoints.Union(endPoints);
                            break;
                        }
                    }
                    if (endpoints is EndPoint[] s && s.Length == 0)
                    {
                        exceptions.Add(new ArgumentException($"Listen ({value.Key}) Address '{address}' can not convert to EndPoint."));
                        r = false;
                    }
                    else
                    {
                        all = all.Union(endpoints);
                    }
                }

                value.ListenEndPointOptions = all.Select(i => new ListenEndPointOptions()
                {
                    EndPoint = i,
                    Protocols = value.Protocols,
                    Key = value.Key,
                    Parent = value
                }).ToList();
            }
        }

        return r;
    }
}