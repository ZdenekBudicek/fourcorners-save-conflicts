using FourCorners.SaveConflicts;
var anonymous = new Progress(9, 3, 2, 0, 100);
var account = new Progress(2, 57, 140, 12, 900);
foreach (var context in Enum.GetValues<SyncContext>())
{
    var result = SaveResolver.Resolve(anonymous, account, context);
    Console.WriteLine($"{context}: {result.Source}, level {result.Selected?.Level}, reason {result.Reason}");
}
Console.WriteLine("After an authenticated identity change, independent counters must not overwrite deeper account progress.");
