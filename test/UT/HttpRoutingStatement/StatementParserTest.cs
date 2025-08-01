using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Text;
using VKProxy.HttpRoutingStatement;
using VKProxy.HttpRoutingStatement.Statements;

namespace UT.HttpRoutingStatement;

public class StatementParserTest
{
    private void TestStatement(string v, Action<Stack<Statement>> action)
    {
        var statements = HttpRoutingStatementParser.ParseStatements(v);
        action(statements);
    }

    [Theory]
    [InlineData("1", "1", typeof(NumberValueStatement))]
    [InlineData("1.3", "1.3", typeof(NumberValueStatement))]
    [InlineData("-77.3", "-77.3", typeof(NumberValueStatement))]
    [InlineData("'sdd sd'", "sdd sd", typeof(StringValueStatement))]
    [InlineData("true", "tRue", typeof(BooleanValueStatement))]
    [InlineData("false", "false", typeof(BooleanValueStatement))]
    [InlineData("xx", "xx", typeof(FieldStatement))]
    [InlineData("v in (1)", " v in (1) ", typeof(InOperaterStatement))]
    [InlineData("v in (1,2,3,4)", " v in (1,2,3,4) ", typeof(InOperaterStatement))]
    [InlineData("v in ('1',null)", " v in ('1',null) ", typeof(InOperaterStatement))]
    [InlineData("v in ('1\\'',  '2' ,'3', '4')", " v in ('1\\'','2','3','4') ", typeof(InOperaterStatement))]
    [InlineData("v in (true,false)", " v in (true,false) ", typeof(InOperaterStatement))]
    [InlineData("xx = true", " xx = true ", typeof(OperaterStatement))]
    [InlineData("xx <= 3", " xx <= 3 ", typeof(OperaterStatement))]
    [InlineData("xx >= 3", " xx >= 3 ", typeof(OperaterStatement))]
    [InlineData("xx > 3", " xx > 3 ", typeof(OperaterStatement))]
    [InlineData("xx != 3", " xx != 3 ", typeof(OperaterStatement))]
    [InlineData("xx != 'sdsd != s'", " xx != 'sdsd != s' ", typeof(OperaterStatement))]
    [InlineData("yy = NULL ", " yy = null ", typeof(OperaterStatement))]
    [InlineData("1 = 1", " 1 = 1 ", typeof(OperaterStatement))]
    [InlineData("1 = 1 and 2 != 3 or 11 >= 13.1 or 23 <= 31", " ( ( ( 1 = 1  and  2 != 3 )  or  11 >= 13.1 )  or  23 <= 31 ) ", typeof(ConditionsStatement))]
    [InlineData("11 >= 13.1 or 23 <= 31 ", " ( 11 >= 13.1  or  23 <= 31 ) ", typeof(ConditionsStatement))]
    [InlineData("((11 >= 13.1) or (23 <= 31 ))", " ( 11 >= 13.1  or  23 <= 31 ) ", typeof(ConditionsStatement))]
    [InlineData("((11 >= 13.1 and 1 != 2) or (23 <= 31  or x != y ))", " ( ( 11 >= 13.1  and  1 != 2 )  or  ( 23 <= 31  or  x != y ) ) ", typeof(ConditionsStatement))]
    [InlineData("(11 >= 13.1)", " 11 >= 13.1 ", typeof(OperaterStatement))]
    [InlineData("not(11 >= 13.1)", " not ( 11 >= 13.1 ) ", typeof(UnaryOperaterStatement))]
    [InlineData("(not(11 >= 13.1 and 1 != 2) or (23 <= 31  or x != y ))", " ( not ( ( 11 >= 13.1  and  1 != 2 ) )  or  ( 23 <= 31  or  x != y ) ) ", typeof(ConditionsStatement))]
    [InlineData("Query('xx')", "Query:xx", typeof(DynamicFieldStatement))]
    [InlineData("Cookie('x1x')", "Cookie:x1x", typeof(DynamicFieldStatement))]
    [InlineData("Header(\"x2x\")", "Header:x2x", typeof(DynamicFieldStatement))]
    public void ShouldParse(string test, string expected, Type type)
    {
        TestStatement(test, statements =>
        {
            Assert.Single(statements);
            var t = statements.Pop();
            Assert.Equal(type, t.GetType());
            if (t is NumberValueStatement nv)
            {
                Assert.Equal(expected, nv.Value.ToString());
            }
            else if (t is StringValueStatement s)
            {
                Assert.Equal(expected, s.Value);
            }
            else if (t is BooleanValueStatement b)
            {
                Assert.Equal(bool.Parse(expected), b.Value);
            }
            else if (t is DynamicFieldStatement d)
            {
                Assert.Equal(expected, $"{d.Field}:{d.Key}");
            }
            else if (t is FieldStatement f)
            {
                Assert.Equal(expected, f.Field);
            }
            else if (t is OperaterStatement op)
            {
                Assert.Equal(expected, op.ConvertToString());
            }
            else if (t is InOperaterStatement na)
            {
                Assert.Equal(expected, na.ConvertToString());
            }
            else if (t is ConditionsStatement cs)
            {
                Assert.Equal(expected, cs.ConvertToString());
            }
            else if (t is UnaryOperaterStatement us)
            {
                Assert.Equal(expected, us.ConvertToString());
            }
            else
            {
                Assert.Fail("Not Found Statement");
            }
        });
    }

