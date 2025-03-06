using Robust.Shared.Serialization;

namespace Content.Shared._TP.Aquaculture;

[Serializable, NetSerializable]
public enum SharedFishOMatic : byte
{
    Extract,
    Mutate
}
