using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.Aquaculture;

[UsedImplicitly]
public sealed partial class FishAdjustAmmonia : FishAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "fish-attribute-ammonia";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanFishMetabolize(args.TargetEntity, out var fishGrowerComp, args.EntityManager))
            return;

        fishGrowerComp.AmmoniaAmount += Amount;
    }
}
