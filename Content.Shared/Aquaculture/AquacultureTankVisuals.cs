using Robust.Shared.Serialization;

namespace Content.Shared.Aquaculture;

[Serializable, NetSerializable]
public enum AquacultureTankVisuals
{
    FishRsi,
    FishIconState,
    HealthLight,
    WaterLight,
    NutritionLight,
    HarvestLight,
    PlanktonLight,
}
