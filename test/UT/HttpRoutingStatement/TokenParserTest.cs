using VKProxy.HttpRoutingStatement;

namespace UT.HttpRoutingStatement;

public class TokenParserTest
{
    [Fact]
    public void TestParseToken()
    {
        TestToken("", tokens =>
        {
            Assert.Empty(tokens);
        });
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData(" 2 ", "2")]
    [InlineData(" -3 ", "-3")]
    [InlineData("-4", "-4")]
    [InlineData("-5.6", "-5.6")]
    [InlineData("0.789", "0.789")]
    [InlineData("\r\t\n\r\t\n0.789   \r\t\n\r\t\n", "0.789")]
    public void ShouldParseNumber(string test, string expected)
    {
        TestToken(test, tokens =>
        {
            Assert.Single(tokens);
            var t = tokens[0];
            Assert.Equal(TokenType.Number, t.Type);
            Assert.Equal(expected, t.GetValue());
        });
    }

    [Theory]
    [InlineData(" 2.3.4 ", "Can't parse near by 2.3.4  (Line:0,Col:1)")]
    public void ShouldNotParseNumber(string test, string expected)
    {
        var ex = Assert.Throws<ParserExecption>(() =>
        {
            TestToken(test, tokens =>
            {
            });
        });
        Assert.Equal(expected, ex.Message);
    }

    [Theory]
    [InlineData("k", "k")]
    [InlineData(" k1 ", "k1")]
    public void ShouldParseWord(string test, string expected)
    {
        TestToken(test, tokens =>
        {
            Assert.Single(tokens);
            var t = tokens[0];
            Assert.Equal(TokenType.Word, t.Type);
            Assert.Equal(expected, t.GetValue());
        });
    }

    [Theory]
    [InlineData(" k$1 ", "k,$,1")]
    [InlineData("-1-", "-1,-")]
    [InlineData("-1<=3", "-1,<=,3")]
    [InlineData("-1 <= 3", "-1,<=,3")]
    [InlineData("-1 < = 3", "-1,<,=,3")]
    [InlineData("-1 =< 3", "-1,=,<,3")]
    [InlineData("-1 => 3", "-1,=,>,3")]
    [InlineData("(-1 = 3) and (5 = 4)", "(,-1,=,3,),and,(,5,=,4,)")]
    [InlineData(" '\r\t's\\'  gdfdg'     ", "\r\t,s\\,  gdfdg")]
    public void ShouldParseSign(string test, string expected)
    {
        TestToken(test, tokens =>
        {
            var s = expected.Split(",");
            Assert.Equal(s, tokens.Select(i => i.GetValue().ToString()));
            Assert.Equal(s.Length, tokens.Count);
            for (var i = 0; i < s.Length; i++)
            {
                Assert.Equal(s[i], tokens[i].GetValue());
            }
        });
    }

    [Theory]
    [InlineData("'s'", "s")]
    [InlineData(" '\r\t\\'s\\'  gdfdg'     ", "\r\t\'s\'  gdfdg")]
    [InlineData(" \"\r\t's'  gdfdg\\\"    \\\" ", "\r\t's'  gdfdg\"    \"")]
    public void ShouldParseString(string test, string expected)
    {
        TestToken(test, tokens =>
        {
            Assert.Single(tokens);
            var t = tokens[0];
            Assert.Equal(TokenType.String, t.Type);
            Assert.Equal(expected, t.GetValue());
        });
    }

    private void TestToken(string v, Action<List<Token>> action)
    {
        var tokens = HttpRoutingStatementParser.Tokenize(v);
        action(tokens.ToList());
    }
}