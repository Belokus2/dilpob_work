namespace Vkr.Optimization;

public sealed class CachedObjectiveFunction : IObjectiveFunction
{
    private readonly Dictionary<VectorKey, double> _cache = new();
    private readonly IObjectiveFunction _inner;
    private readonly double _tolerance;

    private int _cacheHits;
    private int _cacheMisses;

    public int CacheHits => _cacheHits;
    public int CacheMisses => _cacheMisses;

    public CachedObjectiveFunction(IObjectiveFunction inner, double tolerance = 1e-8)
    {
        _inner = inner;
        _tolerance = tolerance;
    }

    public double Evaluate(double[] point)
    {
        var key = new VectorKey(point, _tolerance);
        if (_cache.TryGetValue(key, out double val))
        {
            _cacheHits++;
            return val;
        }

        _cacheMisses++;
        val = _inner.Evaluate(point);
        _cache[key] = val;
        return val;
    }

    public double[] EvaluateBatch(double[][] points)
    {
        var results = new double[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            results[i] = Evaluate(points[i]);
        }
        return results;
    }

    private readonly struct VectorKey : IEquatable<VectorKey>
    {
        private readonly double[] _p;
        private readonly double _tol;

        public VectorKey(double[] p, double tol)
        {
            _p = p;
            _tol = tol;
        }

        public bool Equals(VectorKey other)
        {
            if (_p.Length != other._p.Length) return false;
            for (int i = 0; i < _p.Length; i++)
            {
                if (Math.Abs(_p[i] - other._p[i]) > _tol)
                    return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => obj is VectorKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Округляем до заданной точности
                int digits = Math.Max(0, (int)Math.Floor(-Math.Log10(_tol)));
                foreach (var v in _p)
                {
                    double rounded = Math.Round(v, digits);
                    hash = hash * 31 + rounded.GetHashCode();
                }
                return hash;
            }
        }
    }
}
