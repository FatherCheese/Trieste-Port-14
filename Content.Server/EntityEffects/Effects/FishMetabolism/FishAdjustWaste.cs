using Content.Server._TP.Aquaponics.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.FishMetabolism;

[UsedImplicitly]
public sealed partial class FishAdjustWaste : FishAdjustAttribute
{

    public override string GuidebookAttributeName { get; set; } = "fish-attribute-waste";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if(!CanMetabolize(args.TargetEntity, out var fishHolderComp, args.EntityManager))
            return;

        var fishHolder = args.EntityManager.System<FishGrowthSystem>();
        fishHolder.AdjustWaste(args.TargetEntity, Amount, fishHolderComp);
    }
}
