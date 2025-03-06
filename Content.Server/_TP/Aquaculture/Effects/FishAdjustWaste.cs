using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server._TP.Aquaculture.Effects;

[UsedImplicitly]
public sealed partial class FishAdjustWaste : FishAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "fish-attribute-ammonia";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanFishMetabolize(args.TargetEntity, out var fishGrowerComp, args.EntityManager))
            return;

        fishGrowerComp.WasteAmount += Amount;
    }
}
