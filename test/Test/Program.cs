using BenchmarkDotNet.Running;

var a = new Path_HttpRoutingStatementParserBenchmarks();
a.Complex();
a.Complexp();
a.ComplexpV2();

var summary = BenchmarkRunner.Run<Path_HttpRoutingStatementParserBenchmarks>();