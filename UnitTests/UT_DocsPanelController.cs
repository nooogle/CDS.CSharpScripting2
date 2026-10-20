using AwesomeAssertions;
using CDS.CSharpScript2.Documentation;

namespace UnitTests;

[TestClass]
public class UT_DocsPanelController
{
    [TestMethod]
    public async Task ShowAt_FromClosed_EntersFollowing()
    {
        var (source, controller) = Create();
        var task = controller.ShowAtAsync(5);
        source.Complete(0, Page("A"));
        await task;

        controller.State.Should().Be(DocsPanelState.Following);
        controller.CurrentPage!.Title.Should().Be("A");
        source.Requests[0].Position.Should().Be(5);
    }

    [TestMethod]
    public async Task ShowAt_FromClosed_IsFollowingAndLoadingBeforeTheSourceCompletes()
    {
        var (source, controller) = Create();
        var raised = 0;
        controller.Changed += (_, _) => raised++;

        var task = controller.ShowAtAsync(5);

        controller.State.Should().Be(DocsPanelState.Following);
        controller.IsLoading.Should().BeTrue();
        raised.Should().Be(1);

        source.Complete(0, Page("A"));
        await task;

        controller.IsLoading.Should().BeFalse();
        controller.CurrentPage!.Title.Should().Be("A");
        raised.Should().Be(2);
    }

    [TestMethod]
    public async Task ShowAt_WhenSourceReturnsNull_StopsLoading()
    {
        var (source, controller) = Create();
        var task = controller.ShowAtAsync(5);
        source.Complete(0, null);
        await task;

        controller.IsLoading.Should().BeFalse();
        controller.CurrentPage.Should().BeNull();
    }

    [TestMethod]
    public async Task CaretMoved_DuringFirstShow_IsNotLost()
    {
        var (source, controller) = Create();
        var show = controller.ShowAtAsync(5);

        var move = controller.CaretMovedAsync(9);
        source.Requests[1].Position.Should().Be(9);
        source.Complete(1, Page("B"));
        await move;
        source.Complete(0, Page("A"));
        await show;

        controller.IsLoading.Should().BeFalse();
        controller.CurrentPage!.Title.Should().Be("B");
    }

    [TestMethod]
    public async Task Close_WhileLoading_StopsLoading()
    {
        var (source, controller) = Create();
        var task = controller.ShowAtAsync(5);

        controller.Close();
        source.Complete(0, Page("A"));
        await task;

        controller.IsLoading.Should().BeFalse();
        controller.CurrentPage.Should().BeNull();
        controller.State.Should().Be(DocsPanelState.Closed);
    }

    [TestMethod]
    public async Task ShowAt_OnNothing_FromClosed_HasNoPage()
    {
        var (source, controller) = Create();
        var task = controller.ShowAtAsync(5);
        source.Complete(0, null);
        await task;

        controller.State.Should().Be(DocsPanelState.Following);
        controller.CurrentPage.Should().BeNull();
    }

    [TestMethod]
    public async Task CaretMoved_WhileFollowing_UpdatesPage()
    {
        var (source, controller) = await FollowingAsync("A");
        var task = controller.CaretMovedAsync(9);
        source.Complete(1, Page("B"));
        await task;

        controller.CurrentPage!.Title.Should().Be("B");
    }

    [TestMethod]
    public async Task CaretMoved_OnNothing_KeepsPage()
    {
        var (source, controller) = await FollowingAsync("A");
        var task = controller.CaretMovedAsync(9);
        source.Complete(1, null);
        await task;

        controller.CurrentPage!.Title.Should().Be("A");
    }

    [TestMethod]
    public async Task CaretMoved_WithinSameSymbol_RaisesNothing()
    {
        var (source, controller) = await FollowingAsync("A");
        var raised = 0;
        controller.Changed += (_, _) => raised++;
        var task = controller.CaretMovedAsync(9);
        source.Complete(1, Page("A"));
        await task;

        raised.Should().Be(0);
    }

