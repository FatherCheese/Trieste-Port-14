using Content.Shared.Examine;
using FishEggComponent = Content.Server._TP.Aquaculture.Components.FishEggComponent;

namespace Content.Server._TP.Aquaculture.Systems;

/// <summary>
/// Gives something the ability to be put inside a Fish Grower component.
/// </summary>
public sealed class FishEggSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishEggComponent, ExaminedEvent>(OnExamined);
    }


    /// <summary>
    /// The description given when examined by a user.
    /// </summary>
    /// <param name="uid">FishEgg uid</param>
    /// <param name="comp">FishEggComponent</param>
    /// <param name="args">ExaminedEvent</param>
    private void OnExamined(EntityUid uid, FishEggComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FishEggComponent)))
        {
            args.PushMarkup(comp.FishDescription);
        }
    }
}
