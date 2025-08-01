using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Text.RegularExpressions;
using VKProxy.HttpRoutingStatement;

[MemoryDiagnoser, Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest), GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
public class Path_HttpRoutingStatementParserBenchmarks
{
    private DefaultHttpContext HttpContext;
    private Func<HttpContext, bool> _PathEqual;
    private Func<HttpContext, bool> _PathEqualV2;
    private Func<HttpContext, bool> _PathEqualTrue;
    private Func<HttpContext, bool> _PathEqualTrueV2;
    private readonly Func<HttpContext, bool> _PathIn;
    private readonly Func<HttpContext, bool> _PathInV2;
    private readonly HashSet<string> set;
    private readonly Regex queryRegx;
    private readonly Func<HttpContext, bool> _PathComplex;
    private readonly Func<HttpContext, bool> _PathComplexV2;
    private readonly Func<HttpContext, bool> _IsHttps;
    private readonly Func<HttpContext, bool> _IsHttpsV2;
    private Regex regx;
    private Func<HttpContext, bool> _PathRegx;
    private Func<HttpContext, bool> _PathRegxV2;

    public Path_HttpRoutingStatementParserBenchmarks()
    {
        this.HttpContext = new DefaultHttpContext();
        var req = HttpContext.Request;
        req.Path = "/testp/dsd/fsdfx/fadasd3/中";
        req.Method = "GET";
        req.Host = new HostString("x.com");
        req.Scheme = "https";
        req.Protocol = "HTTP/1.1";
        req.ContentType = "json";
        req.QueryString = new QueryString("?s=123&d=456&f=789");
        req.IsHttps = true;

        queryRegx = new Regex(@"s[=].*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        regx = new Regex(@"^[/]testp.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        _PathRegx = StatementParser.ConvertToFunc("Path ~= '^[/]testp.*'");
        _PathRegxV2 = HttpRoutingStatementParser.ConvertToFunction("Path ~= '^[/]testp.*'");
        _PathEqual = StatementParser.ConvertToFunc("Path = '/testp'");
        _PathEqualV2 = HttpRoutingStatementParser.ConvertToFunction("Path = '/testp'");
        _PathEqualTrue = StatementParser.ConvertToFunc("Path = '/testp/DSD/fsdfx/fadasd3/中'");
        _PathEqualTrueV2 = HttpRoutingStatementParser.ConvertToFunction("Path = '/testp/DSD/fsdfx/fadasd3/中'");

        _PathIn = StatementParser.ConvertToFunc("Path in ('/testp','/testp/DSD/fsdfx/fadasd3/中')");
        _PathInV2 = HttpRoutingStatementParser.ConvertToFunction("Path in ('/testp','/testp/DSD/fsdfx/fadasd3/中')");
        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/testp", "/testp/DSD/fsdfx/fadasd3/中" };

        var w = "IsHttps = true and Path = '/testp/DSD/fsdfx/fadasd3/中' AND Method = \"GET\" AND Host = \"x.com\" AND Scheme = \"https\" AND Protocol = \"HTTP/1.1\" AND ContentType = \"json\" AND QueryString ~= 's[=].*' and not(Scheme = \"http\")";
        _PathComplex = StatementParser.ConvertToFunc(w);
        _PathComplexV2 = HttpRoutingStatementParser.ConvertToFunction(w);

        w = "IsHttps = true";
        _IsHttps = StatementParser.ConvertToFunc(w);
        _IsHttpsV2 = HttpRoutingStatementParser.ConvertToFunction(w);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("PathEqual")]
    public void PathEqualString()
    {
        var b = string.Equals(HttpContext.Request.Path.Value, "/testp", StringComparison.OrdinalIgnoreCase);
        var c = string.Equals(HttpContext.Request.Path.Value, "/testp/DSD/fsdfx/fadasd3/中", StringComparison.OrdinalIgnoreCase);
    }

    [Benchmark, BenchmarkCategory("PathEqual")]
    public void PathEqual()
    {
        var b = _PathEqual(HttpContext);
        var c = _PathEqualTrue(HttpContext);
    }

    [Benchmark, BenchmarkCategory("PathEqual")]
    public void PathEqualV2()
    {
        var b = _PathEqualV2(HttpContext);
        var c = _PathEqualTrueV2(HttpContext);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Regx")]
    public void PathRegxString()
    {
        var b = regx.IsMatch(HttpContext.Request.Path.Value);
    }

    [Benchmark, BenchmarkCategory("Regx")]
    public void PathRegx()
    {
        var b = _PathRegx(HttpContext);
    }

    [Benchmark, BenchmarkCategory("Regx")]
    public void PathRegxV2()
    {
        var b = _PathRegxV2(HttpContext);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("In")]
    public void PathInString()
    {
        var b = set.Contains(HttpContext.Request.Path.Value);
    }

    [Benchmark, BenchmarkCategory("In")]
    public void PathIn()
    {
        var b = _PathIn(HttpContext);
    }

    [Benchmark, BenchmarkCategory("In")]
    public void PathInV2()
    {
        var b = _PathInV2(HttpContext);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("bool")]
    public void IsHttps()
    {
        var b = HttpContext.Request.IsHttps == true;
    }

    [Benchmark, BenchmarkCategory("bool")]
    public void IsHttpsp()
    {
        var b = _IsHttps(HttpContext);
    }

    [Benchmark, BenchmarkCategory("bool")]
    public void IsHttpspV2()
    {
        var b = _IsHttpsV2(HttpContext);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Complex")]
    public void Complex()
    {
        var req = HttpContext.Request;
        var b = req.IsHttps == true
            && req.Path.Value.Equals("/testp/DSD/fsdfx/fadasd3/中", StringComparison.OrdinalIgnoreCase)
            && req.Method == "GET"
            && req.Host.ToString() == "x.com"
            && req.Scheme == "https"
            && req.Protocol == "HTTP/1.1"
            && req.ContentType == "json"
            && queryRegx.IsMatch(req.QueryString.ToString())
            && !(req.Scheme == "http");
    }

    [Benchmark, BenchmarkCategory("Complex")]
    public void Complexp()
    {
        var b = _PathComplex(HttpContext);
    }

    [Benchmark, BenchmarkCategory("Complex")]
    public void ComplexpV2()
    {
        var b = _PathComplexV2(HttpContext);
    }
}