using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Aquaculture.Components;
using Content.Shared.Botany;

namespace Content.Server.Aquaculture.Systems;

public sealed partial class AquacultureSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishEggComponent, ExaminedEvent>(OnExamined);
    }

    public bool TryToGetEggs(FishEggComponent comp, [NotNullWhen(true)] out FishEggData? seed)
    {
        if (comp.FishEgg != null)
        {
            seed = comp.FishEgg;
            return true;
        }

        if (comp.FishEggId != null
            && _prototypeManager.TryIndex(comp.FishEggId, out FishEggPrototype? protoSeed))
        {
            seed = protoSeed;
            return true;
        }

        seed = null;
        return false;
    }

    public bool TryToGetEggs(FishProduceComponent comp, [NotNullWhen(true)] out FishEggData? seed)
    {
        if (comp.FishEgg != null)
        {
            seed = comp.FishEgg;
            return true;
        }

        if (comp.FishEggId != null
            && _prototypeManager.TryIndex(comp.FishEggId, out FishEggPrototype? protoEgg))
        {
            seed = protoEgg;
            return true;
        }

        seed = null;
        return false;
    }

    private void OnExamined(EntityUid uid, FishEggComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryToGetEggs(component, out var seed))
            return;

        using (args.PushGroup(nameof(FishEggComponent)))
        {
            var name = Loc.GetString(seed.DisplayName);
            args.PushMarkup(Loc.GetString($"seed-component-description", ("seedName", name)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", seed.Yield)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", seed.Potency)));
        }
    }

    #region SeedPrototype prototype stuff

    /// <summary>
    /// Spawns fish eggs on the floor at a position, then tries to put it in the user's hands if possible.
    /// </summary>
    public EntityUid SpawnFishEggs(FishEggData proto, EntityCoordinates coords, EntityUid user, float? healthOverride = null)
    {
        var seed = Spawn(proto.FishEggsPrototype, coords);
        var seedComp = EnsureComp<FishEggComponent>(seed);
        seedComp.FishEgg = proto;
        seedComp.HealthOverride = healthOverride;

        var name = Loc.GetString(proto.Name);
        var val = Loc.GetString("botany-seed-packet-name", ("seedName", name));
        _metaData.SetEntityName(seed, val);

        // try to automatically place in user's other hand
        _hands.TryPickupAnyHand(user, seed);
        return seed;
    }

    public IEnumerable<EntityUid> AutoHarvest(FishEggData proto, EntityCoordinates position, int yieldMod = 1)
    {
        if (position.IsValid(EntityManager) &&
            proto.ProductPrototypes.Count > 0)
            return GenerateProduct(proto, position, yieldMod);

        return Enumerable.Empty<EntityUid>();
    }

    public IEnumerable<EntityUid> Harvest(FishEggData proto, EntityUid user, int yieldMod = 1)
    {
        if (proto.ProductPrototypes.Count == 0 || proto.Yield <= 0)
        {
            _popupSystem.PopupCursor(Loc.GetString("botany-harvest-fail-message"), user, PopupType.Medium);
            return Enumerable.Empty<EntityUid>();
        }

        var name = Loc.GetString(proto.DisplayName);
        _popupSystem.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);
        return GenerateProduct(proto, Transform(user).Coordinates, yieldMod);
    }

    public IEnumerable<EntityUid> GenerateProduct(FishEggData proto, EntityCoordinates position, int yieldMod = 1)
    {
        var totalYield = 0;
        if (proto.Yield > -1)
        {
            if (yieldMod < 0)
                totalYield = proto.Yield;
            else
                totalYield = proto.Yield * yieldMod;

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();

        if (totalYield > 1 || proto.HarvestRepeat != HarvestType.NoRepeat)
            proto.Unique = false;

        for (var i = 0; i < totalYield; i++)
        {
            var product = _robustRandom.Pick(proto.ProductPrototypes);

            var entity = Spawn(product, position);
            _randomHelper.RandomOffset(entity, 0.25f);
            products.Add(entity);

            // This should make it also drop fish eggs when harvested.
            var entityEggs = Spawn(proto.FishEggsPrototype, position);
            products.Add(entityEggs);

            var produce = EnsureComp<FishProduceComponent>(entityEggs);

            produce.FishEgg = proto;
            ProduceGrown(entityEggs, produce);

            _appearance.SetData(entityEggs, ProduceVisuals.Potency, proto.Potency);

            if (!proto.Mysterious)
                continue;

            var metaData = MetaData(entityEggs);
            _metaData.SetEntityName(entityEggs, metaData.EntityName + "?", metaData);
            _metaData.SetEntityDescription(entityEggs,
                metaData.EntityDescription + " " + Loc.GetString("botany-mysterious-description-addon"),
                metaData);
        }

        return products;
    }

    public bool CanHarvest(FishEggData proto, EntityUid? held = null)
    {
        return !proto.Ligneous || proto.Ligneous && held != null && HasComp<SharpComponent>(held);
    }

    #endregion
}
