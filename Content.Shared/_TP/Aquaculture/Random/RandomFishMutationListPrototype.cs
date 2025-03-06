using Robust.Shared.Prototypes;

namespace Content.Shared._TP.Aquaculture.Random;

[Prototype("RandomFishMutationList")]
public sealed class RandomFishMutationListPrototype : IPrototype
{
    [DataField]
    public string ID { get; } = default!;

    /// <summary>
    /// A list of random mutations that can be picked from.
    /// </summary>
    [DataField("mutations", required: true, serverOnly: true)]
    public List<RandomFishMutation> Mutations { get; } = new();
}
