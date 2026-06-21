using Vkr.Optimization;

namespace Vkr.SyntheticBenchmarks;

/// <summary>
/// Набор из 22 тестовых функций для оценки работы оптимизаторов.
/// Все функции минимизируются: f(x) -> scalar.
/// </summary>
public static class TestFunctions
{
    public sealed record Benchmark(
        int Id,
        string Name,
        Func<double[], double> Function,
        Bounds[] Bounds,
        double Min,
        double Accept);

    public static IReadOnlyList<Benchmark> CreateAll(int D)
    {
        if (D < 2) throw new ArgumentOutOfRangeException(nameof(D), "Размерность должна быть >= 2");

        static Bounds[] Box(int d, double lo, double hi)
        {
            var b = new Bounds[d];
            for (int i = 0; i < d; i++) 
            {
                b[i] = new Bounds(lo, hi);
            }
            return b;
        }

        // Для Михалевича минимумы зависят от размерности
        static (double min, double acc) MichalewiczTargets(int d)
        {
            return d switch
            {
                2 => (-1.801303, -1.80), 
                30 => (-30.0, -29.0),
                50 => (-50.0, -49.0),
                100 => (-100.0, -99.0),
                _ => (-d, -(d - 1)) // грубое приближение для других D
            };
        }

        var (min22, acc22) = MichalewiczTargets(D);

        return new List<Benchmark>
        {
            new( 1, "Sphere", Sphere, Box(D, -100, 100), 0, 1e-8),
            new( 2, "Elliptic", Elliptic, Box(D, -100, 100), 0, 1e-8),
            new( 3, "SumSquare", SumSquare, Box(D, -10, 10), 0, 1e-8),
            new( 4, "sumPower", SumPower, Box(D, -1, 1), 0, 1e-8),
            new( 5, "Schwefel 2.22", Schwefel222, Box(D, -10, 10), 0, 1e-8),
            new( 6, "Schwefel 2.21", Schwefel221, Box(D, -100, 100), 0, 1e0),
            new( 7, "Step", Step, Box(D, -100, 100), 0, 1e-8),
            new( 8, "Exponential", Exponential, Box(D, -10, 10), 0, 1e-8),
            new( 9, "Quartic", Quartic, Box(D, -1.28, 1.28), 0, 1e-1),
            new(10, "Rosenbrock", Rosenbrock, Box(D, -5, 10), 0, 1e-1),
            new(11, "Rastrigin", Rastrigin, Box(D, -5.12, 5.12), 0, 1e-8),
            new(12, "NCRastrigin", NCRastrigin, Box(D, -5.12, 5.12), 0, 1e-8),
            new(13, "Griewank", Griewank, Box(D, -600, 600), 0, 1e-8),
            new(14, "Schwefel2.26", Schwefel226, Box(D, -500, 500), 0, 1e-8),
            new(15, "Ackley", Ackley, Box(D, -50, 50), 0, 1e-8),
            new(16, "Penalized1", Penalized1, Box(D, -100, 100), 0, 1e-8),
            new(17, "Penalized2", Penalized2, Box(D, -100, 100), 0, 1e-8),
            new(18, "Alpine", Alpine, Box(D, -10, 10), 0, 1e-8),
            new(19, "Levy", Levy, Box(D, -10, 10), 0, 1e-8),
            new(20, "Weierstrass", Weierstrass, Box(D, -1, 1), 0, 1e-8),
            // Фактически это функция Стиблинского-Танга, хотя в некоторых статьях пишут Himmelblau
            new(21, "Styblinski-Tang", StyblinskiTang, Box(D, -5, 5), -39.16617, -39),
            new(22, "Michalewicz", Michalewicz, Box(D, 0, Math.PI), min22, acc22),
        };
    }

    // ---------------- Реализации функций ----------------

    public static double Sphere(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++) s += x[i] * x[i];
        return s;
    }

    public static double Elliptic(double[] x)
    {
        int D = x.Length;
        if (D == 1) return x[0] * x[0];
        double s = 0;
        for (int i = 1; i <= D; i++)
        {
            double exp = (double)(i - 1) / (D - 1);
            double w = Math.Pow(1e6, exp);
            s += w * x[i - 1] * x[i - 1];
        }
        return s;
    }

    public static double SumSquare(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++) s += (i + 1) * x[i] * x[i];
        return s;
    }

    public static double SumPower(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++) s += Math.Pow(Math.Abs(x[i]), i + 2);
        return s;
    }

    public static double Schwefel222(double[] x)
    {
        double sum = 0;
        double prod = 1;
        for (int i = 0; i < x.Length; i++)
        {
            double a = Math.Abs(x[i]);
            sum += a;
            prod *= a;
        }
        return sum + prod;
    }

    public static double Schwefel221(double[] x)
    {
        double m = 0;
        for (int i = 0; i < x.Length; i++) m = Math.Max(m, Math.Abs(x[i]));
        return m;
    }

    public static double Step(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double v = Math.Floor(x[i] + 0.5);
            s += v * v;
        }
        return s;
    }

    public static double Exponential(double[] x)
    {
        double sum = 0;
        for (int i = 0; i < x.Length; i++) sum += x[i];
        return Math.Exp(0.5 * sum);
    }

    public static double Quartic(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++) s += (i + 1) * Math.Pow(x[i], 4);
        return s;
    }

    public static double Rosenbrock(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length - 1; i++)
        {
            double a = x[i + 1] - (x[i] * x[i]);
            double b = x[i] - 1.0;
            double inner = (100.0 * a) + (b * b);
            s += inner * inner;
        }
        return s;
    }

    public static double Rastrigin(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++)
            s += (x[i] * x[i]) - (10.0 * Math.Cos(2.0 * Math.PI * x[i])) + 10.0;
        return s;
    }

    public static double NCRastrigin(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double xi = x[i];
            double yi = Math.Abs(xi) < 0.5 ? xi : (Math.Round(2.0 * xi) / 2.0);
            s += (yi * yi) - (10.0 * Math.Cos(2.0 * Math.PI * yi)) + 10.0;
        }
        return s;
    }

    public static double Griewank(double[] x)
    {
        double sum = 0;
        double prod = 1;
        for (int i = 0; i < x.Length; i++)
        {
            sum += x[i] * x[i];
            prod *= Math.Cos(x[i] / Math.Sqrt(i + 1));
        }
        return (sum / 4000.0) - prod + 1.0;
    }

    public static double Schwefel226(double[] x)
    {
        double sum = 0;
        for (int i = 0; i < x.Length; i++) sum += x[i] * Math.Sin(Math.Sqrt(Math.Abs(x[i])));
        return (418.9828872724338 * x.Length) - sum;
    }

    public static double Ackley(double[] x)
    {
        int D = x.Length;
        double sumSq = 0;
        double sumCos = 0;
        for (int i = 0; i < D; i++)
        {
            sumSq += x[i] * x[i];
            sumCos += Math.Cos(2.0 * Math.PI * x[i]);
        }
        double term1 = -20.0 * Math.Exp(-0.2 * Math.Sqrt(sumSq / D));
        double term2 = -Math.Exp(sumCos / D);
        return 20.0 + Math.E + term1 + term2;
    }

    public static double Penalized1(double[] x)
    {
        int D = x.Length;
        var y = new double[D];
        for (int i = 0; i < D; i++) y[i] = 1.0 + (0.25 * (x[i] + 1.0));

        double sum = 10.0 * Math.Pow(Math.Sin(Math.PI * y[0]), 2);
        for (int i = 0; i < D - 1; i++)
        {
            double term = (y[i] - 1.0) * (y[i] - 1.0);
            sum += term * (1.0 + (10.0 * Math.Pow(Math.Sin(Math.PI * y[i + 1]), 2)));
        }
        sum += (y[D - 1] - 1.0) * (y[D - 1] - 1.0);
        sum *= (Math.PI / D);

        double penalty = 0;
        for (int i = 0; i < D; i++) penalty += U(x[i], 10.0, 100.0, 4.0);
        return sum + penalty;
    }

    public static double Penalized2(double[] x)
    {
        int D = x.Length;
        double sum = Math.Pow(Math.Sin(Math.PI * x[0]), 2);

        for (int i = 0; i < D - 1; i++)
        {
            double term = (x[i] - 1.0) * (x[i] - 1.0);
            sum += term * (1.0 + Math.Pow(Math.Sin(3.0 * Math.PI * x[i + 1]), 2));
        }

        sum += (x[D - 1] - 1.0) * (x[D - 1] - 1.0) * (1.0 + Math.Pow(Math.Sin(2.0 * Math.PI * x[D - 1]), 2));
        sum *= 0.1;

        double penalty = 0;
        for (int i = 0; i < D; i++) penalty += U(x[i], 10.0, 100.0, 4.0);
        return sum + penalty;
    }

    public static double Alpine(double[] x)
    {
        int limit = Math.Max(0, x.Length - 1);
        double s = 0;
        for (int i = 0; i < limit; i++) s += Math.Abs((x[i] * Math.Sin(x[i])) + (0.1 * x[i]));
        return s;
    }

    public static double Levy(double[] x)
    {
        int D = x.Length;
        double sum = 0;
        for (int i = 0; i < D - 1; i++)
        {
            double term = (x[i] - 1.0) * (x[i] - 1.0);
            sum += term * (1.0 + Math.Pow(Math.Sin(3.0 * Math.PI * x[i + 1]), 2));
        }
        sum += Math.Pow(Math.Sin(3.0 * Math.PI * x[0]), 2);
        sum += Math.Abs(x[D - 1] - 1.0) * (1.0 + Math.Pow(Math.Sin(3.0 * Math.PI * x[D - 1]), 2));
        return sum;
    }

    public static double Weierstrass(double[] x)
    {
        const double a = 0.5;
        const double b = 3.0;
        const int kMax = 20;
        int D = x.Length;

        double sum = 0;
        for (int i = 0; i < D; i++)
        {
            double inner = 0;
            for (int k = 0; k <= kMax; k++)
                inner += Math.Pow(a, k) * Math.Cos(2.0 * Math.PI * Math.Pow(b, k) * (x[i] + 0.5));
            sum += inner;
        }

        double constant = 0;
        for (int k = 0; k <= kMax; k++)
            constant += Math.Pow(a, k) * Math.Cos(2.0 * Math.PI * Math.Pow(b, k) * 0.5);

        return sum - (D * constant);
    }

    public static double StyblinskiTang(double[] x)
    {
        int D = x.Length;
        double s = 0;
        for (int i = 0; i < D; i++)
        {
            double xi = x[i];
            s += Math.Pow(xi, 4) - (16.0 * xi * xi) + (5.0 * xi);
        }
        return s / D;
    }

    public static double Michalewicz(double[] x)
    {
        double s = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double xi = x[i];
            double inner = Math.Sin(((i + 1) * xi * xi) / Math.PI);
            s += Math.Sin(xi) * Math.Pow(inner, 20);
        }
        return -s;
    }

    private static double U(double x, double a, double k, double m)
    {
        if (x > a) return k * Math.Pow(x - a, m);
        if (x < -a) return k * Math.Pow(-x - a, m);
        return 0;
    }
}
