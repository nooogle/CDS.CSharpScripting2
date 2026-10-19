using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Documentation;
using CDS.CSharpScript2.Editors;

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

    [TestMethod]
    public async Task VariablePage_ForScriptVariable()
    {
        var page = await GetPageAsync(RoundScript, "price,");

        page.Should().NotBeNull();
        page!.Title.Should().Be("price");
        page.Signature.Should().Be("double price");
        page.KindLine.ToPlainText().Should().Be("Variable · Double");
        var link = page.KindLine.Single(r => r.Text == "Double");
        link.Kind.Should().Be(DocRunKind.Link);
        link.Target!.DocumentationId.Should().Be("T:System.Double");
        page.Target.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ParameterPage_HasNoTarget()
    {
        const string script = "double Hyp(double a) => a;";
        var page = await GetPageAsync(script, "a;");

        page.Should().NotBeNull();
        page!.KindLine.ToPlainText().Should().Be("Parameter · Double");
        page.Signature.Should().Be("double a");
        page.Target.Should().BeNull();
    }

    [TestMethod]
    public async Task KeywordForBuiltInType_OpensTheTypePage()
    {
        (await GetPageAsync("int n = 1;", "int"))!.Title.Should().Be("Int32");
        (await GetPageAsync(RoundScript, "var"))!.Title.Should().Be("Double");
    }

    [TestMethod]
    public async Task Namespace_ReturnsNull()
    {
        (await GetPageAsync("System.Console.WriteLine();", "System")).Should().BeNull();
    }

    [TestMethod]
    public async Task BlankLine_ReturnsNull()
    {
        const string script = "var a = 1;\n\nvar b = 2;";
        var context = (await ScriptContext.CreateAsync()).ApplyScript(script);
        (await new ScriptAnalyser(context).GetDocPageAsync(script.IndexOf("\n\n") + 1)).Should().BeNull();
    }

    [TestMethod]
    public async Task Comment_ReturnsNull()
    {
        (await GetPageAsync("// a comment here", "comment")).Should().BeNull();
    }

    [TestMethod]
    public async Task UnresolvedType_ReturnsNull()
    {
        (await GetPageAsync("UnknownType x;", "UnknownType")).Should().BeNull();
    }

    [TestMethod]
    public async Task Target_ResolvesToItsPage()
    {
        var analyser = await AnalyserForAsync("int n = 1;");
        var page = await analyser.GetDocPageAsync(new DocTarget("T:System.ArgumentOutOfRangeException"));

        page!.Title.Should().Be("ArgumentOutOfRangeException");
    }

    [TestMethod]
    public async Task MethodTarget_WithReturnTypeSuffix_ResolvesAndMarksTheOverload()
    {
        var analyser = await AnalyserForAsync(RoundScript);
        var current = (await analyser.GetDocPageAsync(RoundScript.IndexOf("Round") + 1))!.Overloads.Single(o => o.IsCurrent);
        var other = (await analyser.GetDocPageAsync(RoundScript.IndexOf("Round") + 1))!.Overloads.First(o => !o.IsCurrent);

        var page = await analyser.GetDocPageAsync(other.Target);

        page!.IsUnavailable.Should().BeFalse();
        page.Overloads.Should().ContainSingle(o => o.IsCurrent).Which.Target.Should().Be(other.Target);
        current.Target.Should().NotBe(other.Target);
    }

    [TestMethod]
    public async Task ScriptDocs_SeeCrefBecomesALink()
    {
        const string script = "/// <summary>Uses <see cref=\"Math.Sqrt(double)\"/> on <paramref name=\"a\"/>.</summary>\ndouble Hyp(double a) => a;\nHyp(2);";
        var analyser = await AnalyserForAsync(script);
        var page = await analyser.GetDocPageAsync(script.IndexOf("Hyp(2") + 1);

        var link = page!.Summary.Should().ContainSingle(r => r.Kind == DocRunKind.Link).Subject;
        link.Target!.DocumentationId.Should().Be("M:System.Math.Sqrt(System.Double)");
        (await analyser.GetDocPageAsync(link.Target))!.Title.Should().Be("Math.Sqrt");
        page.Summary.Should().ContainSingle(r => r.Kind == DocRunKind.Code && r.Text == "a");
    }

    [TestMethod]
    public async Task ScriptSymbolTarget_ResolvesAfterAnEdit()
    {
        const string script = "double Hyp(double a) => a;\nHyp(2);";
        var context = (await ScriptContext.CreateAsync()).ApplyScript(script);
        var target = (await new ScriptAnalyser(context).GetDocPageAsync(script.IndexOf("Hyp(2") + 1))!.Target!;

        var edited = context.ApplyScript(script + "\nvar z = 1;");
        (await new ScriptAnalyser(edited).GetDocPageAsync(target))!.Title.Should().Be("Hyp");

        var removed = context.ApplyScript("var z = 1;");
        (await new ScriptAnalyser(removed).GetDocPageAsync(target))!.IsUnavailable.Should().BeTrue();
    }

    [TestMethod]
    public async Task UnresolvableCref_IsPlainText()
    {
        const string script = "/// <summary>See <see cref=\"!:NoSuchThing\"/> here.</summary>\nvoid F() { }\nF();";
        var page = await (await AnalyserForAsync(script)).GetDocPageAsync(script.IndexOf("F();") + 1);

        page!.Summary.Should().NotContain(r => r.Kind == DocRunKind.Link);
        page.Summary.ToPlainText().Should().Contain("NoSuchThing");
    }

    [TestMethod]
    public async Task EditorManager_GetDocPageAsync_ByPositionAndTarget()
    {
        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript(RoundScript);

        IDocPageSource source = manager;
        var byPosition = await source.GetDocPageAsync(RoundScript.IndexOf("Round") + 1, CancellationToken.None);
        byPosition!.Title.Should().Be("Math.Round");

        var byTarget = await manager.GetDocPageAsync(new DocTarget("T:System.Math"));
        byTarget!.Title.Should().Be("Math");
        (await manager.GetDocPageAsync(RoundScript.IndexOf("Round") + 1))!.Title.Should().Be("Math.Round");
        (await source.GetDocPageAsync(new DocTarget("T:System.Math"), CancellationToken.None))!.Title.Should().Be("Math");
    }

    [TestMethod]
    public async Task PropertyPage_DescribesTheProperty()
    {
        const string script = "var s = \"abc\"; var n = s.Length;";
        var page = await GetPageAsync(script, "Length");

        page.Should().NotBeNull();
        page!.Title.Should().Be("String.Length");
        page.KindLine.ToPlainText().Should().Be("Property · String");
        page.Signature.Should().Contain("int Length");
        page.Target.Should().NotBeNull();
        page.Summary.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task FieldTarget_ResolvesToAFieldPage()
    {
        var analyser = await AnalyserForAsync("int n = 1;");
        var page = await analyser.GetDocPageAsync(new DocTarget("F:System.Math.PI"));

        page!.IsUnavailable.Should().BeFalse();
        page.Title.Should().Be("Math.PI");
        page.KindLine.ToPlainText().Should().Be("Field · Math");
    }

    [TestMethod]
    public async Task EventTarget_ResolvesToAnEventPage()
    {
        var analyser = await AnalyserForAsync("int n = 1;");
        var page = await analyser.GetDocPageAsync(new DocTarget("E:System.AppDomain.ProcessExit"));

        page!.IsUnavailable.Should().BeFalse();
        page.KindLine.ToPlainText().Should().Be("Event · AppDomain");
    }

    [TestMethod]
    public async Task IndexerTarget_ListsOtherIndexersAsOverloads()
    {
        var analyser = await AnalyserForAsync("int n = 1;");
        var page = await analyser.GetDocPageAsync(new DocTarget("P:System.Collections.Generic.List`1.Item(System.Int32)"));

        page!.IsUnavailable.Should().BeFalse();
        page.KindLine.ToPlainText().Should().StartWith("Property · ");
    }

    [TestMethod]
    public async Task TypePage_MemberLinksAllResolveToPages()
    {
        var analyser = await AnalyserForAsync("int n = 1;");
        var type = await analyser.GetDocPageAsync(new DocTarget("T:System.Math"));

        foreach (var group in type!.MemberGroups)
        {
            var page = await analyser.GetDocPageAsync(group.Members[0].Target);
            page!.IsUnavailable.Should().BeFalse($"{group.Heading} {group.Members[0].Name} should have a page");
        }
    }

    [TestMethod]
    public async Task PropertyCref_BecomesALinkToAPage()
    {
        const string script = "/// <summary>Length is <see cref=\"string.Length\"/>.</summary>\nvoid F() { }\nF();";
        var analyser = await AnalyserForAsync(script);
        var page = await analyser.GetDocPageAsync(script.IndexOf("F();") + 1);

        var link = page!.Summary.Should().ContainSingle(r => r.Kind == DocRunKind.Link).Subject;
        (await analyser.GetDocPageAsync(link.Target!))!.IsUnavailable.Should().BeFalse();
    }

    [TestMethod]
    public async Task NamespaceCref_IsPlainText()
    {
        const string script = "/// <summary>In <see cref=\"N:System.Text\"/> namespace.</summary>\nvoid F() { }\nF();";
        var page = await (await AnalyserForAsync(script)).GetDocPageAsync(script.IndexOf("F();") + 1);

        page!.Summary.Should().NotContain(r => r.Kind == DocRunKind.Link);
        page.Summary.ToPlainText().Should().Contain("Text");
    }

    private static async Task<ScriptAnalyser> AnalyserForAsync(string script)
    {
        var context = (await ScriptContext.CreateAsync()).ApplyScript(script);
        return new ScriptAnalyser(context);
    }

    /// <summary>Asks for a page one character into <paramref name="marker"/>.</summary>
    private static async Task<DocPage?> GetPageAsync(string script, string marker)
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        return await new ScriptAnalyser(context).GetDocPageAsync(script.IndexOf(marker) + 1);
    }
}
