namespace Vkr.Optimization;

internal static class MathUtil
{
    public static double NextGaussian(this Random rng, double mean = 0, double stdDev = 1)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double r = Math.Sqrt(-2.0 * Math.Log(u1));
        double theta = 2.0 * Math.PI * u2;
        return mean + stdDev * (r * Math.Cos(theta));
    }

    public static int SampleFromWeights(this Random rng, double[] weights)
    {
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) 
        {
            sum += weights[i];
        }
        
        if (sum <= 0) return rng.Next(weights.Length);

        double r = rng.NextDouble() * sum;
        double acc = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            acc += weights[i];
            if (r <= acc) return i;
        }
        return weights.Length - 1;
    }

    public static double[] RandomPoint(Random rng, Bounds[] bounds)
    {
        var x = new double[bounds.Length];
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = bounds[i].Lo + rng.NextDouble() * bounds[i].Range;
        }
        return x;
    }

    public static void ClampInPlace(double[] x, Bounds[] bounds)
    {
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = bounds[i].Clamp(x[i]);
        }
    }

    public static double[] Clone(double[] x) => (double[])x.Clone();

    public static int NearestIndex(double[] sortedGrid, double value)
    {
        int lo = 0, hi = sortedGrid.Length - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (sortedGrid[mid] < value) 
                lo = mid;
            else 
                hi = mid;
        }
        return Math.Abs(sortedGrid[lo] - value) <= Math.Abs(sortedGrid[hi] - value) ? lo : hi;
    }
}
