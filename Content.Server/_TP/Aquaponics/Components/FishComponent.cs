namespace Content.Server._TP.Aquaponics.Components;

[RegisterComponent]
public sealed partial class FishComponent : Component
{
    #region Basic

    [DataField(required: true)]
    public string Species = "";

    [DataField(required: true)]
    public FishType FishType = FishType.Pisciform;

    [DataField("result", required: true)]
    public string ResultingItem = "";

    /// <summary>
    ///     The growth stage of the fish.
    ///     Default is "Egg"
    /// </summary>
    [DataField]
    public FishGrowthStage GrowthStage = FishGrowthStage.Egg;

    /// <summary>
    ///     The tier of the fish.
    ///     Default is "1"
    /// </summary>
    [DataField(required: true)]
    public int FishTier = 1;

    /// <summary>
    ///     The compatible breeding type, which returns the resulting species
    /// </summary>
    [DataField]
    public Dictionary<FishType, Dictionary<int, string>> CompatibleTypes = new();

    /// <summary>
    ///     Health. Enough said.
    /// </summary>
    [DataField]
    public float Health = 100.0f;

    #endregion

    # region Traits
    /// <summary>
    ///     A list of traits that this fish has.
    /// </summary>
    [DataField]
    public List<FishTraitData> Traits = new();

    /// <summary>
    ///     The base traits of the fish.
    /// </summary>
    [DataField("baseTraits")]
    public Dictionary<string, float> TraitsBase = new();

    /// <summary>
    ///     A weighted list of different, pre-made traits.
    ///     DO NOT ADD TO YML!!
    /// </summary>
    public static readonly List<FishTraitData> TraitWeights = new()
    {
        new FishTraitData
        {
            TraitName = "Hyperactive Metabolism",
            TraitTypes = new Dictionary<FishTraitType, float>
            {
                { FishTraitType.DeathRate, 2.0f }, // Faster
                { FishTraitType.FoodRate, 2.0f }, // Faster
                { FishTraitType.WasteRate, 2.0f }, // Faster
            }
        },
        new FishTraitData
        {
            TraitName = "Fast Aging",
            TraitTypes = new Dictionary<FishTraitType, float>
            {
                { FishTraitType.FoodRate, 2.0f }, // Faster
                { FishTraitType.GrowthRate, 0.5f }, // Slower
                { FishTraitType.WasteRate, 1.33f }, // Faster
                { FishTraitType.WaterRate, 1.33f } // Faster
            }
        },
        new FishTraitData
        {
            TraitName = "Fatty",
            TraitTypes = new Dictionary<FishTraitType, float>
            {
                { FishTraitType.EggRate, 2.0f }, // Slower
                { FishTraitType.FoodRate, 2.0f }, // Faster
                { FishTraitType.MeatRate, 3.0f } // Positive
            }
        },
        new FishTraitData
        {
            TraitName = "Slow Metabolism",
            TraitTypes = new Dictionary<FishTraitType, float>
            {
                { FishTraitType.DeathRate, 0.66f }, // Slower
                { FishTraitType.FoodRate, 0.5f }, // Slower
                { FishTraitType.WasteRate, 2.0f }, // Faster
                { FishTraitType.WaterRate, 2.0f }, // Faster

            }
        },
        new FishTraitData
        {
            TraitName = "Skinny",
            TraitTypes = new Dictionary<FishTraitType, float>
            {
                { FishTraitType.FoodRate, 0.5f }, // Slower
                { FishTraitType.EggRate, 0.5f }, // Faster
                { FishTraitType.MeatRate, 0.5f }, // Negative
            }
        },
        new FishTraitData
        {
            TraitName = "Sterile",
            TraitTypes = new Dictionary<FishTraitType, float>
            {
                { FishTraitType.DeathRate, 2.0f }, // Faster
                { FishTraitType.EggRate, 1000.0f } // Impossible to breed
            }
        }
    };
    #endregion

    #region Timing
    /// <summary>
    ///     Runtime timers. DO NOT ADD TO YML!!
    /// </summary>
    public readonly Dictionary<string, TimeSpan> Timers = new();

    /// <summary>
    ///     A list of timers controlling things such as growth, eating, etc.
    /// </summary>
    [DataField("timers", required: true)]
    public Dictionary<string, float[]> BaseTimerRanges = new();
    #endregion

    #region Miscellaneous
    /// <summary>
    ///     The prototype ID of the waste solution/reagent.
    /// </summary>
    [DataField]
    public string WasteProduct = "Ammonia";

    /// <summary>
    ///     Whether this fish causes aberrant damage when harvested.
    /// </summary>
    [DataField]
    public bool CausesAberrant;
    #endregion

    #region Stability
    /// <summary>
    ///     Fish's genetic stability - 0.0 to 1.0
    ///     0.0 is more dangerous, 1.0 is less dangerous.
    /// </summary>
    [DataField]
    public float GeneticStability = 1.0f;

    /// <summary>
    ///     Base threshold below which fish dies instantly
    /// </summary>
    [DataField]
    public float DeathThreshold = 0.2f;

    /// <summary>
    ///     How much a unit of Unstable Mutagen increases the death threshold.
    /// </summary>
    [DataField]
    public float MutagenPenalty = 0.05f;
    #endregion
}

public enum FishGrowthStage
{
    Egg,
    Fry,
    Juvenile,
    Adult
}

public enum FishType
{
    Pisciform,
    Crustacean,
    Jelliform,
    Aberrant,
    Mutant
}

[DataDefinition]
public partial struct FishTraitData
{
    /// <summary>
    ///     The trait name. Things such as "Fatty". "Hyperactive Metabolism", .etc.
    /// </summary>
    [DataField]
    public string TraitName = "";

    /// <summary>
    ///     The trait type.
    ///     See <see cref="FishTraitType"/> for more info.
    /// </summary>
    [DataField]
    public Dictionary<FishTraitType, float> TraitTypes = new();
}

public enum FishTraitType
{
    DeathRate,
    EggRate,
    FoodRate,
    GrowthRate,
    MeatRate,
    WasteRate,
    WaterRate
}
