using Content.Server._TP.Aquaculture.Effects;
using Content.Server._TP.Aquaculture.Systems;
using Content.Shared._TP.Aquaculture.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using FishAdjustAttribute = Content.Server._TP.Aquaculture.Effects.FishAdjustAttribute;

namespace Content.Server._TP.Aquaculture.Components;

/// <summary>
/// This component handles adding the ability of growing fish.
/// It should use up two different solutions to do so; food and water.
/// </summary>
[RegisterComponent] [Access(typeof(FishGrowerSystem),
    typeof(FishAdjustAttribute))]
public sealed partial class FishGrowerComponent : Component
{
    #region Solution Amounts and Uses
    /// <summary>
    /// The amount of 'clean' water in the container.
    /// Default is 100.
    /// </summary>
    [DataField]
    public float WaterAmount = 100f;

    /// <summary>
    /// The amount of plant-food in the container.
    /// Default is 100.
    /// </summary>
    [DataField]
    public float FoodAmount = 100f;

    /// <summary>
    /// The amount of Ammonia in the container.
    /// Default is 0.
    /// </summary>
    [DataField]
    public float WasteAmount = 0f;

    /// <summary>
    /// The maximum amount of waste that can be held by the container.
    /// Default is 100.
    /// </summary>
    [DataField]
    public float WasteAmountMax = 100f;

    /// <summary>
    /// The amount of Mutator in the container.
    /// Default is 0.
    /// </summary>
    [DataField]
    public float MutatorAmount = 0f;
    #endregion

    #region Fishies
    /// <summary>
    /// The current fish eggs growing in the container.
    /// </summary>
    [DataField]
    public FishEggComponent? FishEgg { get; set; }

    /// <summary>
    /// If the fish in the container are dead.
    /// </summary>
    [DataField]
    public bool FishDead;

    /// <summary>
    /// The health of the fish in the container.
    /// Default is 100.
    /// </summary>
    [DataField]
    public int FishHealth = 100;

    /// <summary>
    /// The max health of the fish in the container.
    /// Default is 100.
    /// </summary>
    [DataField]
    public int FishHealthMax = 100;
    #endregion

    #region Solutions
    /// <summary>
    /// The water solution name.
    /// Default is 'water'.
    /// </summary>
    [DataField]
    public string WaterSolutionName { get; set; } = "water";

    /// <summary>
    /// The food solution name.
    /// Default is 'soil'.
    /// </summary>
    [DataField]
    public string FoodSolutionName { get; set; } = "soil";

    /// <summary>
    /// The mutator solution name.
    /// Default is "fishDna"
    /// </summary>
    [DataField]
    public string MutatorSolutionId { get; set; } = "TP14FishDNA";

    /// <summary>
    /// The total solution inside the container 'soil' solution.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? TotalSolution = null;

    /// <summary>
    /// The sound that plays when a solution is put in.
    /// </summary>
    [DataField]
    public SoundSpecifier? SolutionSound;
    #endregion

    #region Timers
    /// <summary>
    /// The minimum amount of time it takes for a fish to age.
    /// Default is 60.
    /// </summary>
    [DataField]
    public float FishGrowMin = 60f;

    /// <summary>
    /// The maximum amount of time it takes for a fish to age.
    /// Default is 120.
    /// </summary>
    [DataField]
    public float FishGrowMax = 120f;

    /// <summary>
    /// Inherited from Botany.
    /// </summary>
    [DataField]
    public TimeSpan ConsumptionDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// A cycle delay for the Update timer.
    /// This is apparently needed, or else the Container queues indefinitely.
    /// </summary>
    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    /// <summary>
    /// Same as the CycleDelay, this is needed for the Update timer.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    /// <summary>
    /// When the fish should grow.
    /// </summary>
    [DataField]
    public TimeSpan NextFishGrowth = TimeSpan.Zero;

    /// <summary>
    /// Inherited from Botany.
    /// </summary>
    [DataField]
    public TimeSpan FishGrowthDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// When to consume food and water.
    /// </summary>
    [DataField]
    public TimeSpan NextConsumption = TimeSpan.Zero;

    /// <summary>
    /// The minimum amount of time it takes for a fish to consume.
    /// Default is 15.
    /// </summary>
    [DataField]
    public float ConsumeMin = 15f;

    /// <summary>
    /// The maximum amount of time it takes for a fish to consume.
    /// Default is 45.
    /// </summary>
    [DataField]
    public float ConsumeMax = 45f;
    #endregion

    #region Filters
    /// <summary>
    /// The filter ID.
    /// Default is 'Filter'
    /// </summary>
    [DataField]
    public string FilterId = "filter";

    /// <summary>
    /// The component needed for the filter item.
    /// </summary>
    [DataField]
    public FitsInFilterComponent? Filter;

    /// <summary>
    /// Input slot for the filter
    /// Default is set to "filterSlot"
    /// </summary>
    [DataField]
    public string InputSlotName = "filterSlot";
    #endregion
}
