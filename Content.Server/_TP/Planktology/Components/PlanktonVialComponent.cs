namespace Content.Server._TP.Planktology.Components;

/// <summary>
///     This allows something to hold a single plankton colony.
/// </summary>
[RegisterComponent]
public sealed partial class PlanktonVialComponent : Component
{
    /// <summary>
    ///     The plankton colony currently in the container.
    ///     This is null by default, obviously.
    /// </summary>
    [DataField]
    public PlanktonInstance? Plankton { get; set; }

    /// <summary>
    ///     Whether to generate a plankton colony on round-start.
    /// </summary>
    [DataField]
    public bool PreGenerated = false;
}
