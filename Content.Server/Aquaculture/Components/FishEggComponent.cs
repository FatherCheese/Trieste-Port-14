using Content.Server.Aquaculture.Systems;
using Content.Shared.Botany.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Aquaculture.Components;

[RegisterComponent, Access(typeof(AquacultureSystem))]
public sealed partial class FishEggComponent : SharedSeedComponent
{
    /// <summary>
    ///     Seed data containing information about the plant type & properties that this seed can grow seed. If
    ///     null, will instead attempt to get data from a seed prototype, if one is defined. See <see
    ///     cref="FishEggId"/>.
    /// </summary>
    [DataField("fishegg")]
    public FishEggData? FishEgg;

    /// <summary>
    ///     If not null, overrides the plant's initial health. Otherwise, the plant's initial health is set to the Endurance value.
    /// </summary>
    [DataField]
    public float? HealthOverride = null;

    /// <summary>
    ///     Name of a base seed prototype that is used if <see cref="FishEgg"/> is null.
    /// </summary>
    [DataField("fishEggId", customTypeSerializer: typeof(PrototypeIdSerializer<FishEggPrototype>))]
    public string? FishEggId;
}
