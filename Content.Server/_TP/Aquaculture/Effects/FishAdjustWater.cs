using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server._TP.Aquaculture.Effects;

[UsedImplicitly]
public sealed partial class FishAdjustWater : FishAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "fish-attribute-water";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanFishMetabolize(args.TargetEntity, out var fishGrowerComp, args.EntityManager))
            return;

        fishGrowerComp.WaterAmount += Amount;
    }
}
