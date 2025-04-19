using Microsoft.AspNetCore.Http;

namespace VKProxy.Middlewares.Http.Transforms;

internal sealed class PathTransformFactory : ITransformFactory
{
    internal const string PathSetKey = "PathSet";
    internal const string PathPrefixKey = "PathPrefix";
    internal const string PathRemovePrefixKey = "PathRemovePrefix";
    //internal const string PathPatternKey = "PathPattern";

    //private readonly TemplateBinderFactory _binderFactory;

    //public PathTransformFactory(TemplateBinderFactory binderFactory)
    //{
    //    _binderFactory = binderFactory ?? throw new ArgumentNullException(nameof(binderFactory));
    //}

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue(PathSetKey, out var pathSet))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            var path = MakePathString(pathSet);
            AddPathSet(context, path);
        }
        else if (transformValues.TryGetValue(PathPrefixKey, out var pathPrefix))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            var path = MakePathString(pathPrefix);
            AddPathPrefix(context, path);
        }
        else if (transformValues.TryGetValue(PathRemovePrefixKey, out var pathRemovePrefix))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            var path = MakePathString(pathRemovePrefix);
            AddPathRemovePrefix(context, path);
        }
        //else if (transformValues.TryGetValue(PathPatternKey, out var pathPattern))
        //{
        //    TransformHelpers.CheckTooManyParameters(transformValues, expected: 1);
        //    var path = MakePathString(pathPattern);
        //    // We don't use the extension here because we want to avoid doing a DI lookup for the binder every time.
        //    context.RequestTransforms.Add(new PathRouteValuesTransform(path.Value!, _binderFactory));
        //}
        else
        {
            return false;
        }

        return true;
    }

    private static PathString MakePathString(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }
        return new PathString(path);
    }

    public static TransformBuilderContext AddPathSet(TransformBuilderContext context, PathString path)
    {
        context.RequestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.Set, path));
        return context;
    }

    public static TransformBuilderContext AddPathPrefix(TransformBuilderContext context, PathString prefix)
    {
        context.RequestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.Prefix, prefix));
        return context;
    }

    public static TransformBuilderContext AddPathRemovePrefix(TransformBuilderContext context, PathString prefix)
    {
        context.RequestTransforms.Add(new PathStringTransform(PathStringTransform.PathTransformMode.RemovePrefix, prefix));
        return context;
    }
}