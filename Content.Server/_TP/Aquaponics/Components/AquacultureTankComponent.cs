namespace Content.Server._TP.Aquaponics.Components;

[RegisterComponent]
public sealed partial class AquacultureTankComponent : Component
{
    /// <summary>
    ///     The fish currently in the tank.
    /// </summary>
    [DataField]
    public List<FishComponent> Fish = new();

    /// <summary>
    ///     The egg produced via breeding.
    /// </summary>
    [DataField]
    public string? EggPrototype;

    /// <summary>
    ///     The total eggs and fish that can be held.
    /// </summary>
    [DataField]
    public int MaxCapacity = 2;

    /// <summary>
    ///     How much water is currently in the tank.
    /// </summary>
    [DataField("water", required: true)]
    public int CurrentWater = 100;

    /// <summary>
    ///     How much food is currently in the tank.
    /// </summary>
    [DataField("food", required: true)]
    public int CurrentFood = 100;

    /// <summary>
    ///     How much waste is currently in the tank.
    /// </summary>
    [DataField("waste")]
    public int CurrentWaste;
}
