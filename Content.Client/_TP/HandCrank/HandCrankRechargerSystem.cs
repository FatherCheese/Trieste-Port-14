using Content.Shared._TP.HandCrank;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Client._TP.HandCrank;

[Virtual]
public class HandCrankRechargerSystem : SharedHandCrankRechargerSystem
{
    // All logic done in server. This is only here for prediction.
    protected override void StartDoAfter(EntityUid uid, EntityUid user, HandCrankRechargerComponent component) {}
}
