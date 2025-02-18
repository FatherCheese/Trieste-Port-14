using Content.Server.Aquaculture.Components;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Aquaculture;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Aquaculture.Systems;

/// <summary>
/// Gives the ability to grow fish.
/// </summary>
public sealed class FishGrowerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishGrowerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<FishGrowerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FishGrowerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FishGrowerComponent, SolutionTransferredEvent>(OnSolutionTransferred);
    }

    /// <summary>
    /// Sprite updater for the shared visual classes.
    /// </summary>
    /// <param name="uid"> EntityUid</param>
    /// <param name="comp"> FishGrowerComponent</param>
    private void UpdateSprite(EntityUid uid, FishGrowerComponent? comp)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (comp.FishEgg != null)
        {
            // Dead Plant
            if (comp.FishDead)
            {
                _appearance.SetData(uid, FishGrowerVisuals.FishRsi, comp.FishEgg.FishRsi.ToString(), app);
                _appearance.SetData(uid, FishGrowerVisuals.FishIconState, "dead", app);
            }
            else
            {
                if (comp.CurrentFishAge >= comp.FishAgeAdult)
                {
                    _appearance.SetData(uid, FishGrowerVisuals.FishRsi, comp.FishEgg.FishRsi.ToString(), app);
                    _appearance.SetData(uid, FishGrowerVisuals.FishIconState, "harvest", app);
                }
                else
                {
                    _appearance.SetData(uid, FishGrowerVisuals.FishRsi, comp.FishEgg.FishRsi.ToString(), app);
                    _appearance.SetData(uid, FishGrowerVisuals.FishIconState, $"stage-{comp.CurrentFishAge}", app);
                }
            }

            // Health Light
            _appearance.SetData(uid, FishGrowerVisuals.HealthLight, comp.FishHealth <= 50, app);

            // Harvest Light
            _appearance.SetData(uid, FishGrowerVisuals.HarvestLight, comp.CurrentFishAge >= comp.FishAgeAdult, app);
        }
        else
        {
            // These should disable the visuals on harvest, aka when the fish are null
            _appearance.SetData(uid, FishGrowerVisuals.FishIconState, "", app);
            _appearance.SetData(uid, FishGrowerVisuals.HealthLight, false, app);
            _appearance.SetData(uid, FishGrowerVisuals.HarvestLight, false, app);
        }

        // These don't require the fish itself, so are moved outside the null check.
        // Water Light
        _appearance.SetData(uid, FishGrowerVisuals.WaterLight, comp.WaterAmount <= 15, app);

        // Food Light
        _appearance.SetData(uid, FishGrowerVisuals.NutritionLight, comp.FoodAmount <= 10, app);

        // Warning Lights
        var warnings = comp.AmmoniaAmount >= 90;
        _appearance.SetData(uid, FishGrowerVisuals.WarningLight, warnings, app);
    }

    /// <summary>
    /// Harvesting attempt code. Will spawn 1 or 1-3 produce and eggs depending on the circumstances.
    /// </summary>
    /// <param name="uid"> EntityUid</param>
    /// <param name="spawnEntries"> A 'EntitySpawnEntry', used by FishProduced and EggsProduced</param>
    /// <param name="onlyOne"> Whether to only spawn one egg and produce.</param>
    private void TryToSpawnHarvest(EntityUid uid, List<EntitySpawnEntry> spawnEntries, bool onlyOne)
    {
        if (onlyOne)
        {
            foreach (var ent in EntitySpawnCollection.GetSpawns(spawnEntries, _random))
            {
                Spawn(ent, Transform(uid).Coordinates);
            }
        }
        else
        {
            foreach (var ent in EntitySpawnCollection.GetSpawns(spawnEntries, _random))
            {
                for (var i = 0; i < _random.Next(3) + 2; i++)
                {
                    Spawn(ent, Transform(uid).Coordinates);
                }
            }
        }
    }

    private void AdjustNutrient(EntityUid uid, float amount, FishGrowerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.FoodAmount += amount;
    }

    /// <summary>
    /// Interactions that require items; such as fish eggs, the scoop, and the filter.
    /// </summary>
    /// <param name="uid"> EntityUid</param>
    /// <param name="comp"> FishGrowerComponent</param>
    /// <param name="args"> InteractUsingEvent</param>
    private void OnInteractUsing(EntityUid uid, FishGrowerComponent? comp, InteractUsingEvent args)
    {
        // Resolve check
        if (!Resolve(uid, ref comp))
            return;

        // Putting fish in the container
        if (TryComp(args.Used, out FishEggComponent? fishEgg))
        {
            if (comp.FishEgg != null)
            {
                _popup.PopupCursor(Loc.GetString("action-popup-fishtank-planted-fail-message", ("owner", uid)),
                    args.User,
                    type: PopupType.Medium);
                return;
            }

            _popup.PopupCursor(Loc.GetString("action-popup-fishtank-planted-success-message", ("owner", uid)),
                args.User,
                PopupType.Medium);

            comp.FishEgg = fishEgg;
            comp.FishDead = false;
            comp.CurrentFishAge = comp.FishAgeEgg;
            comp.FishHealth = comp.FishHealthMax;

            args.Handled = true;

            QueueDel(args.Used);

            UpdateSprite(uid, comp);
            return;
        }

        // This checks if the held item is a fish net, and removes fish depending on the checks.
        if (_tagSystem.HasTag(args.Used, "Scoop"))
        {
            // If the fish eggs are null then don't scoop anything
            if (comp.FishEgg == null)
            {
                _popup.PopupCursor(Loc.GetString("action-popup-fishtank-scoop-none-message", ("owner", uid)),
                    args.User,
                    PopupType.Medium);

                _popup.PopupEntity(Loc.GetString("action-popup-fishtank-scoop-none-others-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);

                return;
            }

            // If the fish are too young then scoop them out but don't give anything
            if (comp.CurrentFishAge < comp.FishAgeFingerling)
            {
                _popup.PopupCursor(Loc.GetString("action-popup-fishtank-scoop-young-message"),
                    args.User,
                    PopupType.Medium);

                _popup.PopupEntity(Loc.GetString("action-popup-fishtank-scoop-young-others-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);

                comp.FishEgg = null;

                UpdateSprite(uid, comp);
                return;
            }

            // Or if the fish are dead just remove them
            if (comp.FishDead)
            {
                _popup.PopupCursor(Loc.GetString("action-popup-fishtank-scoop-dead-message"),
                    args.User,
                    PopupType.Medium);

                _popup.PopupEntity(Loc.GetString("action-popup-fishtank-scoop-dead-others-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);

                comp.FishEgg = null;
                comp.FishDead = false;

                UpdateSprite(uid, comp);
                return;
            }

            // Finally if the fish age is above or equal to 4 (juvenile)
            // If the fish age is 4 it will only spawn one produce and egg, otherwise it will spawn 1-4.
            if (comp.CurrentFishAge >= comp.FishAgeFingerling)
            {
                var onlySpawnOneHarvest = comp.CurrentFishAge == comp.FishAgeFingerling;
                TryToSpawnHarvest(uid, comp.FishEgg.EggsProduced, onlySpawnOneHarvest);
                TryToSpawnHarvest(uid, comp.FishEgg.FishProduced, onlySpawnOneHarvest);

                _popup.PopupCursor(Loc.GetString("action-popup-fishtank-scoop-success-message"),
                    args.User,
                    PopupType.Medium);

                _popup.PopupEntity(Loc.GetString("action-popup-fishtank-scoop-success-others-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);

                comp.FishEgg = null;

                UpdateSprite(uid, comp);
                return;
            }
        }

        if (TryComp<ProduceComponent>(args.Used, out var produce))
        {
            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("action-popup-fishtank-compost-message",
                    ("usingItem", args.Used),
                    ("owner", uid)),
                PopupType.Medium);

            _popup.PopupEntity(Loc.GetString("action-popup-fishtank-compost-others-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                    ("usingItem", args.Used),
                    ("owner", uid)),
                uid,
                Filter.PvsExcept(args.User),
                true);

            // Adding fluids to the container.
            if (_solution.TryGetSolution(args.Used, produce.SolutionName, out var solEnt, out var solution))
            {
                if (_solution.ResolveSolution(uid, comp.FoodSolutionName, ref comp.TotalSolution, out var solution2))
                {
                    var fillAmount = FixedPoint2.Min(solution.Volume, solution2.AvailableVolume);
                    _solution.TryAddSolution(comp.TotalSolution.Value,
                        _solution.SplitSolution(solEnt.Value, fillAmount));
                }
            }

            if (comp.FishEgg != null)
            {
                var nutrientBonus = comp.FishEgg.Potency / 2.5f;
                AdjustNutrient(uid, nutrientBonus, comp);
            }

            QueueDel(args.Used);
        }

        // Try to put the filter in
        // If it's not dirty then it succeeds
        if (TryComp<SolutionContainerManagerComponent>(args.Used, out _))
        {
            if (!_itemSlots.TryGetSlot(uid, comp.InputSlotName, out var slot))
                return;

            if (_solution.TryGetSolution(args.Used, "filter", out _, out var solution))
            {
                if (solution.Volume < solution.MaxVolume || solution.Volume == 0)
                {
                    _itemSlots.TryInsert(uid, slot, args.Used, args.User, true);
                }
            }
        }
    }

    /// <summary>
    /// The function to actually try and age the fish up.
    /// If age is above or equal to adult when aging; damage the component by 10.
    /// </summary>
    /// <param name="uid"> EntityUid</param>
    /// <param name="comp"> FishGrowerComponent</param>
    /// <returns></returns>
    private void TryToAgeFish(EntityUid uid, FishGrowerComponent? comp)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (comp.FishDead || comp.FishEgg == null)
            return;

        if (comp.CurrentFishAge > comp.FishAgeAdult)
            comp.FishHealth -= 10;

        comp.CurrentFishAge++;

        UpdateSprite(uid, comp);
    }

    /// <summary>
    /// Water and Food consumption.
    /// Ammonia creation.
    /// Health checking.
    /// </summary>
    /// <param name="uid"> EntityUid</param>
    /// <param name="comp"> FishGrowerComponent</param>
    private void ConsumeResources(EntityUid uid, FishGrowerComponent? comp)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (comp is { WaterAmount: > 10, FoodAmount: > 5, FishEgg: not null })
        {
            var healthMod = _random.Next(5, 10);

            // Health recovery
            if (comp.AmmoniaAmount < 90)
            {
                if (comp.FishHealth <= comp.FishHealthMax && comp.CurrentFishAge <= comp.FishAgeAdult)
                {
                    comp.FishHealth += (int)_random.Next(0.35f) * healthMod;
                    UpdateSprite(uid, comp);
                }
            }
            else
            {
                comp.FishHealth -= healthMod;
                UpdateSprite(uid, comp);
            }

            // Nutrient consumption
            if (comp.FoodUse > 0 && _random.Prob(0.75f))
            {
                comp.FoodAmount -= MathF.Max(0f, comp.FoodUse);
                UpdateSprite(uid, comp);
            }

            // Water consumption
            if (comp.WaterUse > 0 && _random.Prob(0.75f))
            {
                comp.WaterAmount -= MathF.Max(0f, comp.WaterUse);
                UpdateSprite(uid, comp);
            }

            // Ammonia creation
            if (comp.AmmoniaAmount < 100 && _random.Prob(0.75f))
            {
                comp.AmmoniaAmount += comp.AmmoniaCreation;
                UpdateSprite(uid, comp);
            }

            // Ammonia consumption (via filter)
            if (comp.AmmoniaAmount >= 10)
            {

                if (!_itemSlots.TryGetSlot(uid, comp.InputSlotName, out var slot))
                    return;

                if (slot.Item == null)
                    return;

                if (!TryComp<SolutionContainerManagerComponent>(slot.Item, out _))
                    return;

                if (!_solution.TryGetSolution(slot.Item.Value, "filter", out var solEnt, out var solution))
                    return;

                if (solution.Volume >= solution.MaxVolume)
                    return;

                comp.AmmoniaAmount -= 10;
                _solution.TryAddSolution(solEnt.Value, new Solution("EZNutrient", 1));
            }
        }
    }

    private void UpdateReagents(EntityUid uid, FishGrowerComponent? comp)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!_solution.ResolveSolution(uid, comp.FoodSolutionName, ref comp.TotalSolution, out var solution))
            return;

        if (solution.Volume <= 0)
            return;

        foreach (var entry in _solution.RemoveEachReagent(comp.TotalSolution.Value, 1))
        {
            var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
            reagentProto.ReactionFish(uid, entry, solution);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FishGrowerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.FishHealth <= 0)
                comp.FishDead = true;

            var curTime = _timing.CurTime;

            if (curTime < comp.LastCycle + comp.CycleDelay)
                return;

            comp.LastCycle = curTime;

            UpdateReagents(uid, comp);

            if (comp.NextFishGrowth <= _timing.CurTime)
            {
                comp.NextFishGrowth = _timing.CurTime + comp.FishGrowthDelay;

                comp.NextFishGrowth +=
                    TimeSpan.FromSeconds(_random.NextFloat(comp.FishGrowMin, comp.FishGrowMax));

                TryToAgeFish(uid, comp);
            }

            if (comp.NextConsumption <= _timing.CurTime)
            {
                comp.NextConsumption = _timing.CurTime + comp.ConsumptionDelay;

                comp.NextConsumption +=
                    TimeSpan.FromSeconds(_random.NextFloat(comp.ConsumeMin, comp.ConsumeMax));

                ConsumeResources(uid, comp);
            }
        }
    }

    /// <summary>
    /// Map initialization
    /// </summary>
    /// <param name="uid"> EntityUid</param>
    /// <param name="comp"> FishGrowerComponent</param>
    /// <param name="args"> MapInitEvent</param>
    private void OnMapInit(EntityUid uid, FishGrowerComponent comp, MapInitEvent args)
    {
        comp.NextFishGrowth = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(comp.FishGrowMin, comp.FishGrowMax));
    }

    private void OnSolutionTransferred(Entity<FishGrowerComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPvs(ent.Comp.SolutionSound, ent.Owner);
    }

    private void OnExamined(Entity<FishGrowerComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var comp = entity.Comp;

        using (args.PushGroup(nameof(FishGrowerComponent)))
        {
            if (comp.FishEgg == null)
            {
                args.PushMarkup(Loc.GetString("fishtank-fish-none-message"));
            }
            else
            {
                // This needs to be replaced with a name system!
                if (comp.FishDead)
                {
                    args.PushMarkup(Loc.GetString("fishtank-fish-dead-message"));
                }
                else
                {
                    // Jesus christ don't do this, I need to fix this! -Cookie
                    if (comp.CurrentFishAge == comp.FishAgeEgg)
                    {
                        args.PushMarkup(Loc.GetString("fishtank-fish-eggs-message"));
                    }
                    else if (comp.CurrentFishAge == comp.FishAgeEmbryo)
                    {
                        args.PushMarkup(Loc.GetString("fishtank-fish-embryo-message"));
                    }
                    else if (comp.CurrentFishAge == comp.FishAgeFry)
                    {
                        args.PushMarkup(Loc.GetString("fishtank-fish-fry-message"));
                    }
                    else if (comp.CurrentFishAge == comp.FishAgeFingerling)
                    {
                        args.PushMarkup(Loc.GetString("fishtank-fish-fingerling-message"));
                    }
                    else if (comp.CurrentFishAge == comp.FishAgeAdult)
                    {
                        args.PushMarkup(Loc.GetString("fishtank-fish-adult-message"));
                    }
                    else if (comp.CurrentFishAge > comp.FishAgeAdult)
                    {
                        args.PushMarkup(Loc.GetString("fishtank-fish-old-message"));
                    }
                }

                // Health
                if (comp.FishHealth <= 50)
                    args.PushMarkup(Loc.GetString("fishtank-fish-unhealthy-message"));
            }

            // Solutions
            args.PushMarkup(Loc.GetString("fishtank-water-message", ("waterLevel", comp.WaterAmount)));
            args.PushMarkup(Loc.GetString("fishtank-food-message", ("foodLevel", comp.FoodAmount)));

            // Ammonia
            args.PushMarkup(Loc.GetString("fishtank-ammonia-message", ("ammoniaLevel", comp.AmmoniaAmount)));

            // Filters
            if (!_itemSlots.TryGetSlot(entity, comp.InputSlotName, out var slot))
                return;

            if (!slot.HasItem)
                args.PushMarkup(Loc.GetString("fishtank-filter-none-message"));
            else
            {
                if (TryComp<SolutionContainerManagerComponent>(slot.Item, out _))
                {
                    if (_solution.TryGetSolution(slot.Item.Value, "filter", out _, out var solution))
                    {
                        args.PushMarkup(Loc.GetString("fishtank-filter-amount-message",
                            ("filterLevel", solution.Volume)));
                    }
                    else
                    {
                        args.PushMarkup(Loc.GetString("fishtank-filter-amount-message",
                            ("filterLevel", 0)));
                    }
                }
            }
        }
    }
}
