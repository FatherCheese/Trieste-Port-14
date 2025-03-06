using Content.Shared._TP.Aquaculture;
using Robust.Client.GameObjects;
using FishGrowerVisualsComponent = Content.Client._TP14.Aquaculture.Components.FishGrowerVisualsComponent;

namespace Content.Client._TP14.Aquaculture;

public enum FishGrowerLayers : byte
{
    Fish,
    HealthLight,
    WaterLight,
    NutritionLight,
    AlertLight,
    HarvestLight,
    FilterLight,
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

        sprite.LayerMapReserveBlank(FishGrowerLayers.Fish);
        sprite.LayerSetVisible(FishGrowerLayers.Fish, false);
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

        args.Sprite.LayerSetVisible(FishGrowerLayers.Fish, valid);

        if (!valid)
            return;

        args.Sprite.LayerSetRSI(FishGrowerLayers.Fish, rsi);
        args.Sprite.LayerSetState(FishGrowerLayers.Fish, state);
    }
}
