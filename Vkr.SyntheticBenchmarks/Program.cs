using System.Globalization;
using Vkr.Optimization;
using Vkr.SyntheticBenchmarks;

// чтобы в CSV были точки, а не запятые
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("=== Запуск синтетических тестов ВКР ===");

int D = 2; // Размерность по умолчанию
if (args.Length > 0 && int.TryParse(args[0], out int parsedD))
{
    D = parsedD;
}

Console.WriteLine($"Размерность D = {D}");

var runner = new BenchmarkRunner(new BenchmarkRunner.Options(
    Runs: 30,
    SaveTracesToCsv: true,
    OutputDirectory: $"results/D{D}"
));

var functions = TestFunctions.CreateAll(D);

// Открываем общий отчет
Directory.CreateDirectory($"results/D{D}");
using var reportWriter = new StreamWriter($"results/D{D}/summary_report.csv");
reportWriter.WriteLine("Function,Optimizer,SuccessRate,MinFx,MeanFx,StdFx,MeanEvals,MeanTimeMs");

foreach (var bench in functions)
{
    Console.WriteLine($"\n--- Тестируем: f{bench.Id} {bench.Name} ---");
    var f = new LocalObjectiveFunction(bench.Function);

    // Создаем фабрику для генерации ОДИНАКОВЫХ начальных точек для всех оптимизаторов на каждом ране
    double[]? StartPointFactory(int runIndex)
    {
        var rng = new Random(1337 + runIndex); // единый сид для начальной точки
        var pt = new double[D];
        for (int i = 0; i < D; i++)
        {
            pt[i] = bench.Bounds[i].Lo + bench.Bounds[i].Range * rng.NextDouble();
        }
        return pt;
    }

    // 1. AntAnnealingOptimizer (Муравьиный отжиг)
    var acoStats = RunOptimizer(
        runner, "ACO-SA", bench, f,
        runIndex => new AntAnnealingOptimizer(new AntAnnealingOptimizer.Options(
            Ants: 60,
            Iterations: 200,
            GridPerDim: 151,
            Seed: 42 + runIndex
        )),
        StartPointFactory
    );
    LogStats(reportWriter, bench.Name, acoStats);

    // 2. SimulatedAnnealingOptimizer (Обычный отжиг)
    var saStats = RunOptimizer(
        runner, "SA", bench, f,
        runIndex => new SimulatedAnnealingOptimizer(new SimulatedAnnealingOptimizer.Options(
            Iterations: 10_000,
            Seed: 42 + runIndex
        )),
        StartPointFactory
    );
    LogStats(reportWriter, bench.Name, saStats);

    // 3. GradientDescentOptimizer (Градиентный спуск - численный)
    var gdStats = RunOptimizer(
        runner, "GD", bench, f,
        runIndex => new GradientDescentOptimizer(null, new GradientDescentOptimizer.Options(
            Steps: 5_000,
            UseNumericalGradient: true,
            Seed: 42 + runIndex
        )),
        StartPointFactory
    );
    LogStats(reportWriter, bench.Name, gdStats);
}

Console.WriteLine("\nТестирование завершено. Результаты в папке results/");

// Вспомогательный метод для запуска
RunStatistics RunOptimizer(
    BenchmarkRunner benchRunner, 
    string optName, 
    TestFunctions.Benchmark bench, 
    IObjectiveFunction f, 
    Func<int, IOptimizer> factory,
    Func<int, double[]?> startFactory)
{
    Console.Write($"{optName,-10} ");
    var (stats, _) = benchRunner.Run(bench.Name, bench.Accept, factory, f, bench.Bounds, startFactory);
    Console.WriteLine($"\n  Success: {stats.SuccessRate:P0} | MeanFx: {stats.MeanFx:E3}");
    return stats;
}

// Запись в итоговый CSV
void LogStats(StreamWriter sw, string funcName, RunStatistics st)
{
    sw.WriteLine($"{funcName},{st.OptimizerName},{st.SuccessRate.ToString(CultureInfo.InvariantCulture)}," +
                 $"{st.MinFx.ToString("E3", CultureInfo.InvariantCulture)},{st.MeanFx.ToString("E3", CultureInfo.InvariantCulture)}," +
                 $"{st.StdFx.ToString("E3", CultureInfo.InvariantCulture)},{st.MeanEvaluations.ToString(CultureInfo.InvariantCulture)}," +
                 $"{st.MeanMs.ToString("F1", CultureInfo.InvariantCulture)}");
}
