using Content.Shared.EntityEffects;
using Robust.Shared.Serialization;

namespace Content.Shared._TP.Aquaculture.Random;

/// <summary>
/// Data that specifies the odds and effects of possible random fish mutations.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomFishMutation
{
    /// <summary>
    /// Odds of this mutation occurring, with 1 point of mutation severity on a fish.
    /// Default is 0.
    /// </summary>
    [DataField]
    public float BaseOdds = 0;

    /// <summary>
    /// The name of this mutation.
    /// </summary>
    [DataField]
    public string Name = "";

    /// <summary>
    /// The actual EntityEffect to apply to the target.
    /// </summary>
    [DataField]
    public EntityEffect Effect = default!;

    /// <summary>
    /// This mutation will target the harvested produce.
    /// </summary>
    [DataField]
    public bool AppliesToProduce = true;

    /// <summary>
    /// This mutation will target the growing fish as soon as this mutation is applied.
    /// </summary>
    [DataField]
    public bool AppliesToFish = true;

    /// <summary>
    /// This mutation stays on the fish and its produce.
    /// If false while AppliesToFish is true, the effect will run when triggered.
    /// </summary>
    [DataField]
    public bool Persists = true;
}
