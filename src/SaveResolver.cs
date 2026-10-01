namespace FourCorners.SaveConflicts;

/// <summary>A reduced progress snapshot. No identity, SDK, clock, or device data.</summary>
public sealed record Progress(long Version, int Level, int Stars, int BestWave, int Coins)
{
    public bool IsEmpty => Version == 0 && Level == 0 && Stars == 0 && BestWave == 0 && Coins == 0;
    internal void Validate()
    {
        if (Version < 0 || Level < 0 || Stars < 0 || BestWave < 0 || Coins < 0)
            throw new ArgumentOutOfRangeException(nameof(Progress), "Progress values cannot be negative.");
    }
}

public enum SyncContext { SameIdentity, AfterIdentityChange }
public enum Source { Neither, Local, Cloud }
public enum Reason { BothEmpty, RestoreCloud, FirstLocalProgress, NewerVersion, FurtherProgress, TieKeepsLocal }
public sealed record Resolution(Source Source, Reason Reason, Progress? Selected);

/// <summary>Selects a complete snapshot. Caller authenticates identity changes and persists with optimistic concurrency.</summary>
public static class SaveResolver
{
    public static Resolution Resolve(Progress? local, Progress? cloud, SyncContext context)
    {
        if (!Enum.IsDefined(context)) throw new ArgumentOutOfRangeException(nameof(context));
        local?.Validate(); cloud?.Validate();
        bool localEmpty = local is null || local.IsEmpty;
        bool cloudEmpty = cloud is null || cloud.IsEmpty;
        if (localEmpty && cloudEmpty) return new(Source.Neither, Reason.BothEmpty, null);
        if (localEmpty) return new(Source.Cloud, Reason.RestoreCloud, cloud);
        if (cloudEmpty) return new(Source.Local, Reason.FirstLocalProgress, local);
        // Version counters from separate identities are incomparable.
        if (context == SyncContext.SameIdentity && local!.Version != cloud!.Version)
            return local.Version > cloud.Version ? new(Source.Local, Reason.NewerVersion, local) : new(Source.Cloud, Reason.NewerVersion, cloud);
        int comparison = CompareProgress(local!, cloud!);
        if (comparison < 0) return new(Source.Cloud, Reason.FurtherProgress, cloud);
        return new(Source.Local, comparison == 0 ? Reason.TieKeepsLocal : Reason.FurtherProgress, local);
    }
    public static long NextVersion(Progress? local, Progress? cloud)
    {
        local?.Validate(); cloud?.Validate();
        return checked(Math.Max(local?.Version ?? 0, cloud?.Version ?? 0) + 1);
    }
    // Lexicographic priority: arbitrarily many coins cannot outweigh a completed level.
    private static int CompareProgress(Progress a, Progress b)
    {
        int result = a.Level.CompareTo(b.Level);
        if (result == 0) result = a.Stars.CompareTo(b.Stars);
        if (result == 0) result = a.BestWave.CompareTo(b.BestWave);
        if (result == 0) result = a.Coins.CompareTo(b.Coins);
        return result;
    }
}
