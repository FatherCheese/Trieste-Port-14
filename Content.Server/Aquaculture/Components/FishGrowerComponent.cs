using Content.Server.Aquaculture.Systems;
using Content.Server.EntityEffects.Effects.Aquaculture;
using Content.Shared.Aquaculture.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Aquaculture.Components;

/// <summary>
/// This component handles growing fish, such as in aquaculture tanks.
/// It should use up two different solutions to do so; food and water.
/// </summary>
[RegisterComponent, Access(typeof(FishGrowerSystem), typeof(FishAdjustAttribute))]
public sealed partial class FishGrowerComponent : Component
{
    #region Solution Amounts and Uses
    /// <summary>
    /// The amount of 'clean' water in the container. Default is 100.
    /// </summary>
    [DataField]
    public float WaterAmount = 100f;

    /// <summary>
    /// The amount of plant-food in the container. Default is 100.
    /// </summary>
    [DataField]
    public float FoodAmount = 100f;

    /// <summary>
    /// The amount of Ammonia in the container. Default is 0.
    /// </summary>
    [DataField]
    public float AmmoniaAmount = 0f;

    /// <summary>
    /// The amount of water used. (per second?)
    /// </summary>
    [DataField]
    public float WaterUse = 0.75f;

    /// <summary>
    /// The amount of plant-food used. (per second?)
    /// </summary>
    [DataField]
    public float FoodUse = 0.5f;

    /// <summary>
    /// The amount of ammonia created. (per second?)
    /// </summary>
    [DataField]
    public float AmmoniaCreation = 0.75f;
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

    // FISH AGES //
    // Any older than adult they're considered "old" and start to lose health.
    // These are mostly here just to be 'customizable'

    /// <summary>
    /// When the fish should be an egg.
    /// Default is age 1.
    /// </summary>
    [DataField]
    public int FishAgeEgg = 1;

    /// <summary>
    /// When the fish should be an embryo.
    /// Default is age 2.
    /// </summary>
    [DataField]
    public int FishAgeEmbryo = 2;

    /// <summary>
    /// When the fish should be a fry.
    /// Default is age 3.
    /// </summary>
    [DataField]
    public int FishAgeFry = 3;

    /// <summary>
    /// When the fish should be a fingerling. (juvenile)
    /// Default is age 4.
    /// </summary>
    [DataField]
    public int FishAgeFingerling = 4;

    /// <summary>
    /// When the fish should be an adult.
    /// Default is 5.
    /// </summary>
    [DataField]
    public int FishAgeAdult = 5;

    /// <summary>
    /// The current fish age. (duh)
    /// Default is set to 1 for obvious reasons.
    /// </summary>
    [DataField]
    public int CurrentFishAge = 1;

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
    /// The ammonia solution name.
    /// Default is 'eznutrient'. Surprising, I know.
    /// </summary>
    [DataField]
    public string AmmoniaSolutionName { get; set; } = "EZNutrient";

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
    /// The filter item ID.
    /// Default is 'TP14Filter'
    /// </summary>
    [DataField]
    public string FilterId = "TP14Filter";

    /// <summary>
    /// The component needed for the filter item.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FitsInFilterComponent? Filter;

    /// <summary>
    /// Input slot for the filter
    /// Default is set to "filterSlot"
    /// </summary>
    [DataField]
    public string InputSlotName = "filterSlot";
    #endregion
}
