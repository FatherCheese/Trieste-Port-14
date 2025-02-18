using Content.Client.Aquaculture.Components;
using Content.Shared.Aquaculture;
using Robust.Client.GameObjects;

namespace Content.Client.Aquaculture;

public enum FishGrowerLayers : byte
{
    Plant,
    HealthLight,
    WaterLight,
    NutritionLight,
    AlertLight,
    HarvestLight,
}


public sealed class FishGrowerVisualsSystem : VisualizerSystem<FishGrowerVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FishGrowerVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, FishGrowerVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(FishGrowerLayers.Plant);
        sprite.LayerSetVisible(FishGrowerLayers.Plant, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, FishGrowerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid,
                FishGrowerVisuals.FishRsi,
                out var rsi,
                args.Component) ||
            !AppearanceSystem.TryGetData<string>(uid,
                FishGrowerVisuals.FishIconState,
                out var state,
                args.Component))
            return;

        var valid = !string.IsNullOrWhiteSpace(state);

        args.Sprite.LayerSetVisible(FishGrowerLayers.Plant, valid);

        if (!valid)
            return;

        args.Sprite.LayerSetRSI(FishGrowerLayers.Plant, rsi);
        args.Sprite.LayerSetState(FishGrowerLayers.Plant, state);
    }
}
