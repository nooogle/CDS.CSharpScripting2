using AwesomeAssertions;
using CDS.CSharpScript2;


namespace UnitTests;

[TestClass]
public partial class UT_Compilation
{
    /// <summary>
    /// Globals class to store MathNet calculation results
    /// </summary>
    public class MathGlobals
    {
        public MathNetResults Result { get; set; } = new MathNetResults();
    }

    /// <summary>
    /// Container for MathNet calculation results
    /// </summary>
    public class MathNetResults
    {
        public double Determinant { get; set; }
        public System.Numerics.Complex[] Eigenvalues { get; set; } = new System.Numerics.Complex[0];
        public double[] VectorProduct { get; set; } = new double[0];
    }



    /// <summary>
    /// This code is essentially the same as the script code, but is executed in the test method.
    /// We can use this to help verify that the script is working correctly and that we get
    /// the same results as the equivalent code in the test method.
    /// </summary>
    /// <returns></returns>
    static MathNetResults MathnetTest()
    {
        // Create a 3x3 matrix with some values
        var matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(new double[,] {
{ 1, 2, 3 },
{ 4, 5, 6 },
{ 7, 8, 9 }
});

        // Compute determinant
        MathNetResults Result = new MathNetResults();
        Result.Determinant = matrix.Determinant();

        // Compute eigenvalues
        var evd = matrix.Evd();
        Result.Eigenvalues = evd.EigenValues.ToArray();

        // Matrix-vector multiplication
        var vector = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new[] { 1.0, 2.0, 3.0 });
        var product = matrix.Multiply(vector);
        Result.VectorProduct = product.ToArray();

        // All done
        return Result;
    }


    [TestMethod]
    public async Task MathNet_UsedInScript_PerformsCalculationCorrectly()
    {
        // Define globals for script execution
        var globals = new MathGlobals();

        // Get the results from the MathNet code for comparison
        var expectedResults = MathnetTest();


        // Script that uses MathNet.Numerics functionality
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

        // Setup environment with MathNet.Numerics references
        var env = ScriptEnvironment
            .Default
            .WithAdditionalNamespaceForType<MathNet.Numerics.LinearAlgebra.Matrix<double>>()
            .WithAdditionalReferenceForType<MathNet.Numerics.LinearAlgebra.Matrix<double>>()
            .WithAdditionalReferenceName("System.Runtime.Numerics")
            .WithGlobalType<MathGlobals>();

        // Create script manager and run the script
        var scriptManager = await ScriptManager.CreateAsync(env);
        scriptManager = await scriptManager.ApplyScriptAsync(script);

        var diagnostics = await scriptManager.GetDiagnosticsAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();
        
        // Verify no compilation errors
        compilationOutput.ErrorCount.Should().Be(0, "Script should compile without errors");

        // Run the script
        await scriptManager.RunAsync(globals);

        // Verify the determinant
        globals.Result.Determinant.Should().Be(expectedResults.Determinant);

        // Verify the eigenvalues (comparing complex numbers)
        globals.Result.Eigenvalues.Length.Should().Be(expectedResults.Eigenvalues.Length);
        for (int i = 0; i < expectedResults.Eigenvalues.Length; i++)
        {
            globals.Result.Eigenvalues[i].Real.Should().BeApproximately(expectedResults.Eigenvalues[i].Real, 1e-10);
            globals.Result.Eigenvalues[i].Imaginary.Should().BeApproximately(expectedResults.Eigenvalues[i].Imaginary, 1e-10);
        }

        // Verify the vector product
        globals.Result.VectorProduct.Length.Should().Be(expectedResults.VectorProduct.Length);
        for (int i = 0; i < expectedResults.VectorProduct.Length; i++)
        {
            globals.Result.VectorProduct[i].Should().BeApproximately(expectedResults.VectorProduct[i], 1e-10);
        }
    }
}
