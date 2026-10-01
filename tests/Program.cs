using FourCorners.SaveConflicts;
int count = 0;
void Test(string name, Action action) { action(); count++; Console.WriteLine("PASS " + name); }
void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
Progress P(long version = 0, int level = 0, int stars = 0, int wave = 0, int coins = 0) => new(version, level, stars, wave, coins);
Resolution Resolve(Progress? a, Progress? b, SyncContext c = SyncContext.SameIdentity) => SaveResolver.Resolve(a, b, c);
Test("Both empty", () => Check(Resolve(null, P()).Source == Source.Neither));
Test("Fresh installation restores cloud", () => Check(Resolve(P(), P(7, 42)).Source == Source.Cloud));
Test("First local progress", () => Check(Resolve(P(0, 3), null).Source == Source.Local));
Test("Newer local version", () => Check(Resolve(P(12, 30), P(5, 40)).Source == Source.Local));
Test("Newer cloud version", () => Check(Resolve(P(3, 10), P(4, 10)).Source == Source.Cloud));
Test("Identity change preserves account", () => Check(Resolve(P(9, 3), P(2, 57), SyncContext.AfterIdentityChange).Source == Source.Cloud));
Test("Identity change preserves anonymous progress", () => Check(Resolve(P(1, 40), P(30, 5), SyncContext.AfterIdentityChange).Source == Source.Local));
Test("Level outranks unlimited coins", () => Check(Resolve(P(4, 2, coins: int.MaxValue), P(4, 3)).Source == Source.Cloud));
Test("Stars outrank waves and coins", () => Check(Resolve(P(4, 10, 1, int.MaxValue, int.MaxValue), P(4, 10, 2)).Source == Source.Cloud));
Test("Wave outranks coins", () => Check(Resolve(P(4, 10, 2, 1, int.MaxValue), P(4, 10, 2, 2)).Source == Source.Cloud));
Test("Tie keeps local", () => Check(Resolve(P(4, 10), P(4, 10)).Reason == Reason.TieKeepsLocal));
Test("Survival-only progress is not empty", () => Check(Resolve(P(wave: 1), null).Source == Source.Local));
Test("Selects whole snapshot without adding currencies", () => {
    var a = P(5, 20, coins: 10); var b = P(5, 10, coins: 5000);
    Check(ReferenceEquals(Resolve(a, b).Selected, a)); Check(Resolve(a, b).Selected!.Coins == 10);
});
Test("Next version exceeds both", () => Check(SaveResolver.NextVersion(P(9), P(4)) == 10));
Test("First version", () => Check(SaveResolver.NextVersion(null, null) == 1));
Test("Overflow fails", () => { try { SaveResolver.NextVersion(P(long.MaxValue), null); throw new Exception("Expected overflow"); } catch (OverflowException) { } });
foreach (var p in new[] { P(-1), P(level: -1), P(stars: -1), P(wave: -1), P(coins: -1) })
    Test("Negative field rejected", () => { try { Resolve(p, null); throw new Exception("Expected rejection"); } catch (ArgumentOutOfRangeException) { } });
Test("Invalid context rejected", () => { try { Resolve(null, null, (SyncContext)99); throw new Exception("Expected rejection"); } catch (ArgumentOutOfRangeException) { } });
Test("Small-state priority sweep", () => {
    for (int a = 0; a < 5; a++) for (int b = 0; b < 5; b++)
        Check(Resolve(P(1, a, coins: 999), P(1, b)).Source == (a < b ? Source.Cloud : Source.Local));
});
Console.WriteLine($"{count} checks passed.");
