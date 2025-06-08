using System.Numerics;

namespace Content.Server._TP.Planktology.Components.Machines;

/// <summary>
///     This allows something to separate plankton from the
///     SeaWater reagent. NOT the gas.
/// </summary>
[RegisterComponent]
public sealed partial class PlanktonSeparatorComponent : Component
{
    #region Timers
    /// <summary>
    ///     When the separator was last run.
    /// </summary>
    [DataField]
    public TimeSpan? LastRunTime { get; set; }

    /// <summary>
    ///     How many minutes between the separator runs.
    /// </summary>
    [DataField]
    public TimeSpan CooldownDuration { get; set; } = TimeSpan.FromMinutes(10);
    #endregion

    #region Distance
    /// <summary>
    /// Where the separator was last run (to enforce distance requirement)
    /// </summary>
    [DataField]
    public Vector2? LastRunPosition { get; set; }

    /// <summary>
    ///     The minimum distance from the last run position.
    /// </summary>
    [DataField]
    public float MinimumDistance { get; set; } = 50f;
    #endregion

    #region Plankton
    /// <summary>
    ///     A list of plankton UIDs currently in the separator.
    /// </summary>
    [DataField]
    public List<PlanktonInstance> StoredPlankton { get; set; } = new();

    /// <summary>
    ///     The maximum plankton that can be stored.
    /// </summary>
    [DataField]
    public int MaxStoredPlankton { get; set; } = 2;

    /// <summary>
    ///     The plankton created when the separator is used.
    ///     The default value is 2.
    /// </summary>
    [DataField]
    public int CreatedPlankton { get; set; } = 2;
    #endregion
}
