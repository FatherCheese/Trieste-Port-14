using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.Aquaculture;

[UsedImplicitly]
public sealed partial class FishAdjustAge : FishAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "fish-attribute-age";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanFishMetabolize(args.TargetEntity, out var fishGrowerComp, args.EntityManager))
            return;

        fishGrowerComp.CurrentFishAge += (int) Amount;
    }
}
