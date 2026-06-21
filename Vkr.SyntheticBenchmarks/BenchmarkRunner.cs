using System.Globalization;
using Vkr.Optimization;

namespace Vkr.SyntheticBenchmarks;

public sealed record RunStatistics(
    string OptimizerName,
    int Runs,
    double MinFx,
    double MeanFx,
    double StdFx,
    double MeanMs,
    double MeanEvaluations,
    double Accept,
    int SuccessCount,
    double SuccessRate);

public sealed class BenchmarkRunner
{
    public sealed record Options(
        int Runs = 30,
        bool SaveTracesToCsv = true,
        string OutputDirectory = "results");

    private readonly Options _opt;

    public BenchmarkRunner(Options? options = null)
    {
        _opt = options ?? new Options();
    }

    // Запуск оптимизатора N раз, сбор статистики. Фабрика нужна чтобы каждый раз seed менялся.
    public (RunStatistics Stats, List<OptimizationResult> Results) Run(
        string functionName,
        double accept,
        Func<int, IOptimizer> optimizerFactory,
        IObjectiveFunction f,
        Bounds[] bounds,
        Func<int, double[]?>? startFactory = null)
    {
        var functionDir = Path.Combine(_opt.OutputDirectory, Sanitize(functionName));
        Directory.CreateDirectory(functionDir);

        var results = new List<OptimizationResult>(_opt.Runs);
        var fxList = new List<double>(_opt.Runs);
        var msList = new List<double>(_opt.Runs);
        var evalList = new List<double>(_opt.Runs);

        for (int run = 0; run < _opt.Runs; run++)
        {
            var optimizer = optimizerFactory(run);
            double[]? start = startFactory?.Invoke(run);
            
            var res = optimizer.Minimize(f, bounds, start);
            results.Add(res);

            fxList.Add(res.BestFx);
            msList.Add(res.Elapsed.TotalMilliseconds);
            evalList.Add(res.Evaluations);

            if (_opt.SaveTracesToCsv)
            {
                var file = Path.Combine(functionDir, $"{res.OptimizerName}_run{run + 1:00}_trace.csv");
                SaveTrace(file, res.Trace);
            }
            // Индикатор прогресса в консоли
            Console.Write("."); 
        }

        int success = results.Count(r => r.BestFx <= accept);

        var stats = new RunStatistics(
            OptimizerName: results.Count > 0 ? results[0].OptimizerName : "(неизвестно)",
            Runs: _opt.Runs,
            MinFx: fxList.Min(),
            MeanFx: fxList.Average(),
            StdFx: StdDev(fxList),
            MeanMs: msList.Average(),
            MeanEvaluations: evalList.Average(),
            Accept: accept,
            SuccessCount: success,
            SuccessRate: _opt.Runs > 0 ? (double)success / _opt.Runs : 0);

        return (stats, results);
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Replace(' ', '_');
    }

    private static void SaveTrace(string path, IReadOnlyList<OptimizationTracePoint> trace)
    {
        using var w = new StreamWriter(path, false);
        w.WriteLine("iteration,bestFx");
        foreach (var p in trace)
        {
            w.WriteLine($"{p.Iteration.ToString(CultureInfo.InvariantCulture)},{p.BestFx.ToString("G17", CultureInfo.InvariantCulture)}");
        }
    }

    private static double StdDev(List<double> xs)
    {
        if (xs.Count <= 1) return 0;
        double mean = xs.Average();
        double variance = xs.Sum(v => (v - mean) * (v - mean)) / (xs.Count - 1);
        return Math.Sqrt(variance);
    }
}
