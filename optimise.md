# Editor responsiveness — findings and design direction

Status: **steps 0–3 and 5 implemented; step 4 measured and dropped.** Worst-case typing
hitch down ~4× (337 ms → 91 ms); colour latency down ~7× (551 ms → 77 ms). Written
2026-08-08. See §10 for progress, §11 for the step 4/5 decisions, §9 for a parked
investigation.

Trigger: typing in the Scintilla editor hosted by CDS.OpenCvSharpPlayground feels
noticeably laggier than the same control in this solution's WinForms sample.

> **On references to the Playground.** CDS.OpenCvSharpPlayground is a **separate
> application in its own repository** that consumes these packages. This library must
> never reference it, know about it, or depend on anything in it — and does not: there is
> no mention of it in any `.cs`, `.csproj`, `.slnx` or `.props` here. It appears in this
> document only as the place a symptom was observed and as a realistic example of a rich
> host environment. Any citation of a file below marked *(Playground repo)* lives in that
> other repository, not this one. The same rule applies to anything this work produces:
> tests and fixtures added here must build from this repository alone.

---

## 1. What we are actually building

A embeddable C# script editor. The user types, and expects:

| Feedback | Budget | Why |
|---|---|---|
| Keystroke → glyph on screen | **< 16 ms** | One frame. Miss this and it feels broken, full stop. |
| Keystroke → syntax colour | < 50 ms | Colour that trails the caret reads as instability. |
| Completion trigger → list visible | < 100 ms | The user is stopped, waiting. Past ~200 ms they start typing over it. |
| Edit → squiggles | 500 ms – 1 s | Nobody wants errors while mid-identifier. Debounce is *desirable* here. |

The important thing about that table is that the budgets differ by **two orders of
magnitude**. Any design that computes all of it in one pass is forced to serve the
tightest budget with the slowest work. That is the shape of our current problem.

Constraint worth stating up front, because it rules out one popular answer: scripts
are compiled against **live host types** (`ScriptEnvironment.WithGlobalType<T>()`,
`WithAdditionalReferenceForType<T>()`). The host hands us `Type` objects from its own
running AppDomain. That is an in-process feature by construction.

### Compatibility policy for this work

**There are no external consumers yet.** Breaking changes are acceptable and should be
signalled with a version bump rather than worked around. Concretely, for the rest of this
document:

- Prefer the *right* API shape over the binary-compatible one. Renaming, changing
  signatures, and removing members are all on the table.
- The `CLAUDE.md` guidance to prefer overloads over defaulted parameters exists for
  binary compatibility; it is not binding here. Follow it where it also reads better,
  ignore it where it only adds surface.
- Step 2 (§10) was implemented under the old assumption and added paired overloads
  throughout `EditorManager` and `ScriptAnalyser`. Those pairs can now be collapsed into
  single methods with defaulted tokens. Worth doing as a tidy-up — not urgent, and best
  done as its own commit so it does not obscure a behavioural change.
- Revisit this the moment anything ships to a consumer outside this solution.

---

## 2. Measured findings

Method: purpose-built probes (Release, .NET 10, this machine, single run) driving
`EditorManager` under a real WinForms message pump. UI-thread blocking measured with a
10 ms heartbeat timer — any gap > ~10 ms means the pump was starved, which is exactly
"keypress to character on screen". Scripts were synthetic but representative in size.

Treat the numbers as order-of-magnitude, not benchmarks. The *ratios* are the point.

### 2.1 All Roslyn work runs on the UI thread — root cause

`EditorManager` uses `ConfigureAwait(false)` throughout, and `ShowCompletionAsync`
documents the intent explicitly: *"Runs on the UI thread throughout."*

But `ConfigureAwait(false)` only controls where a continuation **resumes**. It does
nothing when the awaited operation never yields — and these never do:

- `compilation.GetDiagnostics()` (`ScriptAnalyser.cs:42`) is a synchronous Roslyn call.
- `Classifier.GetClassifiedSpansAsync` and `CompletionService.GetCompletionsAsync`
  complete synchronously in this workspace configuration.

So the work runs inline on the caller's thread. `ScriptContext.CreateAsync` is the only
place in the codebase that calls `Task.Run`.

Measured pump stalls per analysis pass, and the effect of simply wrapping the pass in
`Task.Run`:

```
 200 lines · current   : worst stall 130.9 ms   (5 stalls >40 ms / 5 passes)
 200 lines · offloaded : worst stall  30.0 ms   (0 stalls >40 ms / 5 passes)
 500 lines · current   : worst stall  94.4 ms   (5 stalls >40 ms / 5 passes)
 500 lines · offloaded : worst stall  39.1 ms   (0 stalls >40 ms / 5 passes)
```

### 2.2 A single `#r` adds ~70 ms to every pass

`DocumentedMetadataReferenceResolver.WithDocumentation` builds a **fresh**
`MetadataReference` on every resolve, with no caching:

