namespace VKProxy.Middlewares.Http.Transforms;

public class StructuredTransformer : IHttpTransformer
{
    public StructuredTransformer(bool? copyRequestHeaders, bool? copyResponseHeaders, bool? copyResponseTrailers, IList<RequestTransform> requestTransforms, IList<ResponseTransform> responseTransforms, IList<ResponseTrailersTransform> responseTrailersTransforms)
    {
    }
}