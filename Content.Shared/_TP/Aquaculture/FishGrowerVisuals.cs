using Robust.Shared.Serialization;

namespace Content.Shared._TP.Aquaculture;

[Serializable, NetSerializable]
public enum FishGrowerVisuals
{
    FishRsi,
    FishIconState,
    HealthLight,
    WaterLight,
    NutritionLight,
    HarvestLight,
    WarningLight,
    FilterLight,
}