```
without #r          : [26, 30, 21, 29, 18, 20, 18, 18] ms
with    #r          : [76, 86, 84, 102, 92, 98, 92, 114] ms
with #r OpenCvSharp : [91, 82, 121, 97, 88, 84, 92, 87] ms
```

The `CreateFromFile` call itself costs only **0.71 ms**. The damage looked like identity: each
new instance is not `Equals` to the previous one, so Roslyn cannot reuse the previous
compilation's reference binding and **rebinds the whole compilation every pass**.

```
ReferenceEquals(a, b) : False
a.Equals(b)           : False
same GetMetadata()    : False
```

> **Partly corrected by implementation (§10).** The original conclusion here — "caching
> should recover essentially all of it" — was **wrong**. Caching recovers about a
> quarter. The rest is Roslyn rebuilding its reference manager whenever the syntax tree
> contains `#r` at all, which cannot be fixed from outside the compiler. Parked in §9.

### 2.3 Cost scales with script size, not with the environment

This was the surprise, and it explains the Playground-vs-sample gap better than the
reference set does.

| `ApplyScript` (diagnostics + classification) | 40 lines | 200 lines | 500 lines |
|---|---|---|---|
| Light env (`ScriptEnvironment.Default`) | 11–26 ms | 70–131 ms | 64–116 ms |
| Heavy env (OpenCvSharp + WinForms + Drawing + globals) | 13–24 ms | 37–89 ms | 44–91 ms |

The heavy environment costs mainly on **cold start** and on completion **list size**
(369 vs 227 items) — not on steady-state analysis.

The sample demos use ~300-character one-liners
(`Console.WriteLine("Hello world, from the script!")`). Playground scripts are real
work. Add two live editors (`MainForm.cs:76-77`, *Playground repo*), each with its own
workspace and its own 500 ms timer, and any `#r` usage from §2.2, and the gap is fully
accounted for.

### 2.4 Secondary costs

- **Whole-document restyle every pass.** `ApplyClassificationsToEditor` resets and
  re-applies every span: ~15 ms at 500 lines / 9,400 spans, on the UI thread.
- **Redundant document re-fork per completion.** `UpdateScriptDocumentAsync` calls
  `Document.WithText(...)` unconditionally, even when the text is unchanged. Measured
  as *marginal* in steady state — only the first request benefits from a guard. Cheap
  to fix, but not a fix for this problem.
- **Repeated `scintilla.Text` reads** — 7+ per pass, plus one per diagnostic inside
  `MarkDiagnosticInEditor`. Measured at 0.01 ms. Tidy-up only. Recorded so nobody
  spends a day on it.

### 2.5 Not the problem

**Logging.** One `Debug.WriteLine`, in a catch block, at `CodeCompletion/Manager.cs:66`.
That is the entire logging surface in the hot path.

---

## 3. How the current design works

By inspection, not measurement:

```
CharAdded ──> HandleTextChanged ──> ResetAnalysisState   (clears squiggles, hides tooltip)
    │                          └──> restart 500 ms WinForms timer
    │
    ├─ '.'            ──> StartCompletionSession(immediate)
    ├─ letter / '_'   ──> StartCompletionSession(150 ms debounce)
    └─ '(' ',' ')'    ──> call-tip session

timer Tick ──> PerformLiveAnalysisAsync
                 ├─ EditorManager.ApplyScript(wholeText)
                 │     ├─ ScriptContext.ApplyScript  → Document.WithText(SourceText.From(all))
                 │     ├─ GetDiagnosticsAsync        → compilation.GetDiagnostics()
                 │     └─ GetClassificationsAsync    → Classifier.GetClassifiedSpansAsync(whole doc)
                 ├─ ApplyDiagnosticsToEditor         (whole doc)
                 └─ ApplyClassificationsToEditor     (whole doc)
```

Four structural observations, independent of performance:

**(a) Change notification is wired to a typing event, not a text-change event.**
The control subscribes to `CharAdded` and `Delete`. `CharAdded` is Scintilla's
SCN_CHARADDED — it fires for typed characters only. `Insert` is **not** subscribed
(confirmed in `ScintillaScriptEditor.Designer.cs`; `Insert` exists on the control with
a `ModificationEventArgs` carrying `Position`, `Text`, `LinesAdded`, `Source`).

Consequence: **a paste at the caret with no selection fires `Insert` only, so
`HandleTextChanged` never runs and the timer is never restarted** — analysis goes stale
until the next keystroke.

> **Confirmed and fixed.** Reproduced interactively by the author, then reproduced
> automatically (§10). Resolved 2026-08-09.

