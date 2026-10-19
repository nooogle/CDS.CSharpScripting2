using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Documentation;

namespace UnitTests;

[TestClass]
public class UT_DocPages
{
    private const string RoundScript = "var price = 19.987; var shown = Math.Round(price, 2);";

    [TestMethod]
    public async Task MethodPage_FromCall_DescribesTheOverloadCalled()
    {
        var page = await GetPageAsync(RoundScript, "Round");

        page.Should().NotBeNull();
        page!.Title.Should().Be("Math.Round");
        page.Signature.Should().Be("public static double Round(double value, int digits)");
        page.KindLine.ToPlainText().Should().Be("Method · Math");
        var math = page.KindLine.Single(r => r.Text == "Math");
        math.Kind.Should().Be(DocRunKind.Link);
        math.Target!.DocumentationId.Should().Be("T:System.Math");
        page.Summary.ToPlainText().Should().StartWith("Rounds a double-precision floating-point value to a specified number of fractional digits");
    }

    [TestMethod]
    public async Task MethodPage_ListsParametersReturnsAndExceptions()
    {
        var page = await GetPageAsync(RoundScript, "Round");

        page!.Parameters.Select(p => p.Name.ToPlainText()).Should().Equal("value", "digits");
        page.Parameters[0].Description.ToPlainText().Should().Be("A double-precision floating-point number to be rounded.");
        page.Parameters[1].Description.ToPlainText().Should().Be("The number of fractional digits in the return value.");

        page.Returns.ToPlainText().Should().Be("The number nearest to value that contains a number of fractional digits equal to digits.");
        page.Returns.Where(r => r.Kind == DocRunKind.Code).Select(r => r.Text).Should().Equal("value", "digits");

        page.Exceptions.Should().ContainSingle();
        var name = page.Exceptions[0].Name.Single();
        name.Kind.Should().Be(DocRunKind.Link);
        name.Text.Should().Be("ArgumentOutOfRangeException");
        name.Target!.DocumentationId.Should().Be("T:System.ArgumentOutOfRangeException");
        page.Exceptions[0].Description.ToPlainText().Should().Be("digits is less than 0 or greater than 15.");
    }

    [TestMethod]
    public async Task MethodPage_ListsEveryOverloadWithTheCurrentOneMarked()
    {
        var page = await GetPageAsync(RoundScript, "Round");

        page!.Overloads.Should().HaveCount(8);
        page.Overloads.Should().ContainSingle(o => o.IsCurrent).Which.Label.Should().Be("Round(double, int)");
        page.Overloads.Should().OnlyContain(o => o.Label.StartsWith("Round("));
    }

    [TestMethod]
    public async Task MethodPage_FromExtensionCall_DescribesTheUnreducedMethod()
    {
        const string script = "var xs = new[] { 1, 2 }; var zs = xs.Select(x => x * 2);";
        var environment = ScriptEnvironment.Default
            .WithAdditionalNamespaceName("System.Linq")
            .WithAdditionalReferenceForType(typeof(System.Linq.Enumerable));
        var context = (await ScriptContext.CreateAsync(environment)).ApplyScript(script);
        var page = await new ScriptAnalyser(context).GetDocPageAsync(script.IndexOf("Select") + 1);

        page.Should().NotBeNull();
        page!.Title.Should().Be("Enumerable.Select");
        page.Signature.Should().Contain("this IEnumerable<TSource> source");
        page.Target!.DocumentationId.Should().StartWith("M:System.Linq.Enumerable.Select");
        page.Overloads.Should().ContainSingle(o => o.IsCurrent);
    }

    [TestMethod]
    public async Task TypePage_ForStaticClass()
    {
        var page = await GetPageAsync(RoundScript, "Math");

        page.Should().NotBeNull();
        page!.Title.Should().Be("Math");
        page.KindLine.ToPlainText().Should().Be("Static class · System");
        page.Signature.Should().Be("public static class Math");
        page.Summary.ToPlainText().Should().StartWith("Provides constants and static methods");
    }

    [TestMethod]
    public async Task TypePage_GroupsDeclaredPublicMembers()
    {
        var page = await GetPageAsync(RoundScript, "Math");

        page!.MemberGroups.Select(g => g.Heading).Should().Equal("Fields", "Methods");
        page.MemberGroups[0].Members.Select(m => m.Name).Should().Contain(["E", "PI", "Tau"]);
        page.MemberGroups[1].Members.Single(m => m.Name == "Round").OverloadCount.Should().Be(8);
        var names = page.MemberGroups.SelectMany(g => g.Members).Select(m => m.Name).ToList();
        names.Should().NotContain(n => n.StartsWith("get_") || n.StartsWith("set_") || n.StartsWith("op_"));
        page.MemberGroups[1].Members.Select(m => m.Name)
            .Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task TypePage_ForLargeType_BuildsAllGroups()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var page = await GetPageAsync("string s = \"\";", "string");
        watch.Stop();

        page.Should().NotBeNull();
        page!.Title.Should().Be("String");
        page.MemberGroups.Select(g => g.Heading).Should().Contain(["Properties", "Methods", "Constructors"]);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    /// <summary>Asks for a page one character into <paramref name="marker"/>.</summary>
    private static async Task<DocPage?> GetPageAsync(string script, string marker)
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        return await new ScriptAnalyser(context).GetDocPageAsync(script.IndexOf(marker) + 1);
    }
}
