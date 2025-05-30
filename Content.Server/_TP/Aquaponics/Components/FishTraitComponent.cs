namespace Content.Server._TP.Aquaponics.Components;

[RegisterComponent]
public sealed partial class FishTraitComponent : Component
{
    #region Rates
    /// <summary>
    ///     How much water this fish consumes.
    /// </summary>
    [DataField]
    public float WaterConsumption = 0.2f;

    /// <summary>
    ///     How much food this fish consumes.
    /// </summary>
    [DataField]
    public float FoodConsumption = 0.5f;

    /// <summary>
    ///     How much waste this fish produces.
    /// </summary>
    [DataField]
    public float WasteProduction = 2.0f;

    /// <summary>
    ///     How much damage this fish takes, via multiplication.
    /// </summary>
    [DataField]
    public float Hardiness = 1.0f;

    /// <summary>
    ///     How fast this fish grows, via multiplication.
    /// </summary>
    [DataField]
    public float GrowthSpeed = 1.0f;
    #endregion
}
