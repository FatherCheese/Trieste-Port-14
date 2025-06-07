using Content.Shared._TP.Planktology;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server._TP.Planktology.Components;

/// <summary>
///     This component allows something to be a plankton colony.
///     This is mainly used by canisters and separators.
///     NOTE: A lot of the data needs to be GENERATED. These are mainly placeholders!
/// </summary>
[RegisterComponent]
public sealed partial class PlanktonComponent : Component
{
    #region Temperature
    /// <summary>
    ///     The minimum temperature that plankton can tolerate.
    ///     Temperature is stored in the tank, and this value
    ///     is modified based on Cryophilic or Pyrophilic traits.
    ///     See SharedPlanktonTypes for more information.
    /// </summary>
    [DataField]
    public float TemperatureToleranceLow { get; set; } = 15.0f;

    /// <summary>
    ///     The maximum temperature that plankton can tolerate.
    ///     Temperature is stored in the tank, and this value
    ///     is modified based on Cryophilic or Pyrophilic traits.
    ///     See SharedPlanktonTypes for more information.
    /// </summary>
    [DataField]
    public float TemperatureToleranceHigh { get; set; } = 35.0f;
    #endregion

    #region Stats
    /// <summary>
    ///     The base diet of the plankton.
    ///     The default value is Photosynthetic.
    /// </summary>
    [DataField]
    public PlanktonDiet Diet { get; set; } = PlanktonDiet.Photosynthetic;

    /// <summary>
    ///     The base species name of the plankton.
    ///     The default value is "Unknown" until randomly generated.
    /// </summary>
    [DataField]
    public PlanktonName SpeciesName { get; set; } = new("Unknown", "species");

    /// <summary>
    ///     The base characteristics of the plankton.
    ///     Default is None before generation. This isn't a list since
    ///     characteristics are a bitwise enum.
    /// </summary>
    [DataField]
    public PlanktonCharacteristics Characteristics = PlanktonCharacteristics.None;

    /// <summary>
    ///     The base size of the planket.
    ///     The default value is "Tiny" until randomly generated.
    /// </summary>
    [DataField]
    public PlanktonSize SpeciesSize { get; set; } = PlanktonSize.Tiny;

    /// <summary>
    ///     Current health of the plankton colony. The default value is 100.
    /// </summary>
    [DataField]
    public float Health { get; set; } = 100.0f;

    /// <summary>
    ///     The plankton's current hunger level. The default value starts at 0.
    ///     The higher the value, the more hungry the plankton is.
    /// </summary>
    [DataField]
    public float Hunger { get; set; } = 0.0f;

    /// <summary>
    ///     The Current population size of the plankton colony. Default is 100.
    ///     It decreases based on health and the environment.
    /// </summary>
    [DataField]
    public int Population { get; set; } = 100;

    /// <summary>
    ///     The chemical reagent this species produces.
    ///     The default value is null before generation and may stay null.
    /// </summary>
    [DataField]
    public ReagentId? ProducedReagent { get; set; } = null;
    #endregion

    #region ResearchSpecific
    /// <summary>
    ///     Whether the plank has been fully analyzed/researched.
    ///     The default value is obviously false.
    /// </summary>
    [DataField]
    public bool IsAnalyzed { get; set; } = false;

    /// <summary>
    ///     Bitflags for which traits have been discovered through research.
    ///     The default value is None.
    /// </summary>
    [DataField]
    public PlanktonCharacteristics DiscoveredTraits { get; set; } = PlanktonCharacteristics.None;
    #endregion

    #region Timers
    /// <summary>
    ///     The last time this plankton was updated (for system processing)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastUpdateTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    ///     The amount of reagent produced per cycle.
    ///     The default value is 0.0f before generation and may stay 0.0f.
    /// </summary>
    [DataField]
    public float ReagentProductionRate { get; set; } = 0.0f;
    #endregion
}
