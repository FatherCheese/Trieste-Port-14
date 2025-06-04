namespace Content.Server._TP.Aquaponics.Components;

[RegisterComponent]
public sealed partial class FishItemComponent : Component
{
    /// <summary>
    ///     Fish traits, copied over from FishComponent on item spawn.
    /// </summary>
    [DataField]
    public List<FishTraitData> Traits = new();
}
