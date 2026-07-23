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
    [TestCategory("compilation")]
    public async Task DualPath_ValidScript_BothPathsReportNoErrors()
    {
        var context = await ScriptContext.CreateAsync(ScriptEnvironment.Default);
        context = context.ApplyScript("var x = Math.Sqrt(4);");

        var workspaceDiagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync();

        workspaceDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Should().BeEmpty("workspace path should report no errors for a valid script");

        executable.HasErrors.Should().BeFalse("execution path should report no errors for a valid script");

        context.Dispose();
    }

    [TestMethod]
    [TestCategory("compilation")]
    public async Task DualPath_ScriptWithError_BothPathsReportErrors()
    {
        var context = await ScriptContext.CreateAsync(ScriptEnvironment.Default);
        context = context.ApplyScript("var x = new UndefinedType123();");

        var workspaceDiagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync();

        workspaceDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Should().NotBeEmpty("workspace path should report errors for an invalid script");

        executable.HasErrors.Should().BeTrue("execution path should report errors for an invalid script");

        context.Dispose();
    }

    [TestMethod]
    [TestCategory("compilation")]
    public async Task DualPath_UsingDeclaration_BothPathsReportNoErrors()
    {
        var context = await ScriptContext.CreateAsync(ScriptEnvironment.Default);
        context = context.ApplyScript("{ using var f2 = System.IO.File.Create(\"x\"); }");

        var analyser = new ScriptAnalyser(context);
        var workspaceDiagnostics = await analyser.GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync();

        workspaceDiagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .Should().BeEmpty("the workspace should parse scoped C# using declarations");

        executable.HasErrors.Should().BeFalse("the execution compiler should parse scoped C# using declarations");

        context.Dispose();
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
            .WithAdditionalReferenceForType<System.Numerics.Complex>()
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
