using Content.Server._TP.Aquaculture.Effects;
using Content.Server._TP.Aquaculture.Systems;
using Content.Shared.Storage;
using Robust.Shared.Utility;

namespace Content.Server._TP.Aquaculture.Components;

/// <summary>
/// This component handles adding different kind of fish roe/eggs.
/// </summary>
[RegisterComponent, Access(typeof(FishEggSystem),
     typeof(FishGrowerSystem),
     typeof(FishAdjustAttribute))]
public sealed partial class FishEggComponent : Component
{
    #region Appearance
    [DataField(required: true)]
    public ResPath FishRsi { get; set; } = default!;

    [DataField]
    public string FishIconState { get; set; } = "produce";
    #endregion

    #region Produce
    /// <summary>
    /// The 'produce' items that spawn upon a harvest.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> FishProduced { get; set; } = new();

    /// <summary>
    /// The 'produce' items that MIGHT spawn upon a harvest.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> ExtraProduced { get; set; } = new();

    /// <summary>
    /// The eggs that spawn upon a harvest.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> EggsProduced { get; set; } = new();

    /// <summary>
    /// The highest chance the extra produce can spawn at.
    /// Default is 4.
    /// </summary>
    [DataField]
    public int ExtraSpawnChance = 4;

    /// <summary>
    /// The minimum quantity of produce. (Both eggs and meat)
    /// Default is 1.
    /// </summary>
    [DataField]
    public int ProduceQuantityMin = 1;

    /// <summary>
    /// The maximum quantity of produce.
    /// Default is 3.
    /// </summary>
    [DataField]
    public int ProduceQuantityMax = 3;

    /// <summary>
    /// The minimum quantity of extra produce.
    /// Default is 1.
    /// </summary>
    [DataField]
    public int ExtraQuantityMin = 1;

    /// <summary>
    /// The maximum quantity of extra produce.
    /// Default is 3.
    /// </summary>
    [DataField]
    public int ExtraQuantityMax = 3;
    #endregion

    #region Solutions
    /// <summary>
    /// The waste solution that builds up inside Fish Growers.
    /// Default is 'EZNutrient'.
    /// </summary>
    [DataField]
    public string WasteSolutionName { get; set; } = "EZNutrient";

    /// <summary>
    /// The amount of water used. (per second?)
    /// </summary>
    [DataField]
    public float WaterUse = 0.5f;

    /// <summary>
    /// The amount of plant-food used. (per second?)
    /// </summary>
    [DataField]
    public float FoodUse = 0.75f;

    /// <summary>
    /// The amount of ammonia created. (per second?)
    /// </summary>
    [DataField]
    public float WasteCreation = 0.75f;
    #endregion

    #region Actual Fish
    /// <summary>
    /// At what age the fish should be considered an 'adult'
    /// </summary>
    [DataField(required: true)]
    public int FishAgeAdult;

    /// <summary>
    /// The 'produce' items that spawn upon a harvest.
    /// </summary>
    [DataField(required: true)]
    public string FishDescription = "";

    /// <summary>
    /// What age the fish currently is. This is for interacting with the growers.
    /// Default is 1. (egg)
    /// </summary>
    [DataField]
    public int CurrentFishAge = 1;

    /// <summary>
    /// The tier this Fish is.
    /// Default is 1.
    /// </summary>
    [DataField]
    public int FishTier = 1;

    /// <summary>
    /// The type of fish this fish is.
    /// Default is 'Jellidtype'.
    /// </summary>
    [DataField]
    public string FishType = "Jellidtype";
    #endregion

    #region Breeding
    /// <summary>
    /// Copied over from SeedPrototype.
    /// </summary>
    [DataField]
    public float Potency = 1f;

    /// <summary>
    /// Whether the fish makes eggs when harvested.
    /// Default is false.
    /// </summary>
    [DataField]
    public bool IsInfertile = false;

    /// <summary>
    /// Whether the fish's health is 'stable'.
    /// Default is false.
    /// </summary>
    [DataField]
    public bool IsUnstable = false;

    /// <summary>
    /// Whether the fish turns into a Drifter.
    /// Default is false.
    /// </summary>
    [DataField]
    public bool IsDrifter = false;
    #endregion
}
