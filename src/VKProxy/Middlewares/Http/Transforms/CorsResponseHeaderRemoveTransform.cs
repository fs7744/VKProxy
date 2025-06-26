namespace VKProxy.Middlewares.Http.Transforms;

internal class CorsResponseHeaderRemoveTransform : ResponseTransform, ITransformProvider
{
    public void Apply(TransformBuilderContext context)
    {
        context.ResponseTransforms.Add(this);
    }

    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        var items = context.HttpContext.Items;
        var respH = context.HttpContext.Response.Headers;
        if (items.ContainsKey("AccessControlAllowOrigin"))
        {
            var d = respH.AccessControlAllowOrigin;
            if (d.Count > 1)
            {
                respH.AccessControlAllowOrigin = d.First();
            }

            d = respH.AccessControlAllowHeaders;
            if (d.Count > 1)
            {
                respH.AccessControlAllowHeaders = d.First();
            }

            d = respH.AccessControlAllowMethods;
            if (d.Count > 1)
            {
                respH.AccessControlAllowMethods = d.First();
            }

            d = respH.AccessControlAllowCredentials;
            if (d.Count > 1)
            {
                respH.AccessControlAllowCredentials = d.First();
            }

            d = respH.AccessControlMaxAge;
            if (d.Count > 1)
            {
                respH.AccessControlMaxAge = d.First();
            }

            d = respH.AccessControlExposeHeaders;
            if (d.Count > 1)
            {
                respH.AccessControlExposeHeaders = d.First();
            }

            d = respH.Vary;
            if (d.Count > 1)
            {
                respH.Vary = d.First();
            }
        }
        return default;
    }
}