    [Theory]
    [InlineData("Path = '/testp'", true)]
    [InlineData("Path = '/testp3'", false)]
    [InlineData("IsHttps = false", true)]
    [InlineData("IsHttps = true", false)]
    [InlineData("ContentLength = 3", false)]
    [InlineData("3 = ContentLength", false)]
    [InlineData("3 > ContentLength", false)]
    [InlineData("3 >= ContentLength", false)]
    [InlineData("13 < ContentLength", false)]
    [InlineData("13 <= ContentLength", false)]
    [InlineData("header('x-c') = 'test'", false)]
    [InlineData("Cookie('x-c') = 'test'", false)]
    [InlineData("Query('x-c') = 'test'", false)]
    [InlineData("header('x-c') = 'a'", true)]
    [InlineData("Cookie('x-c') = 'ddd'", true)]
    [InlineData("Query('x-c') = 'xxx'", true)]
    [InlineData("header('#keys') = 'x-c'", true)]
    [InlineData("header('#keys') = 'a'", false)]
    [InlineData("header('#values') = 'a'", true)]
    [InlineData("header('#kvs') = 'x-c'", true)]
    [InlineData("header('#kvs') = 'a'", true)]
    [InlineData("header('x-c') != 'test'", true)]
    [InlineData("Cookie('x-c') != 'test'", true)]
    [InlineData("Query('x-c') != 'test'", true)]
    [InlineData("header('x-c') != 'a'", false)]
    [InlineData("Cookie('x-c') != 'ddd'", false)]
    [InlineData("Query('x-c') != 'xxx'", false)]
    [InlineData("header('x-c1') = 'a'", false)]
    [InlineData("Cookie('x-c1') = 'ddd'", false)]
    [InlineData("Query('x-c1') = 'xxx'", false)]
    [InlineData("Path ~= '[/]test.*'", true)]
    [InlineData("Path ~= '[/]test.*' and 12 > ContentLength", true)]
    [InlineData("Path ~= '[/]atest.*' and 12 > ContentLength", false)]
    [InlineData("Path ~= '[/]atest.*' or 12 > ContentLength", true)]
    [InlineData("(Path ~= '[/]atest.*' and 12 > ContentLength) or (Path ~= '[/]atest.*' or 12 > ContentLength)", true)]
    [InlineData("(Path ~= '[/]atest.*' and 12 > ContentLength) and (Path ~= '[/]atest.*' or 12 > ContentLength)", false)]
    [InlineData("Path ~= '[/]test77.*'", false)]
    public void Equal(string test, bool expected)
    {
        var func = HttpRoutingStatementParser.ConvertToFunction(test);
        Assert.NotNull(func);

        var context = new Mock<HttpContext>();
        context.Setup(r => r.Request.Path).Returns("/testp");
        context.Setup(r => r.Request.ContentLength).Returns(9);
        var h = new HeaderDictionary();
        h["x-c"] = "a";
        context.Setup(r => r.Request.Headers).Returns(h);
        //context.Setup(r => r.Request.Headers.Keys).Returns(new string[] { "x-c" });
        //context.Setup(r => r.Request.Headers.Values).Returns(new StringValues[] { "a" });
        //context.Setup(r => r.Request.Headers["x-c"]).Returns("a");
        var c = new Mock<IRequestCookieCollection>();
        c.Setup(r => r.TryGetValue(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns((string k, out string v) =>
        {
            v = "ddd";
            return k == "x-c";
        });
        c.SetupGet(i => i.Count).Returns(1);
        context.Setup(r => r.Request.Cookies).Returns(c.Object);
        context.Setup(r => r.Request.Cookies["x-c"]).Returns("ddd");
        var q = new Mock<IQueryCollection>();
        q.Setup(r => r.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny)).Returns((string k, out StringValues v) =>
        {
            v = "xxx";
            return k == "x-c";
        });
        q.SetupGet(i => i.Count).Returns(1);
        context.Setup(r => r.Request.Query).Returns(q.Object);
        context.Setup(r => r.Request.Query["x-c"]).Returns("xxx");

        Assert.Equal(expected, func(context.Object));
    }
}