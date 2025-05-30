using System.Linq;
using Content.Server._TP.Aquaponics.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._TP.Aquaponics.Systems;

/// <summary>
///     The system controlling the fish components.
/// </summary>
public sealed class FishGrowthSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Fish Growth specific
    [Dependency] private readonly FishGrowthVisualSystem _fishGrowthVisuals = default!;

    private static readonly ProtoId<TagPrototype> ScoopTag = "Scoop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AquacultureTankComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AquacultureTankComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AquacultureTankComponent, ExaminedEvent>(OnExaminedEvent);
        SubscribeLocalEvent<AquacultureTankComponent, GetVerbsEvent<Verb>>(OnVerbsEvent);
        SubscribeLocalEvent<AquacultureTankComponent, SolutionTransferredEvent>(OnSolutionTransferred);
    }

    private void OnSolutionTransferred(EntityUid uid, AquacultureTankComponent comp, SolutionTransferredEvent args)
    {
        _audio.PlayPvs(comp.WateringSound, uid);
    }

    /// <summary>
    ///     The context menu "verbs" event, for emptying waste.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <param name="args">Verb Arguments</param>
    private void OnVerbsEvent(EntityUid uid, AquacultureTankComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_itemSlots.TryGetSlot(uid, "beakerSlot", out var beakerSlot) || beakerSlot.Item == null)
            return;

        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionTankWaste, out _, out var wasteSol) || wasteSol.Volume <= 0)
            return;

        args.Verbs.Add(new Verb
        {
            Act = () =>
            {
                EjectWaste(uid, args);
                _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
            },
            Text = "Eject Waste",
            Priority = 1,
        });
    }

    /// <summary>
    ///     The verb function for ejecting waste from the tank.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="args">Verb Arguments</param>
    private void EjectWaste(EntityUid uid, GetVerbsEvent<Verb> args)
    {
        // Get the beaker slot, beaker solution, and then check if it has the solution component.
        if (!_itemSlots.TryGetSlot(uid, "beakerSlot", out var beakerSlot) || beakerSlot.Item == null)
        {
            _popup.PopupClient(Loc.GetString("fish-grower-component-beaker-missing"), args.User);
            return;
        }

        if (!TryComp<SolutionContainerManagerComponent>(beakerSlot.Item, out _))
            return;

        if (!_solutionContainer.TryGetSolution(beakerSlot.Item.Value, "beaker", out var beakerEntity, out var beakerSolution))
            return;

        // Now check if the tank has a waste solution.
        if (!_solutionContainer.TryGetSolution(uid, "tank_waste", out _, out var wasteSol))
            return;

        var transferAmount = FixedPoint2.Min(wasteSol.Volume, beakerSolution.AvailableVolume);
        if (transferAmount <= 0)
        {
            _popup.PopupClient(Loc.GetString("fish-grower-component-waste-empty-or-full"), args.User);
            return;
        }

        var wasteToTransfer = wasteSol.SplitSolution(transferAmount);
        _solutionContainer.TryAddSolution(beakerEntity.Value, wasteToTransfer);

        // Client popup message
        _popup.PopupClient(Loc.GetString("fish-grower-component-waste-emptied"), args.User);

        // Public popup message
        _popup.PopupEntity(Loc.GetString("fish-grower-component-waste-emptied-other",
                ("otherName", Comp<MetaDataComponent>(args.User).EntityName)),
            uid,
            Filter.PvsExcept(args.User),
            true);

        // Admin logging
        _adminLogger.Add(LogType.Botany,
            LogImpact.Low,
            $"{ToPrettyString(args.User):player} emptied tank waste at Pos:{Transform(uid).Coordinates}.");
    }

    /// <summary>
    ///     Description for the fish grower.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <param name="args">ExaminedEvent Arguments</param>
    private void OnExaminedEvent(EntityUid uid, AquacultureTankComponent comp, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("fish-grower-component-water-volume", ("waterVol", (int) comp.WaterLevel)), 10);
        args.PushMarkup(Loc.GetString("fish-grower-component-food-volume", ("foodVol", (int) comp.FoodLevel)), 10);

        if (comp.Fish.ElementAtOrDefault(0) != null)
        {
            if (comp.Fish[0].Health is <= 50 and > 0)
                args.PushMarkup(Loc.GetString("fish-grower-component-health"), 6);

            if (comp.Fish[0].Health <= 0)
                args.PushMarkup(Loc.GetString("fish-grower-component-dead"), 6);

            if (comp.Fish[0].GrowthStage is FishGrowthStage.Egg or FishGrowthStage.Fry)
            {
                args.PushMarkup(Loc.GetString("fish-grower-component-fish-young",
                    ("fish", comp.Fish[0].Species),
                    ("fishAge", comp.Fish[0].GrowthStage)),
                    5);
            }
            else
            {
                args.PushMarkup(Loc.GetString("fish-grower-component-fish",
                    ("fish", comp.Fish[0].Species),
                    ("fishAge", comp.Fish[0].GrowthStage)),
                    5);
            }
        }
        else
            args.PushMarkup(Loc.GetString("fish-grower-component-no-fish"), 6);

        if (comp.Fish.ElementAtOrDefault(1) != null)
        {
            if (comp.Fish[1].Health is <= 50 and > 0)
                args.PushMarkup(Loc.GetString("fish-grower-component-health"), 2);

            if (comp.Fish[1].Health <= 0)
                args.PushMarkup(Loc.GetString("fish-grower-component-dead"), 2);

            if (comp.Fish[1].GrowthStage is FishGrowthStage.Egg or FishGrowthStage.Fry)
            {
                args.PushMarkup(Loc.GetString("fish-grower-component-fish-young",
                    ("fish", comp.Fish[1].Species),
                    ("fishAge", comp.Fish[1].GrowthStage)),
                    1);
            }
            else
            {
                args.PushMarkup(Loc.GetString("fish-grower-component-fish",
                    ("fish", comp.Fish[1].Species),
                    ("fishAge", comp.Fish[1].GrowthStage)),
                    1);
            }
        }

        if (comp.EggPrototype != null)
            args.PushMarkup(Loc.GetString("fish-grower-component-eggs"), 9);

        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionTankWaste, out _, out var wasteSol))
            return;

        if (wasteSol.Volume > 70)
            args.PushMarkup(Loc.GetString("fish-grower-component-waste-volume"), 11);
    }

    /// <summary>
    ///     A function to handle the HAND interactions with the fish grower.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <param name="args">Hand Args</param>
    private void OnInteractHand(EntityUid uid, AquacultureTankComponent comp, InteractHandEvent args)
    {
        // Fish handling
        if (comp.Fish.Count > 0 && comp.EggPrototype == null)
        {
            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("fish-grower-component-hands-message"),
                args.User,
                PopupType.Medium);
            return;
        }

        // Egg handling
        if (comp.EggPrototype != null)
        {
            args.Handled = true;

            // user-only popup message
            _popup.PopupCursor(Loc.GetString("fish-grower-component-eggs-message",
                    ("fishEggName", comp.EggPrototype)),
                args.User,
                PopupType.Medium);

            // Public popup message
            _popup.PopupEntity(Loc.GetString("fish-grower-component-eggs-other-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                    ("fishEggName", comp.EggPrototype)),
                uid,
                Filter.PvsExcept(args.User),
                true);

            // Spawn the egg prototype
            Spawn(comp.EggPrototype, Transform(uid).Coordinates);

            // Admin logging
            _adminLogger.Add(LogType.Botany,
                LogImpact.Low,
                $"{ToPrettyString(args.User):player} harvested {comp.EggPrototype} at Pos:{Transform(uid).Coordinates}.");

            comp.EggPrototype = null;
        }
        else
        {
            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("fish-grower-component-empty-eggs-message"),
                args.User,
                PopupType.Medium);
        }
    }

    /// <summary>
    ///     The function handling the interaction with the fish grower.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <param name="args">The item used on the UID</param>
    private void OnInteractUsing(EntityUid uid, AquacultureTankComponent comp, InteractUsingEvent args)
    {
        // Egg 'planting' for growing components
        if (TryComp(args.Used, out FishComponent? fish))
        {
            if (comp.Fish.Count < comp.EggCapacityMax)
            {
                args.Handled = true;

                // This creates a fresh fish and then adds it to the Aquaculture Tank
                var fishCopy = new FishComponent()
                {
                    Species = fish.Species,
                    FishType = fish.FishType,
                    ResultingItem = fish.ResultingItem,
                    GrowthStage = fish.GrowthStage,
                    CompatibleTypes = fish.CompatibleTypes,
                    Traits = fish.Traits,
                    Health = 100,
                    Timers = fish.Timers
                };

                comp.Fish.Add(fishCopy);

                _popup.PopupCursor(Loc.GetString("fish-grower-component-egg-success-message",
                        ("eggName", fish.Species)),
                    args.User,
                    PopupType.Medium);

                _popup.PopupEntity(Loc.GetString("fish-grower-component-eggs-success-other-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                        ("eggName", fish.Species)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);

                // Delete the used item
                QueueDel(args.Used);

                // Admin logging
                _adminLogger.Add(LogType.Botany,
                    LogImpact.Low,
                    $"{ToPrettyString(args.User):player} put {fish.Species} egg at Pos:{Transform(uid).Coordinates}.");
                return;
            }

            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("fish-grower-component-full-message"),
                args.User,
                PopupType.Medium);

            return;
        }

        // The scoop item, used for removing fish from the tank
        // It only does one at a time, removing from the list and moving the rest upward
        if (_tagSystem.HasTag(args.Used, ScoopTag))
        {
            args.Handled = true;

            // Null check and user-only popup message
            if (comp.Fish.ElementAtOrDefault(0) == null)
            {
                _popup.PopupCursor(Loc.GetString("fish-grower-component-empty-message"),
                    args.User,
                    PopupType.Medium);

                return;
            }

            // Get the current fish at position 0 (or slot 1).
            var currFish = comp.Fish[0];

            // Public popup message
            _popup.PopupEntity(Loc.GetString("fish-grower-component-remove-fish-other-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                    ("fishName", currFish.Species),
                    ("fishAge", currFish.GrowthStage)),
                uid,
                Filter.PvsExcept(args.User),
                true);

            // Admin logging
            _adminLogger.Add(LogType.Botany,
                LogImpact.Low,
                $"{ToPrettyString(args.User):player} scooped out {currFish.Species} at age {currFish.GrowthStage} at Pos:{Transform(uid).Coordinates}.");

            if (currFish.GrowthStage != FishGrowthStage.Adult || currFish.Health <= 0)
            {
                // User-only popup message
                _popup.PopupCursor(Loc.GetString("fish-grower-component-remove-fish-message",
                        ("fishName", currFish.Species),
                        ("fishAge", currFish.GrowthStage)),
                    args.User,
                    PopupType.Medium);
            }
            else
            {
                // User-only popup message
                _popup.PopupCursor(Loc.GetString("fish-grower-component-fish-harvest-message",
                        ("fishName", currFish.Species)),
                    args.User,
                    PopupType.Medium);

                // Others popup message
                _popup.PopupEntity(Loc.GetString("fish-grower-component-fish-harvest-other-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                        ("fishName", currFish.Species)),
                    uid,
                    Filter.PvsExcept(args.User),
                    true);

                // Spawn the harvest item
                var fishItem = Spawn(currFish.ResultingItem, Transform(uid).Coordinates);

                if (TryComp<FishItemComponent>(fishItem, out var fishItemComp))
                    fishItemComp.Traits = new Dictionary<string, float>(currFish.Traits);

                _fishGrowthVisuals.UpdateSpriteForFish(uid, comp, currFish);
            }

            // Remove the fish at position 0 (or slot 1)
            // The RemoveAt function handles shuffling down.
            comp.Fish.RemoveAt(0);
        }
    }

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
                sol.SplitSolutionWithout(addRate, "Water", "EZNutrient");
                var includedSols = sol.SplitSolutionWithOnly(addRate, "Water", "EZNutrient");

                foreach (var sols in includedSols.Contents)
                {
                    if (sols.Reagent.Prototype == "Water")
                    {
                        comp.WaterLevel = Math.Min(comp.WaterLevel + sols.Quantity.Float(), comp.WaterLevelMax);
                        _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
                    }

                    if (sols.Reagent.Prototype == "EZNutrient")
                    {
                        comp.FoodLevel = Math.Min(comp.FoodLevel + sols.Quantity.Float(), comp.FoodLevelMax);
                        _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
                    }
                }
            }

            var curTime = _timing.CurTime;
            foreach (var fish in comp.Fish)
            {
                // Breeding (Adults only!)
                if (fish.Timers.TryGetValue("breed", out var breedVal) && curTime > breedVal)
                {
                    comp.EggPrototype = TryBreedFish(comp);
                    fish.Timers["breed"] = curTime + TimeSpan.FromMinutes(_random.NextFloat(2, 4));
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, comp, fish);
                }

                // Consumption
                if (fish.Timers.TryGetValue("consume", out var consumeVal) && curTime > consumeVal)
                {
                    ConsumeTankResources(uid, comp, fish);
                    fish.Timers["consume"] = curTime + TimeSpan.FromSeconds(_random.NextFloat(30, 90));
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, comp, fish);
                }

                // Growth
                if (fish.Timers.TryGetValue("growth", out var growthVal) && curTime > growthVal)
                {
                    fish.GrowthStage = TryAgeFish(fish);
                    fish.Timers["growth"] = curTime + TimeSpan.FromMinutes(_random.NextFloat(3, 6));
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, comp, fish);
                }

                // Healing
                if (fish.Timers.TryGetValue("health", out var healthVal) && curTime > healthVal)
                {
                    ChangeHealth(uid, comp, fish);
                    fish.Timers["health"] = curTime + TimeSpan.FromSeconds(_random.NextFloat(30, 90));
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, comp, fish);
                }
            }
        }
    }

    /// <summary>
    ///     The function handling the consumption of tank resources
    ///     and creation of waste.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <param name="fish">Fish Component</param>
    private void ConsumeTankResources(EntityUid uid, AquacultureTankComponent comp, FishComponent fish)
    {
        if (fish.GrowthStage == FishGrowthStage.Egg || fish.Health <= 0)
            return;

        var waterNeeded = fish.Traits.GetValueOrDefault("WaterConsumption", 0.2f);
        var foodNeeded = fish.Traits.GetValueOrDefault("FoodConsumption", 0.5f);
        var wasteCreated = fish.Traits.GetValueOrDefault("WasteProduction", 2.0f);
        AdjustWater(-waterNeeded, comp);
        AdjustFood(-foodNeeded, comp);
        AdjustWaste(uid, wasteCreated, comp);
    }

    /// <summary>
    ///     The function to try and breed fish to produce eggs.
    /// </summary>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <returns>Prototype ID (string)</returns>
    private string? TryBreedFish(AquacultureTankComponent comp)
    {
        // If the count is below 0 or if either is null, return an empty string
        if (comp.Fish.Count <= 0)
            return null;

        if (comp.Fish.ElementAtOrDefault(0) == null || comp.Fish.ElementAtOrDefault(1) == null)
            return null;

        // Check if both fish types are adult, then let them get down with it
        if (comp.Fish[0].GrowthStage != FishGrowthStage.Adult || comp.Fish[1].GrowthStage != FishGrowthStage.Adult)
            return null;

        return comp.Fish[0].CompatibleTypes.TryGetValue(comp.Fish[1].FishType, out var result) ? result : null;
    }

    /// <summary>
    ///     Regenerates (or takes away) health from a fish.
    ///     This is based on how much water, food, and waste is in the tank
    ///     as well as age and traits.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <param name="fish"></param>
    private void ChangeHealth(EntityUid uid, AquacultureTankComponent comp, FishComponent fish)
    {
        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionTankWaste, out _, out var wasteSol))
            return;

        var hardiness = fish.Traits.GetValueOrDefault("Hardiness", 1.0f);
        if (fish.GrowthStage != FishGrowthStage.Adult && fish.Health < 100)
        {
            if (comp is { WaterLevel: > 30, FoodLevel: > 20 } && wasteSol.Volume < 70)
                fish.Health += Math.Min(10 * hardiness, 100);
            else
            {
                if (fish.Health > 0)
                    fish.Health -= 10 * hardiness;
            }
        }
        else
        {
            if (fish.Health > 0)
                fish.Health -= 10 * hardiness;
        }

        _fishGrowthVisuals.UpdateSpriteForFish(uid, comp, fish);
    }

    /// <summary>
    ///     A function to try and age a fish in the fish grower
    ///     Goes from egg > fry > juvenile > adult, and another higher defaults to adult
    /// </summary>
    /// <param name="fish">Fish Component</param>
    /// <returns>FishGrowthStage (enum value)</returns>
    private static FishGrowthStage TryAgeFish(FishComponent fish)
    {
        return fish.GrowthStage switch
        {
            FishGrowthStage.Egg => FishGrowthStage.Fry,
            FishGrowthStage.Fry => FishGrowthStage.Juvenile,
            _ => FishGrowthStage.Adult
        };
    }

    public static void AdjustFood(float amount, AquacultureTankComponent comp)
    {
        comp.FoodLevel += amount;
    }

    public static void AdjustHealth(float amount, AquacultureTankComponent comp)
    {
        if (comp.Fish.Count < 0)
            return;

        if (comp.Fish.ElementAtOrDefault(0) != null)
            comp.Fish[0].Health += amount;

        if (comp.Fish.ElementAtOrDefault(1) != null)
            comp.Fish[1].Health += amount;
    }

    public static void AdjustWater(float amount, AquacultureTankComponent comp)
    {
        comp.WaterLevel += amount;
    }

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

        _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
    }
}
