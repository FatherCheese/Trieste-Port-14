using Content.Shared._TP.Aquaculture.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;

namespace Content.Server._TP.Aquaculture.Systems;

public sealed class FilterItemSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FitsInFilterComponent, ExaminedEvent>(OnExamineSolution);
    }

    private void OnExamineSolution(Entity<FitsInFilterComponent> entity, ref ExaminedEvent args)
    {
        if (_solution.TryGetSolution(args.Examined, "filter", out _, out var solution))
        {
            args.PushText(Loc.GetString("filter-item-component-on-examine-solution",
                ("amount", solution.Volume)));
        }
        else
        {
            args.PushText(Loc.GetString("filter-item-component-on-examine-solution",
                ("amount", 0)));
        }
    }
}
