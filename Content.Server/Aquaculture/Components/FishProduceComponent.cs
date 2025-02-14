using Content.Server.Aquaculture.Systems;
using Content.Shared.Botany.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Aquaculture.Components;

[RegisterComponent]
[Access(typeof(AquacultureSystem))]
public sealed partial class FishProduceComponent : SharedProduceComponent
{
    [DataField("targetSolution")] public string SolutionName { get; set; } = "food";

    [DataField("planktonSolution")] public string PlanktonSolutionName { get; set; } = "plankton";

    /// <summary>
    ///     Seed data used to create a <see cref="FishEggComponent"/> when this produce has its seeds extracted.
    /// </summary>
    [DataField]
    public FishEggData? FishEgg;

    /// <summary>
    ///     Seed data used to create a <see cref="FishEggComponent"/> when this produce has its seeds extracted.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<FishEggPrototype>))]
    public string? FishEggId;
}
