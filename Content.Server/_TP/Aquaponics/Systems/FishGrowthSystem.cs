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

namespace Content.Server._TP.Aquaponics.Systems;

/// <summary>
///     The main system controlling the fish components.
///     This is split across multiple system files,
///     as we are NOT taking after botany's black-boxing!
/// </summary>
public sealed class FishGrowthSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    // Fish Growth specific
    [Dependency] private readonly FishGrowthVisualSystem _fishGrowthVisuals = default!;
    [Dependency] private readonly FishGrowthTankSystem _fishGrowthTankSystem = default!;

    private static readonly ProtoId<TagPrototype> ScoopTag = "Scoop";
    private Dictionary<string, string> _fishToEggMap = new(); // This is a bit messy, but I'm getting tired. - Cookie (FatherCheese)

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AquacultureTankComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AquacultureTankComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AquacultureTankComponent, ExaminedEvent>(OnExaminedEvent);
        SubscribeLocalEvent<AquacultureTankComponent, GetVerbsEvent<Verb>>(OnVerbsEvent);
        SubscribeLocalEvent<AquacultureTankComponent, SolutionTransferredEvent>(OnSolutionTransferred);

        BuildFishToEggMap();
    }

    /// <summary>
    ///     A function that adds every fishItem component to the egg prototype.
    ///     Adding an egg prototype to the egg directly seemed messy.
    /// </summary>
    private void BuildFishToEggMap()
    {
        foreach (var eggProto in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (eggProto.TryGetComponent<FishComponent>(out var fishComp))
                _fishToEggMap[fishComp.ResultingItem] = eggProto.ID;
        }
    }

    /// <summary>
    ///     Returns an egg from the fish-egg dictionary.
    /// </summary>
    /// <param name="fishItemId">string</param>
    /// <returns>Egg Prototype (string)</returns>
    public string? GetEggForFish(string fishItemId)
    {
        return _fishToEggMap.TryGetValue(fishItemId, out var eggId) ? eggId : null;
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

            if (comp.Fish[0].GeneticStability <= 0.5 && comp.Fish[0].Health > 0)
                args.PushMarkup(Loc.GetString("fish-grower-component-unstable"), 6);

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

            if (comp.Fish[1].GeneticStability <= 0.5 && comp.Fish[1].Health > 0)
                args.PushMarkup(Loc.GetString("fish-grower-component-unstable"), 6);

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
            _popup.PopupCursor(Loc.GetString("fish-grower-component-hands-message"),
                args.User,
                PopupType.Medium);

            args.Handled = true;
            return;
        }

        // Egg handling, called from the tank systems.
        // Also, an update sprite call for safety.
        _fishGrowthTankSystem.RemoveEggsFromTank(uid, comp, args);
        _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
        args.Handled = true;
        return;
    }

    /// <summary>
    ///     The function handling the interaction with the fish grower.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <param name="args">The item used on the UID</param>
    private void OnInteractUsing(EntityUid uid, AquacultureTankComponent comp, InteractUsingEvent args)
    {
        // Called if an item with the "fish" component, such as eggs,
        // is used on an aquaculture tank. This also handles
        // initializing the internal timers of the fish.
        // Also, an update sprite call for safety.
        if (TryComp(args.Used, out FishComponent? fish))
        {
            _fishGrowthTankSystem.AddFishToTank(uid, comp, fish, args);
            _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
            args.Handled = true;
            return;
        }

        // The scoop item, used for removing fish from an aquaculture tank.
        // It only does one at a time, removing from the list and moving the rest upward
        if (_tagSystem.HasTag(args.Used, ScoopTag))
        {
            // Null check and client popup message (user)
            if (comp.Fish.ElementAtOrDefault(0) == null)
            {
                _popup.PopupCursor(Loc.GetString("fish-grower-component-empty-message"),
                    args.User,
                    PopupType.Medium);

                args.Handled = true;
                return;
            }

            // Get the current fish at position 0 (or slot 1)
            // and then run the tank system remove function.
            // Also, again, an update sprite call for safety.
            var currFish = comp.Fish[0];
            _fishGrowthTankSystem.RemoveFishFromTank(uid, comp, currFish, args);
            _fishGrowthVisuals.UpdateSpriteForTank(uid, comp);
            args.Handled = true;
            return;
        }
    }

    /// <summary>
    ///     Returns a timer from the fish component via string.
    /// </summary>
    /// <param name="timer">String name</param>
    /// <param name="comp">Fish Component</param>
    /// <returns>TimeSpan (seconds)</returns>
    public TimeSpan GetRandomTimer(string timer, FishComponent comp)
    {
        if (comp.BaseTimerRanges.TryGetValue(timer, out var range) && range.Length >= 2)
            return TimeSpan.FromSeconds(_random.NextFloat(range[0], range[1]));

        return TimeSpan.FromSeconds(30); // fallback
    }
}
