using System.Diagnostics;
using LanguageExt;
using static LanguageExt.Prelude;

Console.WriteLine("=== AsyncLocal/Activity.Current Verification with LanguageExt ===\n");

var activitySource = new ActivitySource("TestService");
var listener = new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
};
ActivitySource.AddActivityListener(listener);

// Test 1: Basic async/await
await Test1_BasicAsyncAwait(activitySource);

// Test 2: FinT<IO> without Fork
await Test2_FinT_WithoutFork(activitySource);

// Test 3: IO.liftAsync with nested RunAsync
await Test3_IO_LiftAsync_NestedRunAsync(activitySource);

// Test 4: Fork (expected to fail)
await Test4_Fork(activitySource);

// Test 5: Custom AsyncLocal (comparison)
await Test5_CustomAsyncLocal();

// Test 6: ConfigureAwait(false)
await Test6_ConfigureAwaitFalse(activitySource);

// Test 7: ThreadPool explicit
await Test7_ThreadPoolExplicit(activitySource);

// Test 8: Simulate TraverseSerial pattern
await Test8_TraverseSerialPattern(activitySource);

// Test 9: Multiple nested RunAsync with real async work
await Test9_NestedWithRealAsync(activitySource);

// Test 10: SuppressFlow - THIS should fail
await Test10_SuppressFlow(activitySource);

// Test 11: UnsafeQueueUserWorkItem - THIS should fail
await Test11_UnsafeQueueUserWorkItem(activitySource);

Console.WriteLine("\n=== Summary ===");
Console.WriteLine("If Activity.Current becomes null without Fork, the issue is in IO monad's internal behavior.");
Console.WriteLine("If Activity.Current only becomes null with Fork, the issue is Task.Factory.StartNew(LongRunning).");

listener.Dispose();
activitySource.Dispose();

// =============================================================================
// Test Methods
// =============================================================================

