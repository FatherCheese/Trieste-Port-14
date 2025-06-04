using System.Linq;
using Content.Server._TP.Aquaponics.Components;
using Content.Shared._TP.Aquaponics;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._TP.Aquaponics.Systems;

public sealed class FishGrowthVisualSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AquacultureTankComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<AquacultureTankComponent, EntRemovedFromContainerMessage>(OnContainerModified);
    }

    /// <summary>
    ///     The function that updates the beaker sprite on the tank.
    /// </summary>
    /// <param name="uid">EntityUID</param>
    /// <param name="component">Aquaculture Tank Component</param>
    /// <param name="args">ContainerModified Arguments</param>
    private void OnContainerModified(EntityUid uid, AquacultureTankComponent component, ContainerModifiedMessage args)
    {
        var outputContainer = _itemSlots.GetItemOrNull(uid, "beakerSlot");
        _appearance.SetData(uid, SharedFishGrowerVisualState.BeakerAttached, outputContainer.HasValue);
    }

    /// <summary>
    ///     The function that updates the sprite of the tank for fish-related visuals.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    /// <param name="fish">Fish Component</param>
    public void UpdateSpriteForFish(EntityUid uid, AquacultureTankComponent comp, FishComponent fish)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, SharedFishGrowerVisuals.HealthState, fish.Health <= 50, appearance);

        _appearance.SetData(uid, SharedFishGrowerVisuals.AlertState, fish.Health <= 0 || fish.GeneticStability <= 0.5, appearance);

        _appearance.SetData(uid, SharedFishGrowerVisuals.HarvestState, fish.GrowthStage == FishGrowthStage.Adult, appearance);
    }

    /// <summary>
    ///     The function that updates the sprite of the tank for other visuals.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="comp">Aquaculture Tank Component</param>
    public void UpdateSpriteForTank(EntityUid uid, AquacultureTankComponent comp)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, SharedFishGrowerVisuals.WaterState, comp.WaterLevel <= 30, appearance);

        _appearance.SetData(uid, SharedFishGrowerVisuals.FoodState, comp.FoodLevel <= 20, appearance);

        // Before setting the appearance of the waste light,
        // make sure that the waste solution exists.
        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionTankWaste, out _, out var wasteSol))
            return;

        _appearance.SetData(uid, SharedFishGrowerVisuals.WasteState, wasteSol.Volume >= 70, appearance);
    }
}
