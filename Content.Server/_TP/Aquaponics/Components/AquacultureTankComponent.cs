using Robust.Shared.Audio;

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
    public int EggCapacityMax = 2;

    #region Solutions
    /// <summary>
    ///     Container name of the general solutions, such as water and food.
    /// </summary>
    [DataField]
    public string SolutionTank = "tank_main";

    /// <summary>
    ///     Container name of the waste solution.
    /// </summary>
    [DataField]
    public string SolutionTankWaste = "tank_waste";

    [DataField]
    public float WaterLevel = 100.0f;

    [DataField]
    public float FoodLevel = 100.0f;

    [DataField]
    public float MutagenLevel;

    [DataField]
    public float WaterLevelMax = 100.0f;

    [DataField]
    public float FoodLevelMax = 100.0f;

    [DataField]
    public float MutagenLevelMax = 10.0f;

    [DataField]
    public SoundSpecifier? WateringSound;

    #endregion
}
