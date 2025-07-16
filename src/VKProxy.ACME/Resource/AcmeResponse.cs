namespace VKProxy.ACME.Resource;

public class AcmeResponse<T>
{
    public AcmeResponse(Uri? location, T? resource, ILookup<string, Uri> links, AcmeError? error, int retryafter)
    {
        Location = location;
        Resource = resource;
        Links = links;
        Error = error;
        Retryafter = retryafter;
    }

    public Uri? Location { get; }
    public T? Resource { get; }
    public ILookup<string, Uri> Links { get; }
    public AcmeError? Error { get; }
    public int Retryafter { get; }
}