using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._TP.Aquaculture.Components;

/// <summary>
/// Similar to 'FitsInDispenserComponent'. This is just needed for the filter item.
/// </summary>

[RegisterComponent]
[NetworkedComponent] // only needed for white-lists. Client doesn't actually need Solution data;
public sealed partial class FitsInFilterComponent : Component
{
    /// <summary>
    /// Solution name that will interact with FishGrowerComponent.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";

    /// <summary>
    /// The reagent that needs to be present.
    /// </summary>
    [DataField(required: true)]
    public ReagentId Reagent = new();

    /// <summary>
    /// The max amount of reagent that can be present.
    /// Default is 100.
    /// </summary>
    [DataField]
    public int MaxReagentQuantity = 100;

    [DataField(required: true)]
    public ResPath FilterRsi { get; set; }
}
