// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using dotAPNS.Benchmarks;

Console.WriteLine("Hello, World!");
var summary = BenchmarkRunner.Run<ApnsClientBenchmark>();