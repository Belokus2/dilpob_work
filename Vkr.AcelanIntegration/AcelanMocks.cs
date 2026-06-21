// Заглушки для классов ACELAN.
// Нужны чтобы проект компилировался без тяжелого МКЭ-вычислителя.

namespace Acelan.Fem.Entity
{
    public class Model
    {
        private Mesh _mesh = new();
        public Model() {}
        public Model(object dataSource) {}
        
        public Mesh GetMesh() => _mesh;
        public void SetBodies(object[] bodies) {}
        public void ReenumByPosition() {}
        public void Solve(object settings) {}
        public LoadCase GetSelectedLoadCase() => new();
    }

    public class Mesh
    {
        public int CountVolumes() => 10;
        public Element GetVolume(int tag) => new();
        public List<int> GetOuterNodes() => new() { 1, 2, 3 };
        public void SetMaterialToElement(int tag, int mat) {}
        public List<Node> GetNodesForElement(int tag) => new() { new Node() };
    }

    public class Element
    {
        public int BodyNumber { get; set; } = 1;
    }

    public class Node
    {
        public double X { get; set; } = 0.5;
        public double Y { get; set; } = 0.5;
        public double Z { get; set; } = 0.5;

        public Node SetCoords(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
            return this;
        }

        public double LengthSquared() => X * X + Y * Y + Z * Z;

        public static Node operator -(Node a, Node b) => 
            new Node().SetCoords(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public class LoadCase
    {
        public Solution GetSolution() => new();
    }

    public class Solution
    {
        public Dictionary<int, double> GetResults(Variable v) => new() { {1, 0.1}, {2, 0.2}, {3, 0.3} };
    }

    public enum Variable { Ux, Uy, Uz }

    public class VariableManager
    {
        public static object ElasticUnknowns() => new object();
    }

    public class Solid
    {
        public Solid SetName(string name) => this;
        public Solid SetDensity(double d) => this;
        public Solid SetIsotropicStiffness(double e, double nu) => this;
    }

    public class DbInitializer
    {
        public static void InitLibraries(string path) {}
    }

    public class ScriptParser
    {
        public void ParseBoundaryConditions(string code, ref Model model) {}
    }
}

namespace Acelan.Fem.Materials
{
    public class MaterialLibrary
    {
        public static MaterialLibrary GetDefaultLibrary() => new();
        public void LoadMaterials() {}
        public object GetMaterial(string name) => new object();
    }
}

namespace Acelan.Fem.Elements
{
    public enum ElementType { Hex8 }
    public class ElementFactory
    {
        public static object BuildElement(ElementType type, object vars) => new object();
    }
}

namespace Acelan.Fem.Solvers
{
    public class SolverSettings
    {
        public Solver.SolverOption Option { get; set; }
        public Acelan.Fem.Elements.ElementType Element { get; set; }
    }
    public class Solver { public enum SolverOption { CSparseLU } }
}

namespace Acelan.Fem.DataSources
{
    public class OctreeDataSource { public void Init(object u) {} }
}

namespace Acelan.Triangulation
{
    public class Body
    {
        public static Body Default() => new();
    }
    public class OctreeTriangulator
    {
        public OctreeTriangulator(Body b, object c, int i1, int i2) {}
        public object Triangulate(int level) => new object();
    }
}

namespace Acelan.Triangulation.MaterialConverters
{
    public class ThreeOneConverter
    {
        public ThreeOneConverter(int a, int b) {}
    }
}

namespace Acelan.Triangulation.XmlFormat
{
    public class XmlFormat
    {
        public XmlFormat(Body b, object m) {}
    }
}
