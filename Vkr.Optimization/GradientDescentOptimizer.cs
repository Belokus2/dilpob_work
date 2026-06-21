using System.Diagnostics;

namespace Vkr.Optimization;

public sealed class GradientDescentOptimizer : IOptimizer
{
    public sealed record Options(
        int Steps = 5_000,
        double LearningRate = 0.05,
        bool UseNumericalGradient = true,
        double NumericalEps = 1e-6,
        int Seed = 42);

    private readonly Options _opt;
    private readonly Func<double[], double[]>? _grad;

    public GradientDescentOptimizer(Func<double[], double[]>? grad = null, Options? options = null)
    {
        _grad = grad;
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

        double[] x = start is not null ? MathUtil.Clone(start) : MathUtil.RandomPoint(rng, bounds);
        MathUtil.ClampInPlace(x, bounds);

        double fx = f.Evaluate(x); 
        eval++;
        
        double[] bestX = MathUtil.Clone(x);
        double bestF = fx;

        var trace = new List<OptimizationTracePoint>();
        void Emit(int it)
        {
            var p = new OptimizationTracePoint(it, bestF);
            trace.Add(p);
            traceSink?.Invoke(p);
        }

        Emit(0);

        for (int step = 1; step <= _opt.Steps; step++)
        {
            double[] g = _grad is not null
                ? _grad(x)
                : (_opt.UseNumericalGradient
                    ? NumericalGradient(f, x, bounds, _opt.NumericalEps, ref eval)
                    : throw new InvalidOperationException("Аналитический градиент не передан, а численный выключен."));

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = bounds[i].Clamp(x[i] - _opt.LearningRate * g[i]);
            }

            fx = f.Evaluate(x); 
            eval++;
            
            if (fx < bestF)
            {
                bestF = fx;
                bestX = MathUtil.Clone(x);
            }

            if (step <= 2000 || step % 50 == 0 || step == _opt.Steps)
            {
                Emit(step);
            }
        }

        sw.Stop();
        return new OptimizationResult(bestX, bestF, eval, sw.Elapsed, nameof(GradientDescentOptimizer), trace);
    }

    private static double[] NumericalGradient(
        IObjectiveFunction f,
        double[] x,
        Bounds[] bounds,
        double eps,
        ref int eval)
    {
        int d = x.Length;
        var g = new double[d];

        double[][] batchPoints = new double[2 * d][];
        double[] xpArr = new double[d];
        double[] xmArr = new double[d];

        for (int i = 0; i < d; i++)
        {
            double old = x[i];
            xpArr[i] = bounds[i].Clamp(old + eps);
            xmArr[i] = bounds[i].Clamp(old - eps);

            double[] pxp = MathUtil.Clone(x); 
            pxp[i] = xpArr[i];
            
            double[] pxm = MathUtil.Clone(x); 
            pxm[i] = xmArr[i];

            batchPoints[2 * i] = pxp;
            batchPoints[2 * i + 1] = pxm;
        }

        double[] batchVals = f.EvaluateBatch(batchPoints);
        eval += 2 * d;

        for (int i = 0; i < d; i++)
        {
            double fp = batchVals[2 * i];
            double fm = batchVals[2 * i + 1];

            double denom = (xpArr[i] - xmArr[i]);
            g[i] = denom == 0 ? 0 : (fp - fm) / denom;
        }

        return g;
    }
}
