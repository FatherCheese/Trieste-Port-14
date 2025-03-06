using System.Linq;
using Content.Server._TP.Aquaculture.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._TP.Aquaculture;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._TP.Aquaculture.Systems;

public sealed class FishOMaticSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishOMaticComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, FishOMaticComponent? comp, InteractUsingEvent args)
    {
        // Resolve check
        if (!Resolve(uid, ref comp))
            return;

        if (TryComp(args.Used, out FishEggComponent? _))
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, comp.InputContainerId);

            if (inputContainer.ContainedEntities.Count > comp.StorageMaxEntities)
                return;

            if (!_containerSystem.Insert(args.Used, inputContainer))
                return;

            args.Handled = true;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<FishOMaticComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime > _gameTiming.CurTime)
                continue;

            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, comp.InputContainerId);
            var outputContainer = _itemSlots.GetItemOrNull(uid, comp.BeakerSlotId);
            if (outputContainer is null || !_solutionContainer.TryGetFitsInDispenser(outputContainer.Value,
                    out _,
                    out var containerSolution))
                continue;

            if (containerSolution.Volume <= 0 || containerSolution.Name != comp.MutatorSolutionName)
                return;

            foreach (var containedItem in inputContainer.ContainedEntities.ToList())
            {
                var extract = comp.FishOMatic switch
                {
                    SharedFishOMatic.Extract => GetFishExtract(containedItem),
                    SharedFishOMatic.Mutate => GetFishMutation(containedItem),
                    _ => null,
                };

                if (extract == null)
                    return;

                if (!this.IsPowered(uid, EntityManager))
                    continue;

                _audio.PlayPvs(comp.MutateSound, uid);
                QueueDel(containedItem);
                Spawn(extract.ToString(), Transform(uid).Coordinates);
            }
        }
    }

    private object? GetFishExtract(EntityUid uid)
    {
        return TryComp<FishEggComponent>(uid, out var fishEgg) ? fishEgg.EggsProduced : null;
    }

    private object? GetFishMutation(EntityUid uid)
    {
        if (!TryComp<FishOMaticComponent>(uid, out var machine))
            return null;

        var inputContainer = _containerSystem.EnsureContainer<Container>(uid, machine.InputContainerId);
        foreach (var outcome in machine.FishEggOutcome)
        {
            foreach (var item in inputContainer.ContainedEntities.ToList())
            {
                if (!TryComp<FishEggComponent>(item, out var fishEgg))
                    return null;

                if (fishEgg.FishTier == outcome.Key.Tier && fishEgg.FishType == outcome.Key.Type)
                {
                    return outcome.Value;
                }
            }
        }

        return null;
    }
}
