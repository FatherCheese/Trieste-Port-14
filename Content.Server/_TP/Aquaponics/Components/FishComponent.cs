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
    ///     The compatible breeding type, which returns the resulting species
    /// </summary>
    [DataField]
    public Dictionary<FishType, string> CompatibleTypes = new();

    [DataField]
    public Dictionary<string, float> Traits = new();

    [DataField]
    public int Health = 100;
    #endregion

    #region Timing
    /// <summary>
    ///     Minimum cooldown for when to age the fish.
    /// </summary>
    [DataField]
    public float AgingTimeMin = 120f;

    /// <summary>
    ///     Maximum cooldown for when to age the fish.
    /// </summary>
    [DataField]
    public float AgingTimeMax = 240f;

    /// <summary>
    ///     When to next try to age.
    /// </summary>
    [DataField]
    public TimeSpan NextGrowth = TimeSpan.Zero;
    #endregion

    #region ConsumptionRates
    /// <summary>
    ///     How much water this fish consumes.
    /// </summary>
    [DataField]
    public float WaterConsumption = 0.3f;

    /// <summary>
    ///     How much food this fish consumes.
    /// </summary>
    [DataField]
    public float FoodConsumption = 0.5f;

    /// <summary>
    ///     How much waste this fish produces.
    /// </summary>
    [DataField]
    public float WasteProduction = 1.25f;
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
