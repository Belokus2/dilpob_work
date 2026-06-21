using System.Diagnostics;

namespace Vkr.Optimization;

public sealed class AntAnnealingOptimizer : IOptimizer
{
    public sealed record Options(
        int Ants = 60,
        int Iterations = 200,
        int GridPerDim = 151,
        double Alpha = 1.0,
        double Beta = 3.0,
        double Evaporation = 0.15,
        bool UseLocalAnnealing = true,
        int SaStepsPerAnt = 200,
        double SaT0 = 0.6,
        double SaCooling = 0.995,
        double SaStepStd = 0.15,
        double Q0 = 0.85,              
        double LocalUpdate = 0.05,     
        double Tau0 = 1.0,             
        bool NormalizeEta = true,      
        double EtaFloor = 1e-12,       
        bool ShiftGrid = true,         
        double MinWeight = 1e-300,     
        int Seed = 42,
        double Eps = 1e-12);

    private readonly Options _opt;

    public AntAnnealingOptimizer(Options? options = null)
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
        int g = _opt.GridPerDim;

        double[][] grid = new double[d][];
        for (int i = 0; i < d; i++)
        {
            grid[i] = new double[g];
            for (int j = 0; j < g; j++)
            {
                double t = _opt.ShiftGrid
                    ? (j + 0.5) / g
                    : (double)j / (g - 1);

                grid[i][j] = bounds[i].Lo + bounds[i].Range * t;
            }
        }

        double[][] tau = new double[d][];
        for (int i = 0; i < d; i++)
        {
            tau[i] = Enumerable.Repeat(_opt.Tau0, g).ToArray();
        }

        double[] bestX = start is not null ? MathUtil.Clone(start) : MidPoint(bounds);
        MathUtil.ClampInPlace(bestX, bounds);

        double bestF = f.Evaluate(bestX); 
        eval++;

        var trace = new List<OptimizationTracePoint>();
        void Emit(int it)
        {
            var p = new OptimizationTracePoint(it, bestF);
            trace.Add(p);
            traceSink?.Invoke(p);
        }

        Emit(0);

        var etas = new double[g];
        var weights = new double[g];

        for (int it = 1; it <= _opt.Iterations; it++)
        {
            double[] iterBestX = bestX;
            double iterBestF = double.PositiveInfinity;
            int[] iterBestIdx = new int[d];

            for (int a = 0; a < _opt.Ants; a++)
            {
                int[] idx = new int[d];

                for (int dim = 0; dim < d; dim++)
                {
                    double[][] batchPoints = new double[g][];
                    for (int j = 0; j < g; j++)
                    {
                        double[] tmp = MathUtil.Clone(bestX);
                        tmp[dim] = grid[dim][j];
                        batchPoints[j] = tmp;
                    }

                    double[] batchVals = f.EvaluateBatch(batchPoints);
                    eval += g;

                    double batchMin = batchVals[0];
                    for (int j = 1; j < g; j++)
                    {
                        if (batchVals[j] < batchMin) batchMin = batchVals[j];
                    }

                    double shift = batchMin < 0 ? -batchMin : 0;

                    double minEta = double.PositiveInfinity;
                    double maxEta = double.NegativeInfinity;

                    for (int j = 0; j < g; j++)
                    {
                        double e = 1.0 / (batchVals[j] + shift + _opt.Eps);
                        etas[j] = e;
                        if (e < minEta) minEta = e;
                        if (e > maxEta) maxEta = e;
                    }

                    double denom = Math.Max(_opt.Eps, maxEta - minEta);

                    for (int j = 0; j < g; j++)
                    {
                        double t = Math.Pow(tau[dim][j], _opt.Alpha);

                        double etaVal = etas[j];
                        if (_opt.NormalizeEta)
                        {
                            etaVal = (etaVal - minEta) / denom;
                            etaVal = Math.Max(_opt.EtaFloor, etaVal);
                        }

                        double w = t * Math.Pow(etaVal, _opt.Beta);
                        weights[j] = (w < _opt.MinWeight) ? _opt.MinWeight : w;
                    }

                    // ACS переход
                    int chosen = rng.NextDouble() < _opt.Q0
                        ? ArgMax(weights)
                        : rng.SampleFromWeights(weights);

                    idx[dim] = chosen;

                    tau[dim][chosen] = (1.0 - _opt.LocalUpdate) * tau[dim][chosen] + _opt.LocalUpdate * _opt.Tau0;
                }

                var x = new double[d];
                for (int dim = 0; dim < d; dim++)
                {
                    x[dim] = grid[dim][idx[dim]];
                }

                double fx = f.Evaluate(x); 
                eval++;

                if (_opt.UseLocalAnnealing)
                {
                    (x, fx) = ImproveBySA(rng, f, bounds, x, fx, _opt.SaStepsPerAnt, _opt.SaT0, _opt.SaCooling, _opt.SaStepStd, ref eval);
                }

                for (int dim = 0; dim < d; dim++)
                {
                    idx[dim] = MathUtil.NearestIndex(grid[dim], x[dim]);
                }

                if (fx < iterBestF)
                {
                    iterBestF = fx;
                    iterBestX = MathUtil.Clone(x);
                    Array.Copy(idx, iterBestIdx, d);
                }

                if (fx < bestF)
                {
                    bestF = fx;
                    bestX = MathUtil.Clone(x);
                }
            }

            for (int dim = 0; dim < d; dim++)
            {
                for (int j = 0; j < g; j++)
                {
                    tau[dim][j] *= (1.0 - _opt.Evaporation);
                }
            }

            double iterShift = iterBestF < 0 ? -iterBestF : 0;
            double iterBestFitness = 1.0 / (iterBestF + iterShift + _opt.Eps);
            double add = _opt.Evaporation * iterBestFitness;
            for (int dim = 0; dim < d; dim++)
            {
                tau[dim][iterBestIdx[dim]] += add;
            }

            if (it <= 500 || it % 10 == 0 || it == _opt.Iterations)
            {
                Emit(it);
            }
        }

        sw.Stop();
        return new OptimizationResult(bestX, bestF, eval, sw.Elapsed, nameof(AntAnnealingOptimizer), trace);
    }

    private static (double[] x, double fx) ImproveBySA(
        Random rng,
        IObjectiveFunction f,
        Bounds[] bounds,
        double[] startX,
        double startF,
        int steps,
        double t0,
        double cooling,
        double stepStd,
        ref int eval)
    {
        int d = startX.Length;
        var x = MathUtil.Clone(startX);
        double fx = startF;

        double T = t0;

        for (int i = 0; i < steps; i++)
        {
            var y = MathUtil.Clone(x);
            for (int dim = 0; dim < d; dim++)
            {
                y[dim] = bounds[dim].Clamp(y[dim] + rng.NextGaussian(0, stepStd));
            }

            double fy = f.Evaluate(y);
            eval++;

            double delta = fy - fx;
            if (delta <= 0 || rng.NextDouble() < Math.Exp(-delta / Math.Max(T, 1e-12)))
            {
                x = y;
                fx = fy;
            }
            T *= cooling;
        }

        return (x, fx);
    }

    private static double[] MidPoint(Bounds[] bounds)
    {
        var x = new double[bounds.Length];
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = bounds[i].Lo + bounds[i].Range * 0.5;
        }
        return x;
    }

    private static int ArgMax(double[] xs)
    {
        int best = 0;
        double max = xs[0];
        for (int i = 1; i < xs.Length; i++)
        {
            if (xs[i] > max)
            {
                max = xs[i];
                best = i;
            }
        }
        return best;
    }
}
