using System.Diagnostics;

namespace Vkr.Optimization;

public sealed class SimulatedAnnealingOptimizer : IOptimizer
{
    public sealed record Options(
        int Iterations = 10_000,
        double T0 = 1.0,
        double Cooling = 0.9995,
        double StepStd = 0.1,
        int Seed = 42);

    private readonly Options _opt;

    public SimulatedAnnealingOptimizer(Options? options = null)
    {
        _opt = options ?? new Options();
    }

    public OptimizationResult Minimize(
        IObjectiveFunction f,
        Bounds[] bounds,
        double[]? start = null,
        Action<OptimizationTracePoint>? traceSink = null)
    {
        var sw = Stopwatch.StartNew();
        var rng = new Random(_opt.Seed);
        int eval = 0;
        int d = bounds.Length;

        double[] x = start is not null ? MathUtil.Clone(start) : MathUtil.RandomPoint(rng, bounds);
        MathUtil.ClampInPlace(x, bounds);

        double fx = f.Evaluate(x); 
        eval++;

        double[] bestX = MathUtil.Clone(x);
        double bestF = fx;

        double T = _opt.T0;
        var trace = new List<OptimizationTracePoint>();

        void Log(int it)
        {
            var p = new OptimizationTracePoint(it, bestF);
            trace.Add(p);
            traceSink?.Invoke(p);
        }

        Log(0);

        for (int it = 1; it <= _opt.Iterations; it++)
        {
            double[] y = MathUtil.Clone(x);
            for (int i = 0; i < d; i++)
            {
                y[i] = bounds[i].Clamp(y[i] + rng.NextGaussian(0, _opt.StepStd));
            }

            double fy = f.Evaluate(y); 
            eval++;
            double delta = fy - fx;

            // Критерий Метрополиса
            bool accept = delta <= 0 || rng.NextDouble() < Math.Exp(-delta / Math.Max(T, 1e-12));
            if (accept)
            {
                x = y;
                fx = fy;
                if (fx < bestF)
                {
                    bestF = fx;
                    bestX = MathUtil.Clone(x);
                }
            }

            T *= _opt.Cooling;

            if (it <= 2000 || it % 50 == 0 || it == _opt.Iterations)
                Log(it);
        }

        sw.Stop();
        return new OptimizationResult(bestX, bestF, eval, sw.Elapsed, nameof(SimulatedAnnealingOptimizer), trace);
    }
}
