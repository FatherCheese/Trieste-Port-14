using Content.Server.Aquaculture.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Aquaculture;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Aquaculture.Systems;

public sealed class AquacultureTankSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly AquacultureSystem _aquaculture = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly FishMutationSystem _mutation = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 2f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AquacultureTankComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<AquacultureTankComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AquacultureTankComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AquacultureTankComponent, SolutionTransferredEvent>(OnSolutionTransferred);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AquacultureTankComponent>();
        while (query.MoveNext(out var uid, out var aquacultureTank))
        {
            if (aquacultureTank.NextUpdate > _gameTiming.CurTime)
                continue;
            aquacultureTank.NextUpdate = _gameTiming.CurTime + aquacultureTank.UpdateDelay;

            Update(uid, aquacultureTank);
        }
    }

    private int GetCurrentAgeCycle(Entity<AquacultureTankComponent> entity)
    {
        var (_, component) = entity;

        if (component.FishEgg == null)
            return 0;

        var result = Math.Max(1, (int)(component.Age * component.FishEgg.GrowthStages / component.FishEgg.Maturation));
        return result;
    }

    private void OnExamine(Entity<AquacultureTankComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (_, component) = entity;

        using (args.PushGroup(nameof(AquacultureTankComponent)))
        {
            if (component.FishEgg == null)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
            }
            else if (!component.Dead)
            {
                var displayName = Loc.GetString(component.FishEgg.DisplayName);
                args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                    ("seedName", displayName),
                    ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

                if (component.Health <= component.FishEgg.Endurance / 2)
                {
                    args.PushMarkup(Loc.GetString(
                        "plant-holder-component-something-already-growing-low-health-message",
                        ("healthState",
                            Loc.GetString(component.Age > component.FishEgg.Lifespan
                                ? "plant-holder-component-plant-old-adjective"
                                : "plant-holder-component-plant-unhealthy-adjective"))));
                }
            }
            else
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message"));
            }

            if (component.AmmoniaLevel >= 90)
                args.PushMarkup(Loc.GetString("plant-holder-component-ammonia-high-level-message"));

            args.PushMarkup(Loc.GetString($"plant-holder-component-water-level-message",
                ("waterLevel", (int)component.WaterLevel)));
            args.PushMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int)component.NutritionLevel)));
            args.PushMarkup(Loc.GetString($"plant-holder-component-plankton-level-message",
                ("planktonLevel", (int)component.PlanktonLevel)));

            // Warning draws.
            if (!component.DrawWarnings)
                return;

            if (component.ImproperHeat)
                args.PushMarkup(Loc.GetString("plant-holder-component-heat-improper-warning"));
        }
    }

    private void OnInteractUsing(Entity<AquacultureTankComponent> entity, ref InteractUsingEvent args)
    {
        var (uid, component) = entity;

        if (TryComp(args.Used, out FishEggComponent? fishEggs))
        {
            if (component.FishEgg == null)
            {
                if (!_aquaculture.TryGetSeed(fishEggs, out var fishEgg))
                    return;

                args.Handled = true;
                var name = Loc.GetString(fishEgg.Name);
                _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                    ("seedName", name)),
                    args.User,
                    PopupType.Medium);

                component.FishEgg = fishEgg;
                component.Dead = false;
                component.Age = 1;
                component.Health = fishEggs.HealthOverride ?? component.FishEgg.Endurance;
                component.LastCycle = _gameTiming.CurTime;

                QueueDel(args.Used);

                CheckLevelSanity(uid, component);
                UpdateSprite(uid, component);

                return;
            }

            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-already-seeded-message",
                ("name", Comp<MetaDataComponent>(uid).EntityName)),
                args.User,
                PopupType.Medium);
            return;
        }

        // Scoop out eggs and fish when they're ready to harvest.
        if (_tagSystem.HasTag(args.Used, "Scoop"))
        {
            args.Handled = true;
            if (component.FishEgg != null)
            {
                if (!component.Dead)
                {
                    DoHarvest(uid, args.User, component);
                }
                else
                {
                    _popup.PopupCursor(Loc.GetString("plant-holder-component-remove-plant-message",
                            ("name", Comp<MetaDataComponent>(uid).EntityName)),
                        args.User,
                        PopupType.Medium);

                    _popup.PopupEntity(Loc.GetString("plant-holder-component-remove-plant-others-message",
                            ("name", Comp<MetaDataComponent>(args.User).EntityName)),
                        uid,
                        Filter.PvsExcept(args.User),
                        true);

                    RemoveFish(uid, component);
                }
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)),
                    args.User);
            }

            return;
        }

        if (HasComp<SharpComponent>(args.Used))
        {
            args.Handled = true;
            DoHarvest(uid, args.User, component);
            return;
        }

        // Composting.
        if (!TryComp<FishProduceComponent>(args.Used, out var produce))
            return;

        args.Handled = true;
        _popup.PopupCursor(Loc.GetString("plant-holder-component-compost-message",
            ("owner", uid),
            ("usingItem", args.Used)),
            args.User,
            PopupType.Medium);

        _popup.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
            ("user", Identity.Entity(args.User, EntityManager)),
            ("usingItem", args.Used),
            ("owner", uid)),
            uid,
            Filter.PvsExcept(args.User),
            true);

        if (_solutionContainerSystem.TryGetSolution(args.Used, produce.SolutionName, out var solComp, out var solution))
        {
            if (_solutionContainerSystem.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution2))
            {
                // We try to fit as much of the composted plant's contained solution into the hydroponics tray as we can,
                // since the plant will be consumed anyway.

                var fillAmount = FixedPoint2.Min(solution.Volume, solution2.AvailableVolume);
                _solutionContainerSystem.TryAddSolution(component.SoilSolution.Value,
                    _solutionContainerSystem.SplitSolution(solComp.Value, fillAmount));

                ForceUpdateByExternalCause(uid, component);
            }
        }
        var produceFishEgg = produce.FishEgg;
        if (produceFishEgg != null)
        {
            var nutrientBonus = produceFishEgg.Potency / 2.5f;
            AdjustNutrient(uid, nutrientBonus, component);
        }
        QueueDel(args.Used);
    }

    private void OnSolutionTransferred(Entity<AquacultureTankComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPvs(ent.Comp.WateringSound, ent.Owner);
    }
    private void OnInteractHand(Entity<AquacultureTankComponent> entity, ref InteractHandEvent args)
    {
        DoHarvest(entity, args.User, entity.Comp);
    }


    public void Update(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        UpdateReagents(uid, component);

        var curTime = _gameTiming.CurTime;

        if (component.ForceUpdate)
            component.ForceUpdate = false;
        else if (curTime < component.LastCycle + component.CycleDelay)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);
            return;
        }

        component.LastCycle = curTime;

        // Process mutations
        if (component.MutationLevel > 0)
        {
            Mutate(uid, Math.Min(component.MutationLevel, 25), component);
            component.UpdateSpriteAfterUpdate = true;
            component.MutationLevel = 0;
        }

        // If we have no seed planted, or the plant is dead, stop processing here.
        if (component.FishEgg == null || component.Dead)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);

            return;
        }

        // Advance plant age here.
        if (component.SkipAging > 0)
            component.SkipAging--;
        else
        {
            if (_random.Prob(0.8f))
                component.Age += (int)(1 * HydroponicsSpeedMultiplier);

            component.UpdateSpriteAfterUpdate = true;
        }

        // Nutrient consumption.
        if (component.FishEgg.NutrientConsumption > 0 && component.NutritionLevel > 0 && _random.Prob(0.75f))
        {
            component.NutritionLevel -= MathF.Max(0f, component.FishEgg.NutrientConsumption * HydroponicsSpeedMultiplier);

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        // Water consumption.
        if (component.FishEgg.WaterConsumption > 0 && component.WaterLevel > 0 && _random.Prob(0.75f))
        {
            component.WaterLevel -= MathF.Max(0f,
                component.FishEgg.WaterConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        // Plankton consumption.
        if (component.FishEgg.PlanktonConsumption > 0 && component.PlanktonLevel > 0 && _random.Prob(0.75f))
        {
            component.PlanktonLevel -= MathF.Max(0f,
                component.FishEgg.PlanktonConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        // Ammonia creation.
        if (component is { WaterLevel: > 0, PlanktonLevel: > 0, NutritionLevel: > 0 })
        {
            if (component.FishEgg.AmmoniaCreation > 0 && component.AmmoniaLevel < 100 && _random.Prob(0.75f))
            {
                component.AmmoniaLevel += MathF.Max(0f,
                    component.FishEgg.AmmoniaCreation * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);

                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }
        }

        var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

        // Make sure genetics are viable.
        if (!component.FishEgg.Viable)
        {
            AffectGrowth(uid, -1, component);
            component.Health -= 6 * healthMod;
        }

        // Prevents the plant from aging when lacking resources.
        // Limits the effect on aging so that when resources are added, the plant starts growing in a reasonable amount of time.
        if (component.SkipAging < 10)
        {
            // Make sure the fish is not starving.
            if (component is { NutritionLevel: > 5, PlanktonLevel: > 5 })
            {
                component.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth(uid, -1, component);
                component.Health -= healthMod;
            }

            // Make sure the fish has water.
            if (component.WaterLevel > 10)
            {
                component.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth(uid, -1, component);
                component.Health -= healthMod;
            }

            // Make sure the fish is not suffocating.
            if (component.AmmoniaLevel < 90)
            {
                component.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth(uid, -1, component);
                component.Health -= healthMod;
            }

            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }

        var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;

        component.MissingGas = 0;
        if (component.FishEgg.ConsumeGasses.Count > 0)
        {
            foreach (var (gas, amount) in component.FishEgg.ConsumeGasses)
            {
                if (environment.GetMoles(gas) < amount)
                {
                    component.MissingGas++;
                    continue;
                }

                environment.AdjustMoles(gas, -amount);
            }

            if (component.MissingGas > 0)
            {
                component.Health -= component.MissingGas * HydroponicsSpeedMultiplier;
                if (component.DrawWarnings)
                    component.UpdateSpriteAfterUpdate = true;
            }
        }

        if (component.Age > component.FishEgg.Lifespan)
        {
            component.Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
            if (component.DrawWarnings)
                component.UpdateSpriteAfterUpdate = true;
        }
        else if (component.Age < 0) // Revert back to seed packet!
        {
            var packetSeed = component.FishEgg;
            // will put it in the trays hands if it has any, please do not try doing this
            _aquaculture.SpawnSeedPacket(packetSeed, Transform(uid).Coordinates, uid);
            RemoveFish(uid, component);
            component.ForceUpdate = true;
            Update(uid, component);
            return;
        }

        CheckHealth(uid, component);

        if (component.Harvest && component.FishEgg.HarvestRepeat == HarvestType.SelfHarvest)
            AutoHarvest(uid, component);

        // If enough time has passed since the plant was harvested, we're ready to harvest again!
        if (!component.Dead && component.FishEgg.ProductPrototypes.Count > 0)
        {
            if (component.Age > component.FishEgg.Production)
            {
                if (component.Age - component.LastProduce > component.FishEgg.Production && !component.Harvest)
                {
                    component.Harvest = true;
                    component.LastProduce = component.Age;
                }
            }
            else
            {
                if (component.Harvest)
                {
                    component.Harvest = false;
                    component.LastProduce = component.Age;
                }
            }
        }

        CheckLevelSanity(uid, component);

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, component);
    }

    public void CheckLevelSanity(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.FishEgg != null)
            component.Health = MathHelper.Clamp(component.Health, 0, component.FishEgg.Endurance);
        else
        {
            component.Health = 0f;
            component.Dead = false;
        }

        component.MutationLevel = MathHelper.Clamp(component.MutationLevel, 0f, 100f);
        component.NutritionLevel = MathHelper.Clamp(component.NutritionLevel, 0f, 100f);
        component.WaterLevel = MathHelper.Clamp(component.WaterLevel, 0f, 100f);
        component.PlanktonLevel = MathHelper.Clamp(component.PlanktonLevel, 0f, 100f);
        component.AmmoniaLevel = MathHelper.Clamp(component.AmmoniaLevel, 0f, 100f);
        component.YieldMod = MathHelper.Clamp(component.YieldMod, 0, 2);
        component.MutationMod = MathHelper.Clamp(component.MutationMod, 0f, 3f);
    }

    public bool DoHarvest(EntityUid plantholder, EntityUid user, AquacultureTankComponent? component = null)
    {
        if (!Resolve(plantholder, ref component))
            return false;

        if (component.FishEgg == null || Deleted(user))
            return false;

        if (component is { Harvest: true, Dead: false })
        {
            if (TryComp<HandsComponent>(user, out var hands))
            {
                if (!_aquaculture.CanHarvest(component.FishEgg, hands.ActiveHandEntity))
                {
                    _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), user);
                    return false;
                }
            }
            else if (!_aquaculture.CanHarvest(component.FishEgg))
            {
                return false;
            }

            _aquaculture.Harvest(component.FishEgg, user, component.YieldMod);
            AfterHarvest(plantholder, component);
            return true;
        }

        if (!component.Dead)
            return false;

        RemoveFish(plantholder, component);
        AfterHarvest(plantholder, component);
        return true;
    }

    public void AutoHarvest(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.FishEgg == null || !component.Harvest)
            return;

        _aquaculture.AutoHarvest(component.FishEgg, Transform(uid).Coordinates);
        AfterHarvest(uid, component);
    }

    private void AfterHarvest(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Harvest = false;
        component.LastProduce = component.Age;

        if (component.FishEgg?.HarvestRepeat == HarvestType.NoRepeat)
            RemoveFish(uid, component);

        CheckLevelSanity(uid, component);
        UpdateSprite(uid, component);
    }

    public void CheckHealth(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Health <= 0)
        {
            Die(uid, component);
        }
    }

    public void Die(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Dead = true;
        component.Harvest = false;
        component.MutationLevel = 0;
        component.YieldMod = 1;
        component.MutationMod = 1;
        component.ImproperLight = false;
        component.ImproperHeat = false;
        component.ImproperPressure = false;
        UpdateSprite(uid, component);
    }

    public void RemoveFish(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.YieldMod = 1;
        component.MutationMod = 1;
        component.FishEgg = null;
        component.Dead = false;
        component.Age = 0;
        component.LastProduce = 0;
        component.Sampled = false;
        component.Harvest = false;
        component.ImproperLight = false;
        component.ImproperPressure = false;
        component.ImproperHeat = false;

        UpdateSprite(uid, component);
    }

    public void AffectGrowth(EntityUid uid, int amount, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.FishEgg == null)
            return;

        if (amount > 0)
        {
            if (component.Age < component.FishEgg.Maturation)
                component.Age += amount;
            else if (!component.Harvest && component.FishEgg.Yield <= 0f)
                component.LastProduce -= amount;
        }
        else
        {
            if (component.Age < component.FishEgg.Maturation)
                component.SkipAging++;
            else if (!component.Harvest && component.FishEgg.Yield <= 0f)
                component.LastProduce += amount;
        }
    }

    public void AdjustNutrient(EntityUid uid, float amount, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.NutritionLevel += amount;
    }

    public void AdjustWater(EntityUid uid, float amount, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.WaterLevel += amount;
    }

    public void AdjustPlankton(EntityUid uid, float amount, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.PlanktonLevel += amount;
    }

    public void AdjustAmmonia(EntityUid uid, float amount, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.AmmoniaLevel += amount;
    }

    public void UpdateReagents(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_solutionContainerSystem.ResolveSolution(uid, component.SoilSolutionName, ref component.SoilSolution, out var solution))
            return;

        if (solution.Volume > 0 && component.MutationLevel < 25)
        {
            var amt = FixedPoint2.New(1);
            foreach (var entry in _solutionContainerSystem.RemoveEachReagent(component.SoilSolution.Value, amt))
            {
                var reagentProto = _prototype.Index<ReagentPrototype>(entry.Reagent.Prototype);
                reagentProto.ReactionPlant(uid, entry, solution);
            }
        }

        CheckLevelSanity(uid, component);
    }

    private void Mutate(EntityUid uid, float severity, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.FishEgg != null)
        {
            EnsureUniqueSeed(uid, component);
            _mutation.MutateSeed(uid, ref component.FishEgg, severity);
        }
    }

    public void UpdateSprite(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.UpdateSpriteAfterUpdate = false;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (component.FishEgg != null)
        {
            if (component.DrawWarnings)
            {
                _appearance.SetData(uid, AquacultureTankVisuals.HealthLight, component.Health <= component.FishEgg.Endurance / 2f);
            }

            if (component.Dead)
            {
                _appearance.SetData(uid, AquacultureTankVisuals.FishRsi, component.FishEgg.PlantRsi.ToString(), app);
                _appearance.SetData(uid, AquacultureTankVisuals.FishIconState, "dead", app);
            }
            else if (component.Harvest)
            {
                _appearance.SetData(uid, AquacultureTankVisuals.FishRsi, component.FishEgg.PlantRsi.ToString(), app);
                _appearance.SetData(uid, AquacultureTankVisuals.FishIconState, "harvest", app);
            }
            else if (component.Age < component.FishEgg.Maturation)
            {
                var growthStage = GetCurrentAgeCycle((uid, component));

                _appearance.SetData(uid, AquacultureTankVisuals.FishRsi, component.FishEgg.PlantRsi.ToString(), app);
                _appearance.SetData(uid, AquacultureTankVisuals.FishIconState, $"stage-{growthStage}", app);
                component.LastProduce = component.Age;
            }
            else
            {
                _appearance.SetData(uid, AquacultureTankVisuals.FishRsi, component.FishEgg.PlantRsi.ToString(), app);
                _appearance.SetData(uid, AquacultureTankVisuals.FishIconState, $"stage-{component.FishEgg.GrowthStages}", app);
            }
        }
        else
        {
            _appearance.SetData(uid, AquacultureTankVisuals.FishIconState, "", app);
            _appearance.SetData(uid, AquacultureTankVisuals.HealthLight, false, app);
        }

        if (!component.DrawWarnings)
            return;

        _appearance.SetData(uid, AquacultureTankVisuals.WaterLight, component.WaterLevel <= 15, app);
        _appearance.SetData(uid, AquacultureTankVisuals.NutritionLight, component.NutritionLevel <= 8, app);
        _appearance.SetData(uid, AquacultureTankVisuals.PlanktonLight, component.PlanktonLevel <= 8, app);
        _appearance.SetData(uid, AquacultureTankVisuals.HarvestLight, component.Harvest, app);
    }

    /// <summary>
    ///     Check if the currently contained seed is unique. If it is not, clone it so that we have a unique seed.
    ///     Necessary to avoid modifying global seeds.
    /// </summary>
    public void EnsureUniqueSeed(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.FishEgg is { Unique: false })
            component.FishEgg = component.FishEgg.Clone();
    }

    public void ForceUpdateByExternalCause(EntityUid uid, AquacultureTankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.SkipAging++; // We're forcing an update cycle, so one age hasn't passed.
        component.ForceUpdate = true;
        Update(uid, component);
    }
}
