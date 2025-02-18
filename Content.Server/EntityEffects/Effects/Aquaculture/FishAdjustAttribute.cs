using System.Diagnostics.CodeAnalysis;
using Content.Server.Aquaculture.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.Aquaculture;

// TODO - Guidebook
/// <summary>
/// Base fish attribute adjustment system.
/// This is apparently needed to modify fish reagents??
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class FishAdjustAttribute : EntityEffect
{
    [DataField]
    public float Amount { get; protected set; } = 1;

    /// <summary>
    /// Localisation key for the name of the adjusted attribute. Used for guidebook descriptions.
    /// </summary>
    [DataField]
    public abstract string GuidebookAttributeName { get; set; }

    /// <summary>
    /// Whether the attribute in question is a good thing. Used for guidebook descriptions to determine the color of the number.
    /// </summary>
    [DataField]
    public virtual bool GuidebookIsAttributePositive { get; protected set; } = true;

    public bool CanFishMetabolize(EntityUid uid,
        [NotNullWhen(true)] out FishGrowerComponent? fishGrowerComp,
        IEntityManager entityManager,
        bool fishMustBeAlive = true)
    {
        fishGrowerComp = null;

        if (!entityManager.TryGetComponent(uid, out fishGrowerComp)
            || fishMustBeAlive && (fishGrowerComp.FishEgg == null || fishGrowerComp.FishDead))
            return false;

        return true;
    }


    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        string color;
        if (GuidebookIsAttributePositive ^ Amount < 0.0)
        {
            color = "green";
        }
        else
        {
            color = "red";
        }
        return Loc.GetString("reagent-effect-guidebook-fish-attribute", ("attribute", Loc.GetString(GuidebookAttributeName)), ("amount", Amount.ToString("0.00")), ("colorName", color), ("chance", Probability));
    }
}
