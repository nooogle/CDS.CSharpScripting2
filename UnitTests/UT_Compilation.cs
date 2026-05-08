using AwesomeAssertions;
using CDS.CSharpScript2;


namespace UnitTests;

[TestClass]
public partial class UT_Compilation
{
    public class MathGlobals
    {
        public MathNetResults Result { get; set; } = new MathNetResults();
    }

    public class MathNetResults
    {
        public double Determinant { get; set; }
        public System.Numerics.Complex[] Eigenvalues { get; set; } = new System.Numerics.Complex[0];
        public double[] VectorProduct { get; set; } = new double[0];
    }

    static MathNetResults MathnetTest()
    {
        var matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(new double[,] {
{ 1, 2, 3 },
{ 4, 5, 6 },
{ 7, 8, 9 }
});

        MathNetResults Result = new MathNetResults();
        Result.Determinant = matrix.Determinant();

        var evd = matrix.Evd();
        Result.Eigenvalues = evd.EigenValues.ToArray();

        var vector = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new[] { 1.0, 2.0, 3.0 });
        var product = matrix.Multiply(vector);
        Result.VectorProduct = product.ToArray();

        return Result;
    }

    [TestMethod]
    public async Task MathNet_UsedInScript_PerformsCalculationCorrectly()
    {
        var globals = new MathGlobals();
        var expectedResults = MathnetTest();

        string script = @"
using MathNet.Numerics.LinearAlgebra;

// Create a 3x3 matrix with some values
var matrix = Matrix<double>.Build.DenseOfArray(new double[,] {
    { 1, 2, 3 },
    { 4, 5, 6 },
    { 7, 8, 9 }
});

// Compute determinant
Result.Determinant = matrix.Determinant();

// Compute eigenvalues
var evd = matrix.Evd();
Result.Eigenvalues = evd.EigenValues.ToArray();

// Matrix-vector multiplication
var vector = Vector<double>.Build.Dense(new[] { 1.0, 2.0, 3.0 });
var product = matrix.Multiply(vector);
Result.VectorProduct = product.ToArray();
";

        var env = ScriptEnvironment
            .Default
            .WithAdditionalNamespaceForType<MathNet.Numerics.LinearAlgebra.Matrix<double>>()
            .WithAdditionalReferenceForType<MathNet.Numerics.LinearAlgebra.Matrix<double>>()
            .WithAdditionalReferenceName("System.Runtime.Numerics")
            .WithGlobalType<MathGlobals>();

        var context = await ScriptContext.CreateAsync(env);
        context = context.ApplyScript(script);

        var analyser = new ScriptAnalyser(context);
        var diagnostics = await analyser.GetDiagnosticsAsync();

        var executable = await new ScriptExecutor(context).CompileAsync();
        executable.CompilationOutput.ErrorCount.Should().Be(0, "Script should compile without errors");

        await executable.RunAsync(globals);

        globals.Result.Determinant.Should().Be(expectedResults.Determinant);
        globals.Result.Eigenvalues.Length.Should().Be(expectedResults.Eigenvalues.Length);
        for (int i = 0; i < expectedResults.Eigenvalues.Length; i++)
        {
            globals.Result.Eigenvalues[i].Real.Should().BeApproximately(expectedResults.Eigenvalues[i].Real, 1e-10);
            globals.Result.Eigenvalues[i].Imaginary.Should().BeApproximately(expectedResults.Eigenvalues[i].Imaginary, 1e-10);
        }

        globals.Result.VectorProduct.Length.Should().Be(expectedResults.VectorProduct.Length);
        for (int i = 0; i < expectedResults.VectorProduct.Length; i++)
            globals.Result.VectorProduct[i].Should().BeApproximately(expectedResults.VectorProduct[i], 1e-10);
    }
}
