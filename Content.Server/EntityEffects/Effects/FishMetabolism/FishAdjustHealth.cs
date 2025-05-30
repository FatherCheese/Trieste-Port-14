using Content.Server._TP.Aquaponics.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.FishMetabolism;

[UsedImplicitly]
public sealed partial class FishAdjustHealth : FishAdjustAttribute
{

    public override string GuidebookAttributeName { get; set; } = "fish-attribute-health";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if(!CanMetabolize(args.TargetEntity, out var fishHolderComp, args.EntityManager))
            return;

        FishGrowthSystem.AdjustHealth(Amount, fishHolderComp);
    }
}
