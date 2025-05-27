using System.Linq;
using Content.Server._TP.Aquaponics.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
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
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<TagPrototype> ScoopTag = "Scoop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AquacultureTankComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AquacultureTankComponent, InteractHandEvent>(OnInteractHand);
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
        if (comp.Fish.Count > 0)
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
            if (comp.Fish.Count < comp.MaxCapacity)
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

                    AgingTimeMin = fish.AgingTimeMin,
                    AgingTimeMax = fish.AgingTimeMax,
                    NextGrowth = _timing.CurTime,

                    WaterConsumption = fish.WaterConsumption,
                    FoodConsumption = fish.FoodConsumption,
                    WasteProduction = fish.WasteProduction
                };

                comp.Fish.Add(fishCopy);

                _popup.PopupCursor(Loc.GetString("fish-grower-component-egg-success-message",
                        ("eggName", fish.Species)),
                    args.User,
                    PopupType.Medium);

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

            // Public popup message
            _popup.PopupEntity(Loc.GetString("fish-grower-component-remove-fish-message",
                    ("otherName", Comp<MetaDataComponent>(args.User).EntityName),
                    ("fishName", comp.Fish[0].Species),
                    ("fishAge", comp.Fish[0].GrowthStage)),
                uid,
                Filter.PvsExcept(args.User),
                true);

            // Admin logging
            _adminLogger.Add(LogType.Botany,
                LogImpact.Low,
                $"{ToPrettyString(args.User):player} scooped out {comp.Fish[0].Species} at age {comp.Fish[0].GrowthStage} at Pos:{Transform(uid).Coordinates}.");

            if (comp.Fish[0].GrowthStage != FishGrowthStage.Adult)
            {
                // User-only popup message
                _popup.PopupCursor(Loc.GetString("fish-grower-component-fish-remove-message",
                        ("fishName", comp.Fish[0].Species),
                        ("fishAge", comp.Fish[0].GrowthStage)),
                    args.User,
                    PopupType.Medium);
            }
            else
            {
                // User-only popup message
                _popup.PopupCursor(Loc.GetString("fish-grower-component-fish-harvest-message",
                        ("fishName", comp.Fish[0].Species)),
                    args.User,
                    PopupType.Medium);

                // Spawn the harvest item
                Spawn(comp.Fish[0].ResultingItem, Transform(uid).Coordinates);
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
            foreach (var fish in comp.Fish.Where(fish => _timing.CurTime > fish.NextGrowth))
            {
                fish.NextGrowth += TimeSpan.FromSeconds(_random.NextFloat(fish.AgingTimeMin, fish.AgingTimeMax));
                fish.GrowthStage = TryAgeFish(fish);
                ChangeHealth(uid, fish);
            }

            comp.EggPrototype = TryBreedFish(comp);
        }
    }

    /// <summary>
    ///     The function to try and breed fish to produce eggs.
    /// </summary>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <returns>prototype ID (string)</returns>
    private string TryBreedFish(AquacultureTankComponent comp)
    {
        if (comp.Fish.Count <= 0)
        {
            return "";
        }

        if (comp.Fish.ElementAtOrDefault(0) == null || comp.Fish.ElementAtOrDefault(1) == null)
        {
            return "";
        }

        return comp.Fish[0].CompatibleTypes.TryGetValue(comp.Fish[1].FishType, out var result) ? result : "";
    }

    // TODO - Make health regen also based on traits.
    /// <summary>
    ///     Regenerates (or takes away) health from a fish.
    ///     This is based on how much water, food, and waste is in the tank.
    ///     This is also based on the fish's age.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="fish"></param>
    private void ChangeHealth(EntityUid uid, FishComponent fish)
    {
        if (!TryComp<AquacultureTankComponent>(uid, out var tank))
            return;

        if (fish.GrowthStage != FishGrowthStage.Adult && fish.Health < 100)
        {
            if (tank is { CurrentWater: >= 20, CurrentFood: >= 30, CurrentWaste: < 60 })
                fish.Health += 10;
            else
                fish.Health -= 15;
        }
        else
            fish.Health -= 15;
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
}
