using Microsoft.AspNetCore.Http;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

public interface IFieldStatementFuncConverter
{
    string Field { get; }
}

public interface IStaticFieldStatementFuncConverter : IFieldStatementFuncConverter
{
    Func<HttpContext, bool> Convert(ValueStatement value, string operater);
}

public interface IDynamicFieldStatementFuncConverter : IFieldStatementFuncConverter
{
    Func<HttpContext, bool> Convert(ValueStatement value, string operater, string key);
}