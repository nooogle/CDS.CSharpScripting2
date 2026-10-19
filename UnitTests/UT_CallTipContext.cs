using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.APIInfo;

namespace UnitTests;

[TestClass]
public class UT_CallTipContext
{
    [TestMethod]
    public async Task GetCallTipContextAsync_JustAfterOpenParen_IsFirstArgument()
    {
        var context = await GetContextAtEndAsync("Math.Pow(");

        context.Should().Be(new CallTipContext { ArgumentIndex = 0, OpenParenPosition = 8 });
    }

    [TestMethod]
    public async Task GetCallTipContextAsync_AfterComma_IsNextArgument()
    {
        var context = await GetContextAtEndAsync("Math.Pow(2, ");

        context.Should().Be(new CallTipContext { ArgumentIndex = 1, OpenParenPosition = 8 });
    }

    [TestMethod]
    public async Task GetCallTipContextAsync_InsideUnclosedNestedCall_IsInnerCall()
    {
        var context = await GetContextAtEndAsync("Math.Pow(Math.Abs(1");

        context.Should().Be(new CallTipContext { ArgumentIndex = 0, OpenParenPosition = 17 });
    }

    [TestMethod]
    public async Task GetCallTipContextAsync_AfterClosedNestedCall_IsOuterCall()
    {
        var context = await GetContextAtEndAsync("Math.Pow(Math.Abs(1)");

        context.Should().Be(new CallTipContext { ArgumentIndex = 0, OpenParenPosition = 8 },
            "the caret has left Abs's argument list once its closing parenthesis is typed");
    }

    [TestMethod]
    public async Task GetCallTipContextAsync_JustBeforeCloseParen_IsInsideCall()
    {
        const string script = "Math.Pow(2, 3);";
        var context = await GetContextAsync(script, script.IndexOf(')'));

        context.Should().Be(new CallTipContext { ArgumentIndex = 1, OpenParenPosition = 8 });
    }

    [TestMethod]
    public async Task GetCallTipContextAsync_AfterClosedCall_IsNull()
    {
        const string script = "Math.Pow(2, 3);";
        var context = await GetContextAsync(script, script.IndexOf(')') + 1);

        context.Should().BeNull();
    }

    private static Task<CallTipContext?> GetContextAtEndAsync(string script)
    {
        return GetContextAsync(script, script.Length);
    }

    private static async Task<CallTipContext?> GetContextAsync(string script, int position)
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        return await new ScriptAnalyser(context).GetCallTipContextAsync(position);
    }
}
