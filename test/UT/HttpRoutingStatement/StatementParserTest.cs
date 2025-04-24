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
    [InlineData("v in ('1')", " v in ('1') ", typeof(InOperaterStatement))]
    [InlineData("v in ('1\\'',  '2' ,'3', '4')", " v in ('1\\'','2','3','4') ", typeof(InOperaterStatement))]
    [InlineData("v in (true,false)", " v in (true,false) ", typeof(InOperaterStatement))]
    [InlineData("xx = true", " xx = true ", typeof(OperaterStatement))]
    [InlineData("xx <= 3", " xx <= 3 ", typeof(OperaterStatement))]
    [InlineData("xx >= 3", " xx >= 3 ", typeof(OperaterStatement))]
    [InlineData("xx > 3", " xx > 3 ", typeof(OperaterStatement))]
    [InlineData("xx != 3", " xx != 3 ", typeof(OperaterStatement))]
    [InlineData("xx != 'sdsd != s'", " xx != 'sdsd != s' ", typeof(OperaterStatement))]
    [InlineData("yy = NULL ", " yy = NULL ", typeof(OperaterStatement))]
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
}