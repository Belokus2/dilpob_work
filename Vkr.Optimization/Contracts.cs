namespace Vkr.Optimization;

public readonly record struct Bounds(double Lo, double Hi)
{
    public double Clamp(double v) => v < Lo ? Lo : (v > Hi ? Hi : v);
    public double Range => Hi - Lo;
    public override string ToString() => $"[{Lo}, {Hi}]";
}

public sealed record OptimizationTracePoint(int Iteration, double BestFx);

public sealed record OptimizationResult(
    double[] BestX,
    double BestFx,
    int Evaluations,
    TimeSpan Elapsed,
    string OptimizerName,
    IReadOnlyList<OptimizationTracePoint> Trace);


public interface IObjectiveFunction
{
    double Evaluate(double[] point);
    double[] EvaluateBatch(double[][] points);
}


public sealed class LocalObjectiveFunction : IObjectiveFunction
{
    private readonly Func<double[], double> _f;

    public LocalObjectiveFunction(Func<double[], double> f)
    {
        _f = f;
    }

    public double Evaluate(double[] point) => _f(point);

    public double[] EvaluateBatch(double[][] points)
    {
        var res = new double[points.Length];
        for (int i = 0; i < points.Length; i++) 
        {
            res[i] = _f(points[i]);
        }
        return res;
    }
}


public interface IOptimizer
{
    OptimizationResult Minimize(
        IObjectiveFunction f,
        Bounds[] bounds,
        double[]? start = null,
        Action<OptimizationTracePoint>? traceSink = null);
}
