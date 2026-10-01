# 4Corners save conflict resolver

A small, executable C# case study extracted and adapted from the local/cloud progress decision rules in my Unity game **4Corners**. The interesting part is the policy: a recent anonymous save must not erase an advanced account just because its local write counter is larger.

## Try the concrete failure case

```sh
dotnet run --project demo -c Release
dotnet run --project tests -c Release
```

Requires the .NET 9 SDK. No Unity, backend, credentials or runtime NuGet packages are needed.

| Snapshot | Local write version | Campaign level |
| --- | ---: | ---: |
| Anonymous session | 9 | 3 |
| Existing account | 2 | 57 |

For normal synchronization of the **same identity**, the newer version wins. After an **authenticated identity change**, the counters belong to different histories: the resolver compares progress and selects the level-57 snapshot instead. The demo prints both outcomes and their reasons.

## Policy

1. Empty snapshots do not displace existing progress.
2. Within one identity, version is the primary ordering.
3. For equal versions or an identity change, compare level, stars, best survival wave and finally coins, in that order.
4. A tie selects local deterministically. The result includes the chosen immutable snapshot and a reason.
5. The next version exceeds both inputs; overflow is explicitly rejected.

The public adaptation uses a **lexicographic comparison**. Unlike the original weighted score, even an arbitrarily large coin balance cannot outweigh an extra level. Survival-only progress is not treated as empty. Negative fields and invalid contexts fail explicitly. Regression checks cover these boundaries, identity transitions, empty restores and the original failure scenario.

## Scope and tradeoffs

This is a snapshot-selection policy, **not a complete cloud-save service**, a CRDT, or an anti-cheat system. It selects one complete snapshot; it does not add balances or combine purchases. Selecting one branch can discard progress unique to the other branch. Production integrations should preserve backups and show a conflict choice where appropriate.

A version counter cannot detect every divergent multi-device history. A networked implementation needs authenticated identities, backend authorization, optimistic concurrency/compare-and-swap, retry handling and separate transaction/entitlement records for money or purchases. The caller must establish that an identity switch is genuine. Wall-clock timestamps are deliberately absent from the decision.

## Provenance

Adapted from `ProgressMergeRules`, `ProgressSavePayload` and their regression scenarios in my 4Corners project. The full game's source and assets remain private. For this October 2026 public edition, Codex helped reduce the snapshot to the relevant fields, separate it from Unity/SDKs, translate the documentation, replace weighted scoring with strict priorities, add explicit decision reasons and strengthen boundary tests. This is an honestly scoped adaptation of a real game subsystem, not the entire production save pipeline or a claim of exclusively manual authorship.
