using Content.Shared._TP.Aquaculture;
using Content.Shared.Chemistry.Components;
using Content.Shared.Storage;
using Robust.Shared.Audio;

namespace Content.Server._TP.Aquaculture.Components;

[RegisterComponent, Access(typeof(FishOMaticComponent))]
public sealed partial class FishOMaticComponent : Component
{
    /// <summary>
    /// The dictionary of fish breeding, Keyed by FishTypesAndTiers.
    /// The mutagen system will dictate if the tier of the solution matches.
    /// </summary>
    [DataField("outcome")]
    public Dictionary<FishTypesAndTiers, EntitySpawnEntry> FishEggOutcome = new();

    [DataField]
    public int StorageMaxEntities = 2;

    [DataField]
    public string BeakerSlotId = "beakerSlot";

    [DataField]
    public string InputContainerId = "inputContainer";

    [DataField]
    public string MutatorSolutionName = "UnstableMutagen";

    [DataField]
    public Entity<SolutionComponent>? SolutionName = new();

    [ViewVariables]
    public SharedFishOMatic FishOMatic = new();

    [ViewVariables]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier MutateSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");
}

[DataDefinition]
public sealed partial class FishTypesAndTiers
{
    [DataField]
    public string Type { get; set; } = default!;

    [DataField]
    public int Tier { get; set; } = 1;
}