**(b) Cancellation exists in name only.** `_completionCts` and `_dwellCts` are created
and cancelled — but `EditorManager.ApplyScript`, `GetAutoCompletions`, `GetAPIInfo` and
`GetCallTipContext` take **no `CancellationToken` at all**. `GetDiagnosticsAsync(ct)`
and `GetClassificationsAsync(ct)` accept one and are called with none
(`EditorManager.cs:54-55`). So superseded work runs to completion and its result is
thrown away afterwards. Nothing is actually cancelled. This also contradicts the repo's
own convention in `CLAUDE.md`.

**(c) Staleness is detected by comparing whole document strings.** `_lastScript !=
script`, plus an `_editorStateVersion` counter, plus `ReferenceEquals(manager,
_manager)` — three overlapping mechanisms approximating one missing concept: a
document version stamp.

**(d) Fast and slow work are fused.** Syntactic colouring (cheap, wants every
keystroke) is computed in the same pass as semantic diagnostics (expensive, wants
debouncing). The pass runs at the *slow* cadence, so colour waits for diagnostics.

---

## 4. How other people solve this

**Roslyn inside Visual Studio.** The relevant precedent, because it is the same engine.
Two design choices matter here:

- *Tiered classification.* VS splits **syntactic** classification (lexer/parser only,
  fast, runs eagerly) from **semantic** classification (needs compilation, runs on a
  background thread and arrives later as a tag update). Text gets coloured immediately
  and is *refined* when semantics land. This is why VS colours as you type even in a
  huge solution.
- *Everything compute-bound leaves the UI thread.* The existence of
  `Microsoft.VisualStudio.Threading` / `JoinableTaskFactory` is itself evidence of how
  seriously this is taken — an entire library to manage the UI-thread boundary.

Roslyn's own data model is built for this: `Solution`/`Document` are **immutable
snapshots**, explicitly safe to analyse from any thread. Analysing off the UI thread is
not a workaround; it is the intended usage.

