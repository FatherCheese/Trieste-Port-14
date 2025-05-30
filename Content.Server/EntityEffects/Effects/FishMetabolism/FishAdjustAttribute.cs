using System.Diagnostics.CodeAnalysis;
using Content.Server._TP.Aquaponics.Components;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.FishMetabolism;

[ImplicitDataDefinitionForInheritors]
public abstract partial class FishAdjustAttribute : EntityEffect
{
    [DataField]
    public float Amount { get; protected set; } = 1;

    /// <summary>
    ///     Localization key for the name of the adjusted attribute. Used for guidebook descriptions.
    /// </summary>
    [DataField]
    public abstract string GuidebookAttributeName { get; set; }

    /// <summary>
    ///     Whether the attribute in question is a good thing. Used for guidebook descriptions to determine the color of the number.
    /// </summary>
    [DataField]
    public virtual bool GuidebookIsAttributePositive { get; protected set; } = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var color = GuidebookIsAttributePositive ^ (Amount < 0.0) ? "green" : "red";
        return Loc.GetString("reagent-effect-guidebook-fish-attribute", ("attribute", Loc.GetString(GuidebookAttributeName)), ("amount", Amount.ToString("0.00")), ("colorName", color), ("chance", Probability));
    }

    /// <summary>
    ///     Checks if the aquaculture tank can metabolize the reagent or not.
    ///     Checks if it has a living fish by default.
    /// </summary>
    /// <param name="plantHolder">The entity holding the plant</param>
    /// <param name="tankComp">The plant holder component</param>
    /// <param name="entityManager">The entity manager</param>
    /// <param name="mustHaveAlivePlant">Whether to check if it has a live plant or not</param>
    /// <returns></returns>
    public bool CanMetabolize(EntityUid plantHolder,
        [NotNullWhen(true)] out AquacultureTankComponent? tankComp,
        IEntityManager entityManager,
        bool mustHaveAlivePlant = true)
    {
        tankComp = null;

        return entityManager.TryGetComponent(plantHolder, out tankComp)
               && (!mustHaveAlivePlant || tankComp.Fish.Count <= 0 || !(tankComp.Fish[0].Health > 0));
    }
}
