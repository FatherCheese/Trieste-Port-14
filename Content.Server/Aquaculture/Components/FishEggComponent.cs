using Content.Server.Aquaculture.Systems;
using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Server.Aquaculture.Components;

/// <summary>
/// This component handles the different kind of fish eggs.
/// </summary>
[RegisterComponent, Access(typeof(FishEggSystem), typeof(FishGrowerSystem))]
public sealed partial class FishEggComponent : Component
{
    [DataField(required: true)]
    public ResPath FishRsi { get; set; } = default!;

    [DataField]
    public string FishIconState { get; set; } = "produce";

    /// <summary>
    /// The 'produce' items that spawn upon a harvest.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> FishProduced { get; set; } = new();

    /// <summary>
    /// The eggs that spawn upon a harvest.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> EggsProduced { get; set; } = new();

    /// <summary>
    /// Copied over from SeedPrototype.
    /// </summary>
    [DataField]
    public float Potency = 1f;
}