    [TestMethod]
    public async Task Navigate_EntersBrowsing_AndIgnoresCaret()
    {
        var (source, controller) = await FollowingAsync("A");
        var navigate = controller.NavigateAsync(new DocTarget("T:B"));
        source.Complete(1, Page("B"));
        await navigate;

        controller.State.Should().Be(DocsPanelState.Browsing);
        controller.CurrentPage!.Title.Should().Be("B");

        await controller.CaretMovedAsync(20);
        source.Requests.Should().HaveCount(2);
        controller.CurrentPage!.Title.Should().Be("B");
    }

    [TestMethod]
    public async Task BackAndForward_StepThroughHistory()
    {
        var (source, controller) = await FollowingAsync("A");
        await NavigateAsync(source, controller, "B");
        await NavigateAsync(source, controller, "C");
        controller.CanGoBack.Should().BeTrue();
        controller.CanGoForward.Should().BeFalse();

        var back = controller.BackAsync();
        source.Complete(3, Page("B"));
        await back;
        controller.CurrentPage!.Title.Should().Be("B");
        controller.CanGoForward.Should().BeTrue();

        var forward = controller.ForwardAsync();
        source.Complete(4, Page("C"));
        await forward;
        controller.CurrentPage!.Title.Should().Be("C");
        controller.CanGoForward.Should().BeFalse();
    }

    [TestMethod]
    public async Task BackToFirstPage_StaysBrowsing()
    {
        var (source, controller) = await FollowingAsync("A");
        await NavigateAsync(source, controller, "B");

        var back = controller.BackAsync();
        source.Complete(2, Page("A"));
        await back;

        controller.State.Should().Be(DocsPanelState.Browsing);
        controller.CurrentPage!.Title.Should().Be("A");
        controller.CanGoBack.Should().BeFalse();
        controller.CanGoForward.Should().BeTrue();
    }

    [TestMethod]
    public async Task ShowAt_WhileBrowsing_ReturnsToFollowing()
    {
        var (source, controller) = await FollowingAsync("A");
        await NavigateAsync(source, controller, "B");

        var task = controller.ShowAtAsync(1);
        source.Complete(2, Page("C"));
        await task;

        controller.State.Should().Be(DocsPanelState.Following);
        controller.CurrentPage!.Title.Should().Be("C");
    }

    [TestMethod]
    public async Task Back_ToDeletedTarget_ShowsUnavailable()
    {
        var (source, controller) = await FollowingAsync("A");
        await NavigateAsync(source, controller, "B");
        await NavigateAsync(source, controller, "C");

        var back = controller.BackAsync();
        source.Complete(3, DocPage.Unavailable(new DocTarget("T:B")));
        await back;

        controller.CurrentPage!.IsUnavailable.Should().BeTrue();
        source.Requests[3].Target.Should().Be(new DocTarget("T:B"));
    }

    [TestMethod]
    public async Task Close_ClearsHistory()
    {
        var (source, controller) = await FollowingAsync("A");
        await NavigateAsync(source, controller, "B");

        controller.Close();

        controller.State.Should().Be(DocsPanelState.Closed);
        controller.CurrentPage.Should().BeNull();
        controller.CanGoBack.Should().BeFalse();
        controller.CanGoForward.Should().BeFalse();
    }

    [TestMethod]
    public async Task SlowOlderRequest_DoesNotOverwriteNewer()
    {
        var (source, controller) = Create();
        var first = controller.ShowAtAsync(1);
        var second = controller.ShowAtAsync(2);

        source.Requests[0].Token.IsCancellationRequested.Should().BeTrue();
        source.Complete(1, Page("Second"));
        await second;
        source.Complete(0, Page("First"));
        await first;

        controller.CurrentPage!.Title.Should().Be("Second");
    }

