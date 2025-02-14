using Content.Server.Aquaculture.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Server.Aquaculture.Systems;

public sealed partial class AquacultureSystem
{
    public void ProduceGrown(EntityUid uid, FishProduceComponent produce)
    {
        if (!TryToGetEggs(produce, out var fishEgg))
            return;

        foreach (var mutation in fishEgg.Mutations)
        {
            if (!mutation.AppliesToProduce)
                continue;

            var args = new EntityEffectBaseArgs(uid, EntityManager);
            mutation.Effect.Effect(args);
        }

        if (!_solutionContainerSystem.EnsureSolution(uid,
                produce.SolutionName,
                out var solutionContainer,
                FixedPoint2.Zero))
            return;

        solutionContainer.RemoveAllSolution();
        foreach (var (chem, quantity) in fishEgg.Chemicals)
        {
            var amount = FixedPoint2.New(quantity.Min);
            if (quantity.PotencyDivisor > 0 && fishEgg.Potency > 0)
                amount += FixedPoint2.New(fishEgg.Potency / quantity.PotencyDivisor);
            amount = FixedPoint2.New(MathHelper.Clamp(amount.Float(), quantity.Min, quantity.Max));
            solutionContainer.MaxVolume += amount;
            solutionContainer.AddReagent(chem, amount);
        }
    }
}