**LSP (VS Code, and Roslyn's own `Microsoft.CodeAnalysis.LanguageServer`).** The editor
is a text view; the language service is a separate process over JSON-RPC. Every request
carries a **document version**; stale responses are discarded; requests are cancellable.
The strongest possible form of the same idea — hard process isolation means the language
service physically cannot stall the editor.

**RoslynPad.** The closest open-source analogue to this project — embedded Roslyn +
AvalonEdit. Wraps a workspace in a `RoslynHost`, runs analysis async off the UI thread,
pushes diagnostics back via events.

**The common pattern, across all three:**

1. Text buffer is the single source of truth, with a monotonic version.
2. Every analysis request is tagged with the version it was computed against; stale
   results are dropped, never applied.
3. Requests are cancellable, and cancellation reaches the analysis engine.
4. Work is tiered by latency budget, not run as one pass.
5. Results are applied incrementally, scoped to what is visible.

---

## 5. Clean-slate design

What this would look like built fresh, given the constraint from §1 that it must stay
in-process.

### 5.1 One text-change funnel

Subscribe to `Insert` + `Delete` (the actual modification events). Each carries
`Position` and `Text` — exactly enough to build a Roslyn `TextChange`. Maintain:

```
long _documentVersion;   // bumped once per modification
```

Two benefits beyond correctness. First, paste and undo/redo stop being special cases.
Second, we can feed Roslyn `SourceText.WithChanges(textChange)` instead of
`SourceText.From(entireDocument)`, which lets its **incremental parser** reuse the
unchanged syntax tree rather than making it diff two 20 KB strings to rediscover the
edit we already knew about.

### 5.2 Tier the work by budget

| Tier | Work | Thread | Cadence |
|---|---|---|---|
| 0 | Brace match, indent, caret | UI | synchronous |
| 1 | **Syntactic** classification | UI or short hop | every change |
| 2 | **Semantic** classification + diagnostics | background | debounced ~300–500 ms |
| 3 | Completion, signature help, hover | background | on demand, cancellable |

Tier 1 is the change that makes typing *feel* fixed rather than merely *be* faster.
Colour would stop waiting on diagnostics.

### 5.3 A serialised analysis pump, not free-threaded `Task.Run`

One worker per editor, processing the latest request and cancelling superseded ones.
Roslyn compilations are memory-heavy; with two live editors, naive `Task.Run` per
keystroke risks concurrent compilations and doubled peak memory. Serialising also makes
"latest wins" trivial to reason about.

### 5.4 Version-stamped results

```
record AnalysisResult(long Version, ImmutableArray<Diagnostic> Diagnostics, ...);
```

Apply only if `result.Version == _documentVersion`. This replaces `_lastScript` string
comparison, `_editorStateVersion`, and the `ReferenceEquals(manager, _manager)` checks
with one rule.

### 5.5 Real cancellation

`CancellationToken` on every `EditorManager` / `ScriptAnalyser` method, threaded into
Roslyn. Then cancelling a superseded pass actually stops it, instead of letting it run
to completion and discarding the result.

### 5.6 Incremental, viewport-scoped rendering

Classify and style the visible span plus a margin; diff against what is already applied.
`ScriptAnalyser` already has a `GetClassificationsAsync(spanStart, spanLength, ct)`
overload — it is simply never used by the editor.

### 5.7 Process-wide metadata cache

Cache `MetadataReference` by (path, timestamp), shared across editors. Fixes §2.2 by
construction, and stops two editors each holding a private copy of OpenCvSharp's
metadata. This is what VS does via its metadata/documentation provider services.

### 5.8 Explicitly rejected: out-of-process / LSP

It is the textbook modern answer, and it is wrong here. The globals feature hands live
`Type` objects from the host's AppDomain to the compiler — that cannot cross a process
boundary without a serialisation and deployment story far larger than the problem. We
also ship as a NuGet package into someone else's WinForms app; spawning a sidecar
process is a hostile thing to do to a host application.

**Take the LSP discipline — versioned requests, real cancellation, async boundaries —
without the LSP transport.**

---

## 6. Is offloading a fix for bad design, or good design?

The question worth answering before writing code.

**It is good design in itself.** Roslyn's immutable snapshot model exists precisely so
analysis can run off the UI thread; every serious consumer does it; and it is the only
way to meet a 16 ms keystroke budget with work that takes 50–100 ms. It is not a
workaround.

**But on its own it is insufficient, and slightly dangerous.** Offloading fixes the
*symptom* — the stall — while leaving the structural issues: no versioning, no real
cancellation, fast and slow work still fused, whole-document rendering. Worse, moving
uncancellable work to a background thread means superseded passes now burn a core in
the background instead of visibly blocking, which is harder to notice and harder to
diagnose. Offloading without cancellation trades a visible problem for an invisible one.

So: **do it, but do it together with cancellation and version stamping.** Those three
are one change, not three.

Per-item verdicts:

| Change | Verdict |
|---|---|
| Offload analysis / completion (§2.1) | Good design. Do it — with §5.4 and §5.5. |
| Cache `MetadataReference` (§2.2, §5.7) | Straight bug fix. **Done (§10)** — worth ~25% on `#r` scripts, not the ~100% first predicted. |
| Real cancellation (§5.5) | Good design, and a precondition for offloading. |
| Version stamping (§5.4) | Good design; deletes three ad-hoc mechanisms. |
| Tier 1/2 split (§5.2) | Good design. The change users would actually *feel*. |
| `Insert` event (§5.1, §3a) | Bug fix (stale analysis after paste). |
| Incremental `TextChange` (§5.1) | Good design; unlocks Roslyn's incremental parser. |
| Viewport-scoped styling (§5.6) | Good design, but second-order. Defer. |
| Guard redundant re-fork (§2.4) | Tidy-up. Measured marginal. Low priority. |
| Fewer `scintilla.Text` reads (§2.4) | Cosmetic. Measured at 0.01 ms. Lowest priority. |

---

## 7. Proposed sequence

Each step is independently shippable and independently reviewable.

0. ~~**Move change notification to `Insert` + `Delete`.**~~ **Done** — see §10. Pulled
   ahead of the rest because it is a correctness bug, not a performance one, and it is
   independent of the threading work.
1. ~~**Metadata reference cache.**~~ **Done** — see §10. Smaller win than expected;
   the `#r` penalty is mostly inside Roslyn, not in our resolver.
2. ~~**Thread `CancellationToken` through `EditorManager` and `ScriptAnalyser`.**~~
   **Done** — see §10.
3. ~~**Offload the analysis pass + completion, with version stamping.**~~ **Done** —
   see §10.
4. ~~**Feed Roslyn a `TextChange`** rather than the whole document.~~ **Dropped** —
   measured at ~5%, and the failure mode is silent document drift. See §11.
5. **Split syntactic from semantic classification** (tier 1 vs tier 2). See §11 for the
   measurement and §10 for what landed.
6. *(Optional, later)* Viewport-scoped styling; redundant-fork guard; `Text` read
   reduction.

---

## 11. Measured decisions on steps 4 and 5

Both steps were measured before being built. One survived, one did not.

### Step 4 — feed Roslyn a `TextChange`: **dropped**

§5.1 argued this would unlock incremental parsing. Measured, interleaving the two modes and
repeating so JIT and GC state hit both equally:

| script | whole document | explicit `TextChange` | difference |
|---|---|---|---|
| 200 lines | 22.2 ms | 18.3 ms | 3.9 ms (17.7%, wide spread) |
| 500 lines | 31.9 ms | 30.2 ms | **1.7 ms (5.4%)** |
| 1500 lines | 144.4 ms | 137.5 ms | **6.9 ms (4.8%)** |

The 500 and 1500 line rows had tight spreads and are the reliable ones. The text step
itself measured identically either way (~0.14 ms), so Roslyn's own `GetChangeRanges` diff
already recovers the edit well — which is unsurprising, it is built for this.

**Verdict: not worth it.** ~5% of an operation that now runs off the UI thread anyway, set
against a failure mode where accumulated changes drift from the real document and Roslyn
silently analyses text the user is not looking at — wrong squiggle positions, wrong
completions. A correctness risk bought with a rounding error. The other half of §5.1's
argument (paste and undo stop being special cases) was already delivered by step 0.

### Step 5 — tier 1 vs tier 2 classification: **built**

| script | tier 1 (syntax only) | tier 2 (Roslyn `Classifier`) | ratio | spans |
|---|---|---|---|---|
| 200 lines | 3.6 ms | 56.7 ms | **15.8×** | 2551 vs 2685 |
| 500 lines | 9.2 ms | 71.3 ms | **7.7×** | 6351 vs 6685 |
| 1500 lines | 25.5 ms | 86.9 ms | **3.4×** | 19005 vs 20005 |

~95% of the spans for a fraction of the cost. The missing 5% is exactly what needs symbol
resolution — class names, method names, parameters, locals.

**One thing §5.2 did not anticipate: Roslyn exposes no public syntactic-only classifier.**
`Classifier` has two public methods and both need a `Document` or a `SemanticModel`;
`ISyntaxClassificationService` and everything under it is internal. Reflecting into
internals would be a maintenance liability in a shipped package, so the walk is written by
hand — see `Classification/SyntacticClassifier.cs`.

Steps 1–3 should address the reported symptom. Steps 4–5 are what make it feel genuinely
good rather than merely acceptable.

---

## 8. Open questions

- **Does `UT_EditorExecutionParity` still hold** once analysis moves off the UI thread?
  Per `CLAUDE.md` this suite guards the two-compilation-path divergence; threading
  changes should not affect it, but it is the canary.
- **Should the analysis pump be per-editor or per-process?** Per-editor is simpler;
  per-process bounds total memory better with many editors. A known host runs two.
- **Is 500 ms the right debounce** once the tier split lands? With colour on tier 1,
  diagnostics could probably relax further without anyone noticing.
- **Reproduce with a realistic hand-written script** rather than generated text, to
  confirm the §2.3 size scaling holds for real code. Add the fixture to this repository;
  do not reach into a consuming application for it.

---

## 9. Parked for investigation — the residual `#r` cost

Surfaced while implementing step 1 (§10). **Not yet investigated; recorded so it is not
lost.**

**Observation.** A script containing `#r` costs ~92 ms per analysis pass against ~21 ms
for the same script without it. The reference cache recovers ~24 ms. The remaining
**~47 ms per pass is inside Roslyn** and is not affected by anything the resolver
returns — including returning the identical instance every time.

**Hypothesis, not verified.** Roslyn appears to refuse to reuse a compilation's reference
manager when a syntax tree contains reference or load directives — plausibly a check of
the form `!oldTree.HasReferenceOrLoadDirectives() && !newTree.HasReferenceOrLoadDirectives()`
in `CSharpCompilation.ReplaceSyntaxTree`. That would mean the binding is rebuilt on every
keystroke pass for any script using `#r`, *regardless of whether the directives changed*.
This is consistent with every measurement taken so far but has **not** been confirmed
against Roslyn's source.

**Worth checking:**

- Confirm the mechanism against the Roslyn source for the pinned version
  (`Microsoft.CodeAnalysis.CSharp` 5.6.0). If it is unconditional, no external caching
  will ever help.
- Does `#load` behave the same way? `ScriptEnvironment` configures a `SourceResolver`
  alongside the metadata resolver, so `#load`-using scripts may carry the same penalty.
- Does the cost scale with the *number* of references in the environment, or with the
  number of `#r` directives? The globals variants suggest the former (87–92 ms with a
  globals type versus 32–35 ms without), which would mean any host with a rich
  environment amplifies it.

**Fixtures.** The throwaway probes for this used an assembly from a consuming
application's output folder, purely because one was to hand. Anything that becomes a real
test here must emit or ship its own fixture assembly — see the invalidation check in §10,
which compiles one in a temp directory at run time and is the pattern to follow.

**Possible mitigation, if it proves worth it.** Pre-scan the script for `#r` directives,
resolve them once, and hoist the targets into `ScriptEnvironment.References` so they
become ordinary references and the directive leaves the syntax tree. Re-hoist only when
the set of directives changes. This is a design change rather than tuning, and it carries
real risk: the editor and execution paths must continue to accept identical directives,
which is exactly what `UT_EditorExecutionParity` guards. Do not attempt it without
extending that suite first.

**Priority.** Low until someone actually complains about `#r` scripts specifically. The
UI-thread work (§5.2, §5.3) dominates for ordinary scripts and should land first.

---

## 10. Progress

### Step 0 — change notification moved to `Insert` + `Delete` (2026-08-09)

Files: `ScintillaScriptEditor.cs`, `ScintillaScriptEditor.Designer.cs`.

- Subscribed `scintilla.Insert`; routed it and `Delete` to `HandleTextChanged`. These
  are now the single point at which the editor learns the document changed.
- Removed the `HandleTextChanged()` call from `scintilla_CharAdded`. That handler keeps
  only the genuinely typing-specific behaviour — auto-indent on newline, and the
  completion / call-tip triggers on `.`, `(`, `,`, `)`.
- Added `_suppressTextChangeHandling`, set across the bulk edits in
  `CommentSelectedLines` / `UncommentSelectedLines`. Without it those raise one `Insert`
  per selected line; the existing single trailing `HandleTextChanged()` call now covers
  the whole block, as it did before.

Verified with a probe hosting the real control, asserting that an introduced error is
actually picked up after each kind of edit:

| Case | Before | After |
|---|---|---|
| Paste at caret, no selection | **FAIL** | PASS |
| Typed characters (SendKeys) | PASS | PASS |
| Programmatic `InsertText` | **FAIL** | PASS |
| Undo of a deletion | PASS | PASS |
| Comment block (bulk-edit guard) | PASS | PASS |

Full solution builds; `UnitTests` 77/77 pass on net10.0 and net48. Note the suite does
not reference the Scintilla control, so it does not cover this change — hence the probe.

Two notes for whoever picks up the next step:

- The probe initially reported the paste case as passing on the *unfixed* code. Cause:
  the first analysis pass creates the workspace and takes ~1.4 s, and the timer restart
  in `PerformLiveAnalysisAsync`'s `finally` masked the missing notification. A warm-up
  pass before the first assertion fixed it. Any future test of this area needs the same
  warm-up, or it will quietly pass for the wrong reason.
- `_suppressTextChangeHandling` is a stopgap. Once version stamping (§5.4) lands, bulk
  edits should coalesce naturally — one version bump per logical change — and the flag
  should be deleted rather than carried forward.

### Step 1 — metadata reference cache (2026-08-09)

File: `DocumentedMetadataReferenceResolver.cs`.

Process-wide `ConcurrentDictionary` keyed by (path, `MetadataReferenceProperties`),
holding one `PortableExecutableReference` per referenced file. Entries are validated
against the file's last-write time and length, so rebuilding a referenced assembly is
picked up on the next resolve. Keyed per file rather than per (file, stamp) so a rebuild
*replaces* its entry instead of growing the cache.

Both `ResolveReference` and `ResolveMissingAssembly` go through it.

**Result — and it is smaller than §2.2 predicted.** One variant per process, so the
static cache cannot leak between variants and flatter a later one:

| variant | before | after |
|---|---|---|
| plain, no `#r` | 21 ms | 21 ms |
| plain, `#r` | 32 ms | 25 ms |
| base directory, `#r` | 35 ms | 26 ms |
| globals, `#r` | 87 ms | 64 ms |
| globals + base directory, no `#r` | 21 ms | 20 ms |
| globals + base directory, `#r` | 92 ms | 68 ms |

Consistent **~25% off `#r` scripts**, no measurable change without `#r`.

**Why not more.** The `#r` penalty is +71 ms (92 vs 21). The cache recovers ~24 ms of
that. The residual ~47 ms is Roslyn rebuilding its reference manager: the presence of
`#r` in a syntax tree stops it reusing the previous compilation's binding, whatever we
return from the resolver. Not fixable from outside the compiler. If `#r` responsiveness
matters more later, the option is to hoist resolved `#r` targets into
`ScriptEnvironment.References` so they are ordinary references and the directive
disappears from the tree — a design change, not a tuning one.

Correctness verified: a referenced assembly rebuilt on disk (gaining a new method) is
picked up on the next pass — error before, clean after. The fixture was emitted on the
fly by the probe, not borrowed from another repository. Confirmed separately that holding
a reference does **not** lock the DLL; overwrite and delete both still succeed, so a host
that rebuilds an assembly its scripts `#r` is unaffected.

Secondary benefit, structural rather than measured: several editors referencing the same
assembly now share one `AssemblyMetadata`, and the old code discarded a freshly built one
on every resolve.

Full solution builds; `UnitTests` 77/77 on net10.0 and net48.

**Measurement note for later steps.** Two probes gave misleading results before this
landed, both for the same reason — shared state across variants in one process. The
process-wide cache made later variants look fast, and an earlier probe's label check
(`label.Contains("#r")`) silently skipped the baseline row. Measure one variant per
process when a process-wide cache is in play.

### Step 2 — cancellation reaches Roslyn (2026-08-09)

Files: `EditorManager.cs`, `ScriptAnalyser.cs`, `ScriptContext.cs`,
`ScintillaScriptEditor.cs`.

Previously `_completionCts` and `_dwellCts` were created and cancelled, but no
`EditorManager` method accepted a token, so a superseded pass ran to completion and its
result was discarded afterwards. Nothing was actually cancelled.

- Added `CancellationToken` **overloads** (not defaulted parameters — `CLAUDE.md` calls
  for overloads on library APIs, and adding a defaulted parameter to a shipped method is
  a binary break) across `EditorManager`: `ApplyScript`, `UpdateScriptDocumentAsync`,
  `GetAutoCompletions`, `GetAPIInfo`, `GetCallTipContext`, `GetSyntaxTreeAsync`,
  `GetSemanticModelAsync`, `GetClassificationsAsync`. Every existing signature is
  retained and delegates with `CancellationToken.None`.
- `ScriptAnalyser` gained the same for `GetCompletionsAsync`, `GetAPIInfoAsync` and
  `GetCallTipContextAsync`; the others already took a token and are now actually given
  one.
- `ScriptContext.CreateAsync` gained a token, so the ~1.4 s cold workspace build can be
  abandoned when an editor is disposed during startup.
- Editor wiring: the completion and dwell paths now pass their existing tokens. The
  call-tip path had **no** token at all despite firing on every `(` and `,` — it gained
  `_callTipCts`, cancelled when a new session starts, on Escape, and on disposal.

**`ApplyScript` no longer half-updates on cancellation.** Diagnostics and classifications
are computed into locals and assigned only once the whole pass succeeds. Previously a
cancellation between the two assignments left diagnostics from one revision of the script
alongside classifications from another. Verified: a cancelled pass leaves 8,442
classifications untouched rather than clobbering them.

**Does the token actually reach Roslyn?** A 22 kB script, pass cancelled mid-flight:

| | elapsed | outcome |
|---|---|---|
| uncancelled (baseline) | 288 ms | — |
| cancelled at 0 ms | 0 ms | 6/6 `OperationCanceledException` |
| cancelled after 5 ms | 19 ms | 6/6 `OperationCanceledException` |
| cancelled after 15 ms | 57 ms | 6/6 `OperationCanceledException` |

Yes. Work that would have run 288 ms aborts in 19 ms. Note cancellation is prompt but
not instant — Roslyn polls the token, so cancelling at 15 ms costs 57 ms, not 15 ms.

Full solution builds; `UnitTests` 77/77 on net10.0 and net48. The control was re-checked
with the step 0 probe plus a completion case, since the completion, call-tip and dwell
paths all changed: 5/5 pass, and the completion list still appears after `.`.

**This is a precondition, not a win on its own.** Nothing here reduces UI-thread blocking
— it makes step 3 safe, by ensuring that superseded work moved to a background thread
stops rather than burning a core invisibly (§6).

### Step 3 — analysis off the UI thread, results version-stamped (2026-08-09)

Files: `EditorManager.cs`, `ScintillaScriptEditor.cs`.

**The offload lives in `EditorManager`, not in the control.** A private
`RunAnalysisAsync` captures the immutable `ScriptContext` and dispatches the work to the
thread pool; `ApplyScript`, `GetAutoCompletions`, `GetAPIInfo`, `GetCallTipContext`,
`GetSyntaxTreeAsync`, `GetSemanticModelAsync` and `GetClassificationsAsync` all route
through it. Putting it here rather than in the Scintilla control means every front-end
benefits — RTF editor, `VirtualScriptEditor`, any future host. `CompileAsync` needed
nothing: `ScriptExecutor` already used `Task.Run`.

**Version stamping.** `_documentVersion` increments on every text change;
`_analysedDocumentVersion` records what the last successful pass covered. A pass captures
the version it is analysing and applies its result only if the document still stands at
that version. This removes `_lastScript` and with it the whole-document string
comparisons — the debounce tick and the retry check are now integer compares, so several
`scintilla.Text` reads disappear as a side effect.

**Two counters, not one.** §5.4 proposed collapsing everything into a single version.
That turns out to be wrong: call tips guard on `_editorStateVersion`, and if text edits
bumped it too, a call-tip session would be abandoned on every keystroke — exactly when
the user is typing arguments into the call the tip is describing. So `_documentVersion`
(text) and `_editorStateVersion` (environment swap, disposal) stay separate. The
`ReferenceEquals(manager, _manager)` checks were also kept; they are redundant with
`_editorStateVersion` but harmless, and removing them is churn for no gain.

**Result — the reported symptom, measured on the real control.** Typing a 34-character
line at ~16 chars/sec, watching the message pump with a 10 ms heartbeat:

| | 200 lines | | 500 lines | |
|---|---|---|---|---|
| | before | after | before | after |
| worst pump stall | 336.8 ms | **90.7 ms** | 344.1 ms | **99.4 ms** |
| stalls > 100 ms | 2 | **0** | 4 | **0** |
| worst keystroke → handled | 320.7 ms | **75.3 ms** | 328.3 ms | **84.0 ms**  |
| median keystroke → handled | 35.1 ms | 35.2 ms | 35.2 ms | 35.2 ms |

The worst-case hitch — the thing that is actually felt — drops roughly **4×**, and stalls
over 100 ms are gone entirely.

Two honest caveats. The unchanged ~35 ms median is the probe harness's own floor
(`SendKeys.SendWait` plus `Application.DoEvents`), not the editor: it is identical before
and after. And the count of stalls >40 ms barely moved for the same reason — the harness
cannot let the heartbeat tick faster than that. The >100 ms and worst-case figures are
the meaningful ones.

**What the residual ~90 ms is.** Still-synchronous UI-thread work: the whole-document
restyle in `ApplyClassificationsToEditor` (~15 ms at 500 lines), the indicator clear, and
the marshalling back. That is what §5.2 (tiering) and §5.6 (viewport-scoped styling)
address.

**Also fixed in passing.** Setting `Environment` after load did not restart the debounce
timer, so a host swapping the environment got no fresh analysis until the next keystroke —
the script would compile against new references while showing squiggles from the old
ones. The setter now restarts the timer. Covered by the regression suite below.

Behavioural regression suite on the real control — 9/9 pass, including the burst cases
version stamping exists to handle:

```
PASS  paste at caret, no selection          PASS  comment block (bulk guard)
PASS  typed characters                      PASS  burst of edits settles correctly
PASS  programmatic InsertText               PASS  burst then correction settles clean
PASS  delete restores validity              PASS  environment swap re-analyses
PASS  completion list shown after '.'
```

Full solution builds; `UnitTests` 77/77 on net10.0 and net48.

**Not done, deliberately.** §5.3 argued for a serialised analysis pump rather than
free-threaded `Task.Run`, to bound memory when several editors are live. `Task.Run` plus
cancellation was enough here — within one editor `_analysisInProgress` already serialises
passes, and across editors only the focused one is typing. Revisit if peak memory becomes
a problem with multiple editors on a rich environment.

### Step 5 — two-tier classification (2026-08-09)

Files: `Classification/SyntacticClassifier.cs` (new), `ScriptAnalyser.cs`,
`EditorManager.cs`, `ScintillaScriptEditor.cs` (+ designer).

- **`SyntacticClassifier`** walks the parse tree and classifies from `SyntaxKind` alone —
  keywords (separating control-flow keywords), literals, comments, punctuation versus
  operators, preprocessor directives, XML doc text. Identifiers all come back as
  `Identifier` and are refined by the semantic pass. Written by hand because no public
  Roslyn API does syntax-only classification (§11).
- **`EditorManager.ApplySyntacticPassAsync`** applies text and returns syntactic
  classifications, offloaded like everything else. Deliberately does not touch
  `LastDiagnostics` / `LastClassifications`, which continue to describe the last full pass.
- **The editor now runs two cadences.** `timerSyntacticColour` (60 ms) colours;
  `timerChangeMonitor` (500 ms) produces diagnostics and semantic colouring. A third
  counter, `_colouredDocumentVersion`, tracks what has been coloured, separate from
  `_analysedDocumentVersion`.

**Result — colour latency after an edit:**

| script | before | after |
|---|---|---|
| 200 lines | 551 ms | **77 ms** |
| 500 lines | 568 ms | **79 ms** |

Inside the <100 ms budget from §1, against ~550 ms before. Typing cost is unchanged
(worst pump stall 93–106 ms, versus 91–99 ms without tier 1) — the second pass is cheap
enough to be free in practice.

**Two ordering rules stop the tiers fighting.** The syntactic pass skips if a full pass is
already running (it is about to produce strictly better colouring, and letting both mutate
the manager's context concurrently is asking for trouble), and it refuses to paint if the
full pass has already coloured that same document version. Without the second rule a
late-returning syntactic pass would repaint semantic colouring with the coarser result.
Regression-tested: `semantic colouring survives tier 1` asserts a type name and a local
still end up with different styles.

Behavioural regression suite — 11/11:

```
PASS  paste at caret, no selection          PASS  burst then correction settles clean
PASS  typed characters                      PASS  environment swap re-analyses
PASS  programmatic InsertText               PASS  semantic colouring survives tier 1
PASS  delete restores validity              PASS  comment and keyword differ
PASS  comment block (bulk guard)            PASS  completion list shown after '.'
PASS  burst of edits settles correctly
```

Full solution builds; `UnitTests` 77/77 on net10.0 and net48.

**Known limitation.** Tier 1 restyles the whole document, so on very large scripts the
60 ms pass gets expensive (~25 ms of styling at 1500 lines). Viewport-scoped styling
(§5.6) is the fix and is the natural next step; `ScriptAnalyser` already has the
span-scoped `GetClassificationsAsync(spanStart, spanLength, ct)` overload it would need.
