using BenchmarkDotNet.Running;

var a = new Path_HttpRoutingStatementParserBenchmarks();
a.PathRegxString();
a.PathRegx();
a.PathRegxV2();

var summary = BenchmarkRunner.Run<Path_HttpRoutingStatementParserBenchmarks>();