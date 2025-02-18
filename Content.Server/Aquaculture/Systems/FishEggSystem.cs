using Content.Server.Aquaculture.Components;
using Content.Shared.Examine;

namespace Content.Server.Aquaculture.Systems;

public sealed class FishEggSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishEggComponent, ExaminedEvent>(OnExamined);
    }


    private void OnExamined(EntityUid uid, FishEggComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FishEggComponent)))
        {
            args.PushMarkup(Loc.GetString("fishegg-component-name"));
        }
    }
}
