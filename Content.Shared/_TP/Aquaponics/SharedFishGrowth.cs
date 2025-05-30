using Robust.Shared.Serialization;

namespace Content.Shared._TP.Aquaponics;

public sealed class SharedFishGrowth
{
    public static string BeakerSlotId = "beakerSlot";
}

[Serializable, NetSerializable]
public enum SharedFishGrowerVisualState : byte
{
    BeakerAttached
}

[Serializable, NetSerializable]
public enum SharedFishGrowerVisuals
{
    AlertState,
    FoodState,
    HarvestState,
    HealthState,
    WasteState,
    WaterState,
}
