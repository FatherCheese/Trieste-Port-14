using Content.Server._TP.Aquaponics.Components;
using Content.Server.Kitchen.Components;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;

namespace Content.Server._TP.Aquaponics.Systems;

public sealed class FishItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishItemComponent, AfterInteractUsingEvent>(OnButcherFish);
    }

    private void OnButcherFish(EntityUid uid, FishItemComponent comp, AfterInteractUsingEvent args)
    {
        if (!TryComp<ButcherableComponent>(args.Target, out var butcherable))
            return;

        if (!TryComp<SharpComponent>(args.Used, out var sharp))
            return;

        if (!sharp.Butchering.Add(args.Target.Value))
            return;

        foreach (var fishEgg in butcherable.SpawnedEntities)
        {
            var spawned = Spawn(fishEgg.PrototypeId, Transform(args.Target.Value).Coordinates);
            if (TryComp<FishComponent>(spawned, out var fishEggComp))
            {
                fishEggComp.Traits = new Dictionary<string, float>(comp.Traits);
            }

            QueueDel(args.Target.Value);
        }
    }
}
