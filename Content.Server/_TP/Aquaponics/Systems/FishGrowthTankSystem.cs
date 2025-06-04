using System.Linq;
using Content.Server._TP.Aquaponics.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._TP.Aquaponics.Systems;

public sealed class FishGrowthTankSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Fish Growth specifics
    [Dependency] private readonly FishGrowthVisualSystem _fishGrowthVisuals = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<AquacultureTankComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_solutionContainer.TryGetSolution(uid, comp.SolutionTank, out _, out var sol))
            {
                const float addRate = 0.2f;

                // Erase all fluids that the tank doesn't need.
                // Then process the rest.
                sol.SplitSolutionWithout(addRate, "Water", "EZNutrient", "UnstableMutagen");
                var includedSols = sol.SplitSolutionWithOnly(addRate, "Water", "EZNutrient", "UnstableMutagen");

                foreach (var sols in includedSols.Contents)
                {
                    if (sols.Reagent.Prototype == "Water")
                    {
                        AdjustWater(sols.Quantity.Float(), comp);
                        _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
                    }

                    if (sols.Reagent.Prototype == "EZNutrient")
                    {
                        AdjustFood(sols.Quantity.Float(), comp);
                        _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
                    }

                    if (sols.Reagent.Prototype == "UnstableMutagen")
                    {
                        AdjustMutagenLevel(sols.Quantity.Float(), comp);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     A function that adds a fish to the tank,
    ///     and initialized the fish's timer.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="tankComp"></param>
    /// <param name="fishComp"></param>
    /// <param name="args">InteractUsingEvent Arguments</param>
    public void AddFishToTank(EntityUid uid, AquacultureTankComponent tankComp, FishComponent fishComp, InteractUsingEvent args)
    {
        // Check if the tank is full and send a message, then return.
        // We only want at least TWO fish.
        if (tankComp.Fish.Count >= tankComp.EggCapacityMax)
        {
            _popup.PopupCursor(Loc.GetString("fish-grower-component-full-message"),
                args.User,
                PopupType.Medium);

            return;
        }

        // Make a copy of the fish before adding it to the tank,
        // as well as deep-copy the collections of the component.
        // This is all necessary to prevent issues with data saving and what-not.
        var fishCopy = new FishComponent()
        {
            Species = fishComp.Species,
            FishType = fishComp.FishType,
            FishTier = fishComp.FishTier,
            ResultingItem = fishComp.ResultingItem,
            GrowthStage = fishComp.GrowthStage,
            GeneticStability = fishComp.GeneticStability,
            DeathThreshold = fishComp.DeathThreshold,

            // Deep-copy collections
            CompatibleTypes = fishComp.CompatibleTypes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Traits = fishComp.Traits.Select(t => new FishTraitData
                {
                    TraitName = t.TraitName,
                    TraitTypes = t.TraitTypes
                })
            .ToList(),

            TraitsBase = fishComp.TraitsBase.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            BaseTimerRanges = fishComp.BaseTimerRanges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        // Initialize timers and add the fish to the tank.
        InitializeFishTimers(fishCopy);
        tankComp.Fish.Add(fishCopy);

        // client-only popup message (user)
        _popup.PopupCursor(Loc.GetString("fish-grower-component-egg-success-message",
                ("eggName", fishCopy.Species)),
            args.User,
            PopupType.Medium);

        // public popup message (other users)
        _popup.PopupEntity(Loc.GetString("fish-grower-component-eggs-success-other-message",
                ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                ("eggName", fishCopy.Species)),
            uid,
            Filter.PvsExcept(args.User),
            true);

        // Admin logging
        _adminLogger.Add(LogType.Botany,
            LogImpact.Low,
            $"{ToPrettyString(args.User):player} put {fishCopy.Species} egg at Pos:{Transform(uid).Coordinates}.");

        QueueDel(args.Used);
        args.Handled = true;
    }

    /// <summary>
    ///     A function to initialize the fish's timers from the base timer ranges.
    /// </summary>
    /// <param name="fishComp"></param>
    private void InitializeFishTimers(FishComponent fishComp)
    {
        var curTime = _timing.CurTime;
        foreach (var (timerType, range) in fishComp.BaseTimerRanges)
        {
            if (range.Length >= 2)
            {
                var randomDelay = Random.Shared.NextSingle() * (range[1] - range[0]) + range[0];
                fishComp.Timers[timerType] = curTime + TimeSpan.FromSeconds(randomDelay);
            }
        }
    }

    /// <summary>
    ///     The function handling the removal of fish from
    ///     an aquaculture tank
    /// </summary>
    /// <param name="uid">Entity UID (Aquaculture Tank)</param>
    /// <param name="tankComp">AquacultureTankComponent</param>
    /// <param name="fishComp">FishComponent</param>
    /// <param name="args">InteractUsingEvent Arguments</param>
    public void RemoveFishFromTank(EntityUid uid,
        AquacultureTankComponent tankComp,
        FishComponent fishComp,
        InteractUsingEvent args)
    {
        // Admin logging
        // Doing this at the start this time, just so I don't
        // have to copy the same code. - Cookie (FatherCheese)
        _adminLogger.Add(LogType.Botany,
            LogImpact.Low,
            $"{ToPrettyString(args.User):player} scooped out {fishComp.Species} at age {fishComp.GrowthStage} at Pos:{Transform(uid).Coordinates}.");

        if (fishComp.GrowthStage != FishGrowthStage.Adult || fishComp.Health <= 0)
        {
            // client-only popup message (user)
            _popup.PopupCursor(Loc.GetString("fish-grower-component-remove-fish-message",
                    ("fishName", fishComp.Species),
                    ("fishAge", fishComp.GrowthStage)),
                args.User,
                PopupType.Medium);

            // public popup message (other users)
            _popup.PopupEntity(Loc.GetString("fish-grower-component-remove-fish-other-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                    ("eggName", fishComp.Species)),
                uid,
                Filter.PvsExcept(args.User),
                true);

            tankComp.Fish.RemoveAt(0);
            args.Handled = true;
            return;
        }

        // A deep-copy is needed for this as well.
        // I wasn't sure at first, but on second thought it's probably necessary. - Cookie (FatherCheese)
        var spawnedItem = Spawn(fishComp.ResultingItem, Transform(uid).Coordinates);
        if (TryComp<FishItemComponent>(spawnedItem, out var fishItemComp))
        {
            fishItemComp.Traits = fishComp.Traits.Select(t => new FishTraitData()
                {
                    TraitName = t.TraitName,
                    TraitTypes = t.TraitTypes
                })
                .ToList();
        }

        // Client-only popup message (user)
        _popup.PopupCursor(Loc.GetString("fish-grower-component-fish-harvest-message",
                ("fishName", fishComp.Species)),
            args.User,
            PopupType.Medium);

        // public popup message (other users)
        _popup.PopupEntity(Loc.GetString("fish-grower-component-fish-harvest-other-message",
                ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                ("fishName", fishComp.Species)),
            uid,
            Filter.PvsExcept(args.User),
            true);

        tankComp.Fish.RemoveAt(0);
        args.Handled = true;
        return;
    }

    /// <summary>
    ///     The function handling the removal of eggs from
    ///     an aquaculture tank.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">AquacultureTankComponent</param>
    /// <param name="args">InteractHandEvent Arguments</param>
    public void RemoveEggsFromTank(EntityUid uid, AquacultureTankComponent comp, InteractHandEvent args)
    {
        // Nullability check.
        // It shouldn't be null, but Rider is dumb. - Cookie (FatherCheese)
        if (comp.EggPrototype == null)
        {
            _popup.PopupCursor(Loc.GetString("fish-grower-component-empty-eggs-message"),
                args.User,
                PopupType.Medium);

            args.Handled = true;
            return;
        }

        // Spawn the egg prototype.
        Spawn(comp.EggPrototype, Transform(uid).Coordinates);

        // Client-only popup message (user)
        _popup.PopupCursor(Loc.GetString("fish-grower-component-eggs-message",
                ("fishEggName", comp.EggPrototype)),
            args.User,
            PopupType.Medium);

        // Public popup message (other users)
        _popup.PopupEntity(Loc.GetString("fish-grower-component-eggs-other-message",
                ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                ("fishEggName", comp.EggPrototype)),
            uid,
            Filter.PvsExcept(args.User),
            true);

        // Admin logging
        _adminLogger.Add(LogType.Botany,
            LogImpact.Low,
            $"{ToPrettyString(args.User):player} harvested {comp.EggPrototype} at Pos:{Transform(uid).Coordinates}.");

        // Now remove the egg from the tank and set handled to true.
        comp.EggPrototype = null;
        args.Handled = true;
    }

    /// <summary>
    ///     Adjust the tank's food level.
    /// </summary>
    /// <param name="amount">Amount to add or subtract (float)</param>
    /// <param name="comp">AquacultureTankComponent</param>
    public void AdjustFood(float amount, AquacultureTankComponent comp)
    {
        comp.FoodLevel = amount > 0 ? Math.Min(comp.FoodLevel + amount, comp.FoodLevelMax) : Math.Max(comp.FoodLevel + amount, 0);
    }

    /// <summary>
    ///     Adjust the tank's water level.
    /// </summary>
    /// <param name="amount">Amount to add or subtract (float)</param>
    /// <param name="comp">AquacultureTankComponent</param>
    public void AdjustWater(float amount, AquacultureTankComponent comp)
    {
        comp.WaterLevel = amount > 0 ? Math.Min(comp.WaterLevel + amount, comp.WaterLevelMax) : Math.Max(comp.WaterLevel + amount, 0);
    }

    /// <summary>
    ///     Adjust the tank's waste level.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="amount">Amount to add or subtract (float)</param>
    /// <param name="comp">AquacultureTankComponent</param>
    public void AdjustWaste(EntityUid uid, float amount, AquacultureTankComponent comp)
    {
        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionTankWaste, out _, out var wasteSol))
            return;

        if (comp.Fish.Count < 0)
            return;

        if (comp.Fish.ElementAtOrDefault(0) != null && comp.Fish[0].GrowthStage > FishGrowthStage.Egg)
        {
            var wasteOne = comp.Fish[0].WasteProduct;
            wasteSol.AddReagent(wasteOne, amount);
        }

        if (comp.Fish.ElementAtOrDefault(1) != null && comp.Fish[1].GrowthStage > FishGrowthStage.Egg)
        {
            var wasteTwo = comp.Fish[1].WasteProduct;
            wasteSol.AddReagent(wasteTwo, amount);
        }
    }

    /// <summary>
    ///     Adjust the tank's mutagen level.
    /// </summary>
    /// <param name="amount">Amount to add or subtract (Float)</param>
    /// <param name="comp">AquacultureTankComponent</param>
    public void AdjustMutagenLevel(float amount, AquacultureTankComponent comp)
    {
        comp.MutagenLevel = amount > 0 ? Math.Min(comp.MutagenLevel + amount, comp.MutagenLevelMax) : Math.Max(comp.MutagenLevel + amount, 0);
    }
}
