using Microsoft.AspNetCore.Http;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

public interface IFieldStatementFuncConverter
{
    string Field { get; }

    Func<HttpContext, bool> Convert(ValueStatement value, string operater);
}
