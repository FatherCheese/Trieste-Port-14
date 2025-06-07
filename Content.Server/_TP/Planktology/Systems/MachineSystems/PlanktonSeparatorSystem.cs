using Content.Server._TP.Planktology.Components;
using Content.Server._TP.Planktology.Components.Machines;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._TP.Planktology.Systems.MachineSystems;

public sealed class PlanktonSeparatorSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    // Separator specific
    [Dependency] private readonly PlanktonSeparatorGeneratorSystem _planktonSeparatorGenerator = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanktonSeparatorComponent, InteractHandEvent>(OnInteractWithHands);
        SubscribeLocalEvent<PlanktonSeparatorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlanktonSeparatorComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    ///     The method handling the examination of the separator.
    /// </summary>
    /// <param name="uid">PlanktonSeparator UID</param>
    /// <param name="separatorComp">PlanktonSeparator Component</param>
    /// <param name="args">ExaminedEvent Arguments</param>
    private void OnExamined(EntityUid uid, PlanktonSeparatorComponent separatorComp, ExaminedEvent args)
    {
        // We start with the stored plankton at priority 10. (highest)
        // By "highest", "lower", and "lowest", I mean specifically for this method. - Cookie (FatherCheese)
        args.AddMarkup(separatorComp.StoredPlankton.Count <= 0
                ? Loc.GetString("plankton-separator-empty")
                : Loc.GetString("plankton-separator-plankton-count", ("count", separatorComp.StoredPlankton.Count)),
            10);

       // Now we move onto the last run time at priority 9. (lower)
       // We just check if the last run time is null or if
       // it's below the cooldown duration.
        if (separatorComp.LastRunTime != null)
        {
            var timeSinceLastRun = _timing.CurTime - separatorComp.LastRunTime.Value;
            if (timeSinceLastRun < separatorComp.CooldownDuration)
            {
                var remainingTime = separatorComp.CooldownDuration - timeSinceLastRun;
                args.AddMarkup(Loc.GetString("plankton-separator-cooldown", ("time", TimeSpan.FromSeconds(remainingTime.TotalSeconds))), 9);
            }
            else
            {
                args.AddMarkup(Loc.GetString("plankton-separator-ready"), 9);
            }
        }
        else
            args.AddMarkup(Loc.GetString("plankton-separator-ready"), 9);

        // Finally, we check the distance to the last run position, now at priority 8. (lowest)
        // If it's null or if the distance is below the minimum distance, we say it's ready.
        // This is copied from the "Hands" event but with a markup message.
        var currentPos = _transformSystem.GetWorldPosition(uid);
        if (separatorComp.LastRunPosition != null)
        {
            var distance = (currentPos - separatorComp.LastRunPosition.Value).LengthSquared();
            args.AddMarkup(distance < separatorComp.MinimumDistance
                    ? Loc.GetString("plankton-separator-distance-close")
                    : Loc.GetString("plankton-separator-distance-ready"),
                8);
        }
        else
            args.AddMarkup(Loc.GetString("plankton-separator-distance-ready"), 8);
    }

    /// <summary>
    ///     The method handling the item interactions with the separator.
    ///     This is only really used by the PlanktonVial Component.
    /// </summary>
    /// <param name="uid">PlanktonSeparator UID</param>
    /// <param name="separatorComp">PlanktonSeparator Component</param>
    /// <param name="args">InteractUsingEvent Arguments</param>
    private void OnInteractUsing(EntityUid uid, PlanktonSeparatorComponent separatorComp, InteractUsingEvent args)
    {
        // Some checks before the main method.
        // First, check if the used item is a PlanktonVial component.
        // Then check if the separator has plankton in it.
        // And then another check for an empty vial.
        if (!TryComp<PlanktonVialComponent>(args.Used, out var vialComp))
            return;

        if (separatorComp.StoredPlankton.Count <= 0)
        {
            _popup.PopupEntity(Loc.GetString("plankton-separator-no-plankton-message"), uid, PopupType.Medium);
            return;
        }

        if (vialComp.ContainedSpecimen != null)
        {
            _popup.PopupEntity(Loc.GetString("plankton-vial-full-message"), uid, PopupType.Medium);
            return;
        }

        // Now if we're at this point, store the first plankton in the separator
        // as a variable and remove it from the separator.
        // Then we move it to the vial and set the parent to the used item.
        var planktonUid = separatorComp.StoredPlankton[0];
        separatorComp.StoredPlankton.RemoveAt(0);
        vialComp.ContainedSpecimen = planktonUid;

        _transformSystem.SetParent(planktonUid, args.Used);

        args.Handled = true;
    }

    /// <summary>
    ///     The method handling hand interactions with the separator.
    /// </summary>
    /// <param name="uid">PlanktonSeparator UID</param>
    /// <param name="separatorComp">PlanktonSeparator Component</param>
    /// <param name="args">InteractHandEvent Arguments</param>
    private void OnInteractWithHands(EntityUid uid, PlanktonSeparatorComponent separatorComp, InteractHandEvent args)
    {
        TryRunSeparator(uid, separatorComp);
        args.Handled = true;
    }

    private void TryRunSeparator(EntityUid uid, PlanktonSeparatorComponent separatorComp)
    {
        // First, check if the separator's position is close to the last run position.
        // Or if the last run position is even a thing.
        var currentPos = _transformSystem.GetWorldPosition(uid);
        if (separatorComp.LastRunPosition != null)
        {
            var distance = (currentPos - separatorComp.LastRunPosition.Value).LengthSquared();
            if (distance < separatorComp.MinimumDistance)
            {
                _popup.PopupEntity(Loc.GetString("plankton-separator-distance-message"), uid, PopupType.Medium);
                return;
            }
        }

        // Now two checks if it's in water and if it can create plankton.
        // If it all passed, set the last run position and time.
        if (!IsInSeaWater(uid))
            return;

        if (!CreatePlankton(uid, separatorComp))
            return;

        separatorComp.LastRunTime = _timing.CurTime;
        separatorComp.LastRunPosition = currentPos;
    }

    /// <summary>
    ///     The method to create plankton from the Seawater.
    /// </summary>
    /// <param name="uid">PlanktonSeparator UID</param>
    /// <param name="separatorComp">PlanktonSeparator Component</param>
    /// <returns></returns>
    private bool CreatePlankton(EntityUid uid, PlanktonSeparatorComponent separatorComp)
    {
        if (separatorComp.StoredPlankton.Count + separatorComp.CreatedPlankton > separatorComp.MaxStoredPlankton)
        {
            _popup.PopupEntity(Loc.GetString("plankton-separator-max-plankton-message"), uid, PopupType.Medium);
            return false;
        }

        var planktonOne = _planktonSeparatorGenerator.GenerateCompletePlankton();
        var planktonTwo = _planktonSeparatorGenerator.GenerateCompletePlankton();

        Spawn("Plankton", Transform(uid).Coordinates);
        Spawn("Plankton", Transform(uid).Coordinates);

        return true;
    }

    /// <summary>
    ///     A method to check if the separator is in SeaWater.
    /// </summary>
    /// <param name="separatorUid">PlanktonSeparator UID</param>
    /// <returns>Return true if the water amount is above 0.0f</returns>
    private bool IsInSeaWater(EntityUid separatorUid)
    {
        // Get the grid that the separator is on. If it's null, return false.
        var gridUid = _transformSystem.GetGrid(separatorUid);
        if (gridUid == null)
            return false;

        // Now get the tile position, as well as the mixture at that position.
        var position = _transformSystem.GetGridTilePositionOrDefault(separatorUid);
        var mixture = _atmosphere.GetTileMixture(gridUid.Value, null, position, true);

        // Then check for water in the mixture.
        var waterAmount = mixture?.GetMoles(Gas.Water);

        if (waterAmount <= 0f)
            _popup.PopupEntity(Loc.GetString("plankton-separator-no-water-message"), separatorUid, PopupType.Medium);

        return waterAmount > 0f;
    }
}