    [TestMethod]
    public async Task CancelledSource_ThrowingOperationCanceled_DoesNotFault()
    {
        var (source, controller) = Create();
        var first = controller.ShowAtAsync(1);
        var second = controller.ShowAtAsync(2);

        source.Fail(0, new OperationCanceledException(source.Requests[0].Token));
        source.Complete(1, Page("Second"));
        await first;
        await second;

        controller.CurrentPage!.Title.Should().Be("Second");
    }

    [TestMethod]
    public async Task SourceFailure_KeepsPage()
    {
        var (source, controller) = await FollowingAsync("A");
        var task = controller.CaretMovedAsync(3);
        source.Fail(1, new InvalidOperationException("boom"));
        await task;

        controller.State.Should().Be(DocsPanelState.Following);
        controller.CurrentPage!.Title.Should().Be("A");
    }

    [TestMethod]
    public async Task ShowAt_WhileBrowsing_KeepsHistory()
    {
        var (source, controller) = await FollowingAsync("A");
        await NavigateAsync(source, controller, "B");

        var show = controller.ShowAtAsync(1);
        source.Complete(2, Page("C"));
        await show;

        controller.State.Should().Be(DocsPanelState.Following);
        controller.CanGoBack.Should().BeTrue();
        controller.CanGoForward.Should().BeFalse();

        var back = controller.BackAsync();
        source.Complete(3, Page("B"));
        await back;

        controller.State.Should().Be(DocsPanelState.Browsing);
        controller.CurrentPage!.Title.Should().Be("B");
    }

    [TestMethod]
    public async Task CaretMoved_WhileLinkLoading_IsIgnored()
    {
        var (source, controller) = await FollowingAsync("A");
        var navigate = controller.NavigateAsync(new DocTarget("T:B"));

        controller.State.Should().Be(DocsPanelState.Browsing);
        await controller.CaretMovedAsync(7);
        source.Requests.Should().HaveCount(2);
        source.Requests[1].Token.IsCancellationRequested.Should().BeFalse();

        source.Complete(1, Page("B"));
        await navigate;
        controller.CurrentPage!.Title.Should().Be("B");
    }

    private static async Task NavigateAsync(FakeSource source, DocsPanelController controller, string name)
    {
        var task = controller.NavigateAsync(new DocTarget("T:" + name));
        source.Complete(source.Requests.Count - 1, Page(name));
        await task;
    }

    private static async Task<(FakeSource, DocsPanelController)> FollowingAsync(string name)
    {
        var (source, controller) = Create();
        var task = controller.ShowAtAsync(0);
        source.Complete(0, Page(name));
        await task;
        return (source, controller);
    }

    private static (FakeSource, DocsPanelController) Create()
    {
        var source = new FakeSource();
        return (source, new DocsPanelController(source));
    }

    private static DocPage Page(string name)
        => new() { Target = new DocTarget("T:" + name), Title = name, Signature = "class " + name };

    private sealed class FakeSource : IDocPageSource
    {
        public List<Request> Requests { get; } = [];

        public Task<DocPage?> GetDocPageAsync(int position, CancellationToken cancellationToken)
            => Add(new Request { Position = position, Token = cancellationToken });

        public Task<DocPage?> GetDocPageAsync(DocTarget target, CancellationToken cancellationToken)
            => Add(new Request { Target = target, Token = cancellationToken });

        public void Complete(int index, DocPage? page) => Requests[index].Completion.SetResult(page);

        public void Fail(int index, Exception exception) => Requests[index].Completion.SetException(exception);

        private Task<DocPage?> Add(Request request)
        {
            Requests.Add(request);
            return request.Completion.Task;
        }
    }

    private sealed class Request
    {
        // set, not init: net48 has no IsExternalInit.
        public int? Position { get; set; }

        public DocTarget? Target { get; set; }

        public CancellationToken Token { get; set; }

        public TaskCompletionSource<DocPage?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