static async Task Test1_BasicAsyncAwait(ActivitySource source)
{
    Console.WriteLine("--- Test 1: Basic async/await ---");

    using var activity = source.StartActivity("Test1");
    Activity.Current = activity;

    Console.WriteLine($"  Before await: Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    await Task.Delay(10);

    Console.WriteLine($"  After await:  Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    var result = Activity.Current == activity ? "PASS" : "FAIL";
    Console.WriteLine($"  Result: {result}\n");
}

static async Task Test2_FinT_WithoutFork(ActivitySource source)
{
    Console.WriteLine("--- Test 2: FinT<IO>.Run().RunAsync() without Fork ---");

    using var activity = source.StartActivity("Test2");
    Activity.Current = activity;

    Console.WriteLine($"  Before FinT:  Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    // Simple FinT<IO> execution without Fork
    FinT<IO, int> finT = FinT.Succ<IO, int>(42);
    Fin<int> finResult = await finT.Run().RunAsync();

    Console.WriteLine($"  After FinT:   Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  FinT Result:  {finResult}");

    var result = Activity.Current == activity ? "PASS" : "FAIL";
    Console.WriteLine($"  Result: {result}\n");
}

static async Task Test3_IO_LiftAsync_NestedRunAsync(ActivitySource source)
{
    Console.WriteLine("--- Test 3: IO.liftAsync with nested RunAsync ---");

    using var activity = source.StartActivity("Test3");
    Activity.Current = activity;

    Console.WriteLine($"  Before IO:    Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    // This simulates TraverseSerial pattern
    IO<string> io = IO.liftAsync<string>(async () =>
    {
        Console.WriteLine($"  Inside liftAsync (before nested): Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

        // Nested FinT execution (like TraverseSerial does)
        FinT<IO, int> innerFinT = FinT.Succ<IO, int>(100);
        Fin<int> innerResult = await innerFinT.Run().RunAsync();

        Console.WriteLine($"  Inside liftAsync (after nested):  Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

        return $"Inner result: {innerResult}";
    });

    string result = await io.RunAsync();

    Console.WriteLine($"  After IO:     Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  IO Result:    {result}");

    var testResult = Activity.Current == activity ? "PASS" : "FAIL";
    Console.WriteLine($"  Result: {testResult}\n");
}

static async Task Test4_Fork(ActivitySource source)
{
    Console.WriteLine("--- Test 4: IO.Fork() ---");

    using var activity = source.StartActivity("Test4");
    Activity.Current = activity;

    Console.WriteLine($"  Before Fork:  Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    // Capture Activity.Current inside the forked IO
    string? capturedInFork = null;

    IO<int> ioToFork = IO.lift(() =>
    {
        capturedInFork = Activity.Current?.DisplayName ?? "null";
        return 42;
    });

    // Fork creates a new thread with Task.Factory.StartNew(LongRunning)
    ForkIO<int> fork = await ioToFork.Fork().RunAsync();
    int forkResult = await fork.Await.RunAsync();

    Console.WriteLine($"  After Fork:   Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  Inside Fork:  Activity.Current was = {capturedInFork}");
    Console.WriteLine($"  Fork Result:  {forkResult}");

    var testResult = capturedInFork == activity?.DisplayName ? "PASS" : "FAIL (Activity lost in Fork)";
    Console.WriteLine($"  Result: {testResult}\n");
}

static async Task Test5_CustomAsyncLocal()
{
    Console.WriteLine("--- Test 5: Custom AsyncLocal<string> comparison ---");

    AsyncLocal<string> customAsyncLocal = new();
    customAsyncLocal.Value = "TestValue";

    Console.WriteLine($"  Before FinT:  AsyncLocal = {customAsyncLocal.Value ?? "null"}");

    // FinT execution
    FinT<IO, int> finT = FinT.Succ<IO, int>(42);
    await finT.Run().RunAsync();

    Console.WriteLine($"  After FinT:   AsyncLocal = {customAsyncLocal.Value ?? "null"}");

    // Nested IO.liftAsync
    IO<string> io = IO.liftAsync<string>(async () =>
    {
        Console.WriteLine($"  Inside liftAsync: AsyncLocal = {customAsyncLocal.Value ?? "null"}");

        FinT<IO, int> innerFinT = FinT.Succ<IO, int>(100);
        await innerFinT.Run().RunAsync();

        Console.WriteLine($"  After nested:     AsyncLocal = {customAsyncLocal.Value ?? "null"}");

        return "done";
    });

    await io.RunAsync();

    Console.WriteLine($"  Final:        AsyncLocal = {customAsyncLocal.Value ?? "null"}");

    var result = customAsyncLocal.Value == "TestValue" ? "PASS" : "FAIL";
    Console.WriteLine($"  Result: {result}\n");
}

static async Task Test6_ConfigureAwaitFalse(ActivitySource source)
{
    Console.WriteLine("--- Test 6: ConfigureAwait(false) ---");

    using var activity = source.StartActivity("Test6");
    Activity.Current = activity;

    Console.WriteLine($"  Before:       Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    // ConfigureAwait(false) should NOT affect ExecutionContext/AsyncLocal
    await Task.Delay(10).ConfigureAwait(false);

    Console.WriteLine($"  After CAF:    Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    var result = Activity.Current == activity ? "PASS" : "FAIL";
    Console.WriteLine($"  Result: {result}\n");
}

static async Task Test7_ThreadPoolExplicit(ActivitySource source)
{
    Console.WriteLine("--- Test 7: ThreadPool.QueueUserWorkItem ---");

    using var activity = source.StartActivity("Test7");
    Activity.Current = activity;

    Console.WriteLine($"  Before:       Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    string? capturedInThreadPool = null;
    var tcs = new TaskCompletionSource<bool>();

    // ThreadPool.QueueUserWorkItem does NOT flow ExecutionContext by default
    ThreadPool.QueueUserWorkItem(_ =>
    {
        capturedInThreadPool = Activity.Current?.DisplayName ?? "null";
        tcs.SetResult(true);
    });

    await tcs.Task;

    Console.WriteLine($"  After:        Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  In ThreadPool: Activity.Current was = {capturedInThreadPool}");

    var result = capturedInThreadPool == activity?.DisplayName ? "PASS" : "FAIL (Activity lost in ThreadPool)";
    Console.WriteLine($"  Result: {result}\n");
}

static async Task Test8_TraverseSerialPattern(ActivitySource source)
{
    Console.WriteLine("--- Test 8: TraverseSerial-like pattern ---");

    using var activity = source.StartActivity("Test8");
    Activity.Current = activity;

    Console.WriteLine($"  Before:       Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    // Simulate TraverseSerial: IO.liftAsync with loop and nested RunAsync
    var items = Enumerable.Range(1, 3).ToList();
    List<string> capturedDuringLoop = new();

    IO<List<int>> io = IO.liftAsync<List<int>>(async () =>
    {
        List<int> results = new();

        foreach (var item in items)
        {
            capturedDuringLoop.Add(Activity.Current?.DisplayName ?? "null");

            // Nested FinT execution (like real TraverseSerial)
            FinT<IO, int> innerFinT = IO.liftAsync<int>(async () =>
            {
                await Task.Delay(5);  // Real async work
                return item * 10;
            }).Map(x => Fin.Succ(x)).As().Kind().As();

            // This is what TraverseSerial does internally
            Fin<int> finResult = await innerFinT.Run().RunAsync();
            results.Add(finResult.Match(Succ: x => x, Fail: _ => -1));
        }

        return results;
    });

    var result = await io.RunAsync();

    Console.WriteLine($"  After:        Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  During loop:  [{string.Join(", ", capturedDuringLoop)}]");
    Console.WriteLine($"  Results:      [{string.Join(", ", result)}]");

    var allPreserved = capturedDuringLoop.All(x => x == activity?.DisplayName);
    var testResult = allPreserved && Activity.Current == activity ? "PASS" : "FAIL";
    Console.WriteLine($"  Result: {testResult}\n");
}

static async Task Test9_NestedWithRealAsync(ActivitySource source)
{
    Console.WriteLine("--- Test 9: Deep nesting with real async ---");

    using var activity = source.StartActivity("Test9");
    Activity.Current = activity;

    Console.WriteLine($"  Before:       Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    List<string> capturedAtEachLevel = new();

    // Level 1: Outer IO
    IO<int> level1 = IO.liftAsync<int>(async () =>
    {
        capturedAtEachLevel.Add($"L1: {Activity.Current?.DisplayName ?? "null"}");
        await Task.Delay(5);

        // Level 2: Inner IO
        IO<int> level2 = IO.liftAsync<int>(async () =>
        {
            capturedAtEachLevel.Add($"L2: {Activity.Current?.DisplayName ?? "null"}");
            await Task.Delay(5);

            // Level 3: FinT
            FinT<IO, int> level3 = FinT.liftIO<IO, int>(
                IO.liftAsync<int>(async () =>
                {
                    capturedAtEachLevel.Add($"L3: {Activity.Current?.DisplayName ?? "null"}");
                    await Task.Delay(5);
                    return 42;
                }));

            var l3Result = await level3.Run().RunAsync();
            capturedAtEachLevel.Add($"L2 after L3: {Activity.Current?.DisplayName ?? "null"}");
            return l3Result.Match(Succ: x => x, Fail: _ => -1);
        });

        var l2Result = await level2.RunAsync();
        capturedAtEachLevel.Add($"L1 after L2: {Activity.Current?.DisplayName ?? "null"}");
        return l2Result;
    });

    var result = await level1.RunAsync();

    Console.WriteLine($"  After:        Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  Captured:");
    foreach (var cap in capturedAtEachLevel)
    {
        Console.WriteLine($"    - {cap}");
    }

    var allPreserved = capturedAtEachLevel.All(x => x.Contains(activity?.DisplayName ?? "NOTFOUND"));
    var testResult = allPreserved && Activity.Current == activity ? "PASS" : "FAIL";
    Console.WriteLine($"  Result: {testResult}\n");
}

static async Task Test10_SuppressFlow(ActivitySource source)
{
    Console.WriteLine("--- Test 10: ExecutionContext.SuppressFlow() ---");
    Console.WriteLine("  (This SHOULD fail - proves ExecutionContext matters)");

    using var activity = source.StartActivity("Test10");
    Activity.Current = activity;

    Console.WriteLine($"  Before:       Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    string? capturedInTask = null;

    // Suppress ExecutionContext flow - this should cause AsyncLocal to be lost
    var afc = ExecutionContext.SuppressFlow();
    try
    {
        // Use Wait() instead of await to stay on same thread for SuppressFlow
        var task = Task.Run(() =>
        {
            capturedInTask = Activity.Current?.DisplayName ?? "null";
        });
        task.Wait();  // Sync wait to avoid thread switch before Undo()
    }
    finally
    {
        afc.Undo();
    }

    Console.WriteLine($"  After:        Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  In Task:      Activity.Current was = {capturedInTask}");

    var testResult = capturedInTask == activity?.DisplayName
        ? "PASS (unexpected!)"
        : "FAIL as expected (ExecutionContext was suppressed)";
    Console.WriteLine($"  Result: {testResult}\n");

    await Task.CompletedTask;  // Keep method async
}

static async Task Test11_UnsafeQueueUserWorkItem(ActivitySource source)
{
    Console.WriteLine("--- Test 11: ThreadPool.UnsafeQueueUserWorkItem() ---");
    Console.WriteLine("  (This SHOULD fail - Unsafe version doesn't flow context)");

    using var activity = source.StartActivity("Test11");
    Activity.Current = activity;

    Console.WriteLine($"  Before:       Activity.Current = {Activity.Current?.DisplayName ?? "null"}");

    string? capturedInThreadPool = null;
    var tcs = new TaskCompletionSource<bool>();

    // UnsafeQueueUserWorkItem does NOT flow ExecutionContext
    ThreadPool.UnsafeQueueUserWorkItem(_ =>
    {
        capturedInThreadPool = Activity.Current?.DisplayName ?? "null";
        tcs.SetResult(true);
    }, null);

    await tcs.Task;

    Console.WriteLine($"  After:        Activity.Current = {Activity.Current?.DisplayName ?? "null"}");
    Console.WriteLine($"  In ThreadPool: Activity.Current was = {capturedInThreadPool}");

    var testResult = capturedInThreadPool == activity?.DisplayName
        ? "PASS (unexpected!)"
        : "FAIL as expected (Unsafe doesn't flow context)";
    Console.WriteLine($"  Result: {testResult}\n");
}
