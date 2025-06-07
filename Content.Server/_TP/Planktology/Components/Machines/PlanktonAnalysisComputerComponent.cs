using Content.Shared._TP.Planktology;
using Robust.Shared.Audio;

namespace Content.Server._TP.Planktology.Components.Machines;

/// <summary>
///     This allows something to analyze plankton samples.
/// </summary>
[RegisterComponent]
public sealed partial class PlanktonAnalysisComputerComponent : Component
{
    #region Sample
    [DataField]
    public PlanktonName ScannedSpeciesName { get; set; } = new("Unknown", "species");

    [DataField]
    public PlanktonDiet? ScannedDiet { get; set; }

    [DataField]
    public PlanktonCharacteristics ScannedCharacteristics { get; set; } = PlanktonCharacteristics.None;

    [DataField]
    public PlanktonSize? ScannedSize { get; set; }

    [DataField]
    public float ScannedTempRangeLow { get; set; } = 0.0f;

    [DataField]
    public float ScannedTempRangeHigh { get; set; } = 0.0f;
    #endregion

    /// <summary>
    ///     Whether a research paper has been generated for the current sample.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool PaperGenerated { get; set; } = false;

    /// <summary>
    ///     Time when the sample was first loaded (for tracking).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan SampleLoadTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    ///     The sound played when the computer extracts points from a sample.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExtractSound = new SoundPathSpecifier("/Audio/Effects/radpulse11.ogg")
    {
        Params = new AudioParams
        {
            Volume = 4,
        }
    };

    /// <summary>
    ///     The sound played when the computer prints a paper.
    /// </summary>
    [DataField]
    public SoundSpecifier? PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg")
    {
        Params = new AudioParams
        {
            Volume = 4,
        }
    };

    private (float Low, float High)? _scannedTempRange;
}
