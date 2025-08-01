using BenchmarkDotNet.Running;

var a = new Path_HttpRoutingStatementParserBenchmarks();
a.Template();
a.TemplateF();

var summary = BenchmarkRunner.Run<Path_HttpRoutingStatementParserBenchmarks>();