using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Http;
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
    private Regex regx;
    private Func<HttpContext, bool> _PathRegx;
    private Func<HttpContext, bool> _PathRegxV2;

    public Path_HttpRoutingStatementParserBenchmarks()
    {
        this.HttpContext = new DefaultHttpContext();
        var req = HttpContext.Request;
        req.Path = "/testp/dsd/fsdfx/fadasd3/中";

        regx = new Regex(@"^[/]testp.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        _PathRegx = HttpRoutingStatementParser.ConvertToFunc("Path ~= '^[/]testp.*'");
        _PathRegxV2 = HttpRoutingStatementParser.ConvertToFunction("Path ~= '^[/]testp.*'");
        _PathEqual = HttpRoutingStatementParser.ConvertToFunc("Path = '/testp'");
        _PathEqualV2 = HttpRoutingStatementParser.ConvertToFunction("Path = '/testp'");
        _PathEqualTrue = HttpRoutingStatementParser.ConvertToFunc("Path = '/testp/DSD/fsdfx/fadasd3/中'");
        _PathEqualTrueV2 = HttpRoutingStatementParser.ConvertToFunction("Path = '/testp/DSD/fsdfx/fadasd3/中'");
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
}