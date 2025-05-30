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

    /// <summary>
    ///     A list of traits that this fish has.
    /// </summary>
    [DataField]
    public Dictionary<string, float> Traits = new();

    /// <summary>
    ///     Health. Enough said.
    /// </summary>
    [DataField]
    public float Health = 100.0f;
    #endregion

    #region Timing
    /// <summary>
    ///     A list of timers controlling things such as growth, eating, etc.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, TimeSpan> Timers = new();
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
    public bool CausesAberrant = false;
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
