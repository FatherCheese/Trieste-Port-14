using System.Linq;
using Content.Server._TP.Aquaponics.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._TP.Aquaponics.Systems;

public sealed class FishGrowthFishSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Fish Growth specific
    [Dependency] private readonly FishGrowthSystem _fishGrowth = default!;
    [Dependency] private readonly FishGrowthVisualSystem _fishGrowthVisuals = default!;
    [Dependency] private readonly FishGrowthTankSystem _fishGrowthTankSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AquacultureTankComponent>();
        while (query.MoveNext(out var uid, out var tankComp))
        {
            var curTime = _timing.CurTime;
            foreach (var fishComp in tankComp.Fish)
            {
                // Breeding (Adults only!)
                if (fishComp.GrowthStage == FishGrowthStage.Adult &&
                    fishComp.Timers.TryGetValue("breed", out var breedVal) && curTime > breedVal)
                {
                    // Create a breed rate float and get the breed rate from traits.
                    // If the rate is above 0, then multiply the rates.
                    var breedRate = 1.0f;
                    foreach (var trait in fishComp.Traits)
                    {
                        if (trait.TraitTypes.TryGetValue(FishTraitType.EggRate, out var rate))
                            breedRate *= rate;
                    }

                    // Only attempt breeding if the rate is above 0.
                    var eggResult = TryBreedFish(tankComp);
                    if (breedRate > 0)
                        tankComp.EggPrototype = eggResult;

                    fishComp.Timers["breed"] = curTime + _fishGrowth.GetRandomTimer("breed", fishComp) * breedRate;
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, tankComp, fishComp);
                }

                // Consumption
                if (fishComp.Timers.TryGetValue("consume", out var consumeVal) && curTime > consumeVal)
                {
                    ConsumeTankResources(uid, tankComp, fishComp);
                    fishComp.Timers["consume"] = curTime + _fishGrowth.GetRandomTimer("consume", fishComp);
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, tankComp, fishComp);
                }

                // Growth
                if (fishComp.Timers.TryGetValue("growth", out var growthVal) && curTime > growthVal)
                {
                    // Create a growth rate float and get the growth rate from traits.
                    // If it's above 0, then multiply the rates.
                    var growthRate = 1.0f;
                    foreach (var trait in fishComp.Traits)
                    {
                        if (trait.TraitTypes.TryGetValue(FishTraitType.GrowthRate, out var rate))
                            growthRate *= rate;
                    }

                    if (growthRate > 0)
                        fishComp.GrowthStage = TryAgeFish(fishComp);

                    fishComp.Timers["growth"] = curTime + _fishGrowth.GetRandomTimer("growth", fishComp) * growthRate;
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, tankComp, fishComp);
                }

                // Healing
                if (fishComp.Timers.TryGetValue("health", out var healthVal) && curTime > healthVal)
                {
                    ChangeHealth(uid, tankComp, fishComp);
                    fishComp.Timers["health"] = curTime + _fishGrowth.GetRandomTimer("health", fishComp);
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, tankComp, fishComp);
                }

                // Genetics
                if (fishComp.Timers.TryGetValue("gene", out var geneVal) && curTime > geneVal)
                {
                    CheckGeneticStability(tankComp, fishComp);
                    TryMutateFishTraits(tankComp, fishComp);
                    fishComp.Timers["gene"] = curTime + _fishGrowth.GetRandomTimer("gene", fishComp);
                    _fishGrowthVisuals.UpdateSpriteForFish(uid, tankComp, fishComp);
                }
            }
        }
    }

    private void CheckGeneticStability(AquacultureTankComponent tankComp, FishComponent fishComp)
    {
        // Get the death rate from traits.
        var deathRate = 1.0f;
        foreach (var trait in fishComp.Traits)
        {
            if (trait.TraitTypes.TryGetValue(FishTraitType.DeathRate, out var rate) && rate > 0)
                deathRate *= rate;
        }

        // If the tank's mutagen level is greater than 0,
        // add to the death threshold, by the fish's mutagen penalty and death rate.
        // Also, adjust the tank's mutagen level by one.
        var currentDeathThreshold = fishComp.DeathThreshold;
        if (tankComp.MutagenLevel > 0)
        {
            currentDeathThreshold = Math.Min(currentDeathThreshold + fishComp.MutagenPenalty, 1.0f);
            fishComp.GeneticStability = Math.Max(fishComp.GeneticStability - 0.1f * deathRate, 0);
            _fishGrowthTankSystem.AdjustMutagenLevel(-1, tankComp);
        }

        // If the genetic stability is or greater than 100
        // or if the fish is already dead, then return so no damage is applied.
        if (fishComp.GeneticStability >= 100 || fishComp.Health <= 0)
            return;

        // Check if the fish's health is below the death threshold.
        // The fish will instantly die if so.
        if (fishComp.Health <= currentDeathThreshold)
            AdjustHealth(-100, fishComp);

        // Randomly damage the fish based on 0.05,
        // so 0.95 stability would be 0.05 damage,
        // 0.90 would be 0.10 damage, etc.
        // Then, multiply the damage by the death rate.
        var damage = 1.0f - fishComp.GeneticStability;
        if (_random.Prob(0.5f))
            AdjustHealth(-damage * deathRate, fishComp);
    }

    private void TryMutateFishTraits(AquacultureTankComponent tankComp, FishComponent fishComp)
    {
        var randomTrait = _random.Pick(FishComponent.TraitWeights);
        if (CanCoexist(fishComp, randomTrait) && _random.Next(3) == 0)
            fishComp.Traits.Add(randomTrait);
    }

    /// <summary>
    ///     A function to check if a new trait can coexist with the current traits.
    /// </summary>
    /// <param name="fishComp">FishComponent</param>
    /// <param name="randomTrait">New Trait</param>
    /// <returns>bool</returns>
    private bool CanCoexist(FishComponent fishComp, FishTraitData randomTrait)
    {
        foreach (var trait in fishComp.Traits)
        {
            if (trait.TraitName == randomTrait.TraitName)
                return false;

            if (AreTraitsConflicting(trait, randomTrait))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     A function to check if a current trait and a new trait conflict.
    /// </summary>
    /// <param name="trait1">Current Trait</param>
    /// <param name="trait2">New Trait</param>
    /// <returns>bool</returns>
    private bool AreTraitsConflicting(FishTraitData trait1, FishTraitData trait2)
    {
        // Metabolism checks
        if (trait1.TraitName == "Hyperactive Metabolism" && trait2.TraitName == "Slow Metabolism")
            return true;

        // Fat-ness checks
        if (trait1.TraitName == "Fatty" && trait2.TraitName == "Skinny")
            return true;

        // If all else passes, then return false.
        return false;
    }

    /// <summary>
    ///     A function to change health. This one is for the
    ///     Timer systems.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="tankComp">AquacultureTankComponent</param>
    /// <param name="fishComp">FishComponent</param>
    private void ChangeHealth(EntityUid uid, AquacultureTankComponent tankComp, FishComponent fishComp)
    {
        // Separate blocks for if the waste tank exists. It should,
        // but if we ever add one WITHOUT a waste tank, this is useful.
        if (_solutionContainer.TryGetSolution(uid, tankComp.SolutionTankWaste, out _, out var wasteSol))
        {
            // Young fish in good conditions heal, and in the end
            // young fish in bad condition take damage.
            if (fishComp.GrowthStage != FishGrowthStage.Adult &&
                tankComp is { WaterLevel: > 30, FoodLevel: > 25 } &&
                wasteSol.Volume < 70)
            {
                AdjustHealth(10 * fishComp.GeneticStability, fishComp);
            }

            // Adult fish take aging damage regardless of conditions
            else if (fishComp.GrowthStage == FishGrowthStage.Adult)
            {
                var deathRate = 1.0f;
                foreach (var trait in fishComp.Traits)
                {
                    if (trait.TraitTypes.TryGetValue(FishTraitType.DeathRate, out var rate))
                        deathRate *= rate;
                }

                AdjustHealth(-10 * deathRate, fishComp);
            }
            else
            {
                AdjustHealth(-10, fishComp);
            }
        }
    }

    /// <summary>
    ///     The function handling the aging of fish.
    /// </summary>
    /// <param name="fishComp">FishComponent</param>
    /// <returns>FishGrowthStage (enum value)</returns>
    private FishGrowthStage TryAgeFish(FishComponent fishComp)
    {
        if (fishComp.Health <= 0)
            return fishComp.GrowthStage;

        return fishComp.GrowthStage switch
        {
            FishGrowthStage.Egg => FishGrowthStage.Fry,
            FishGrowthStage.Fry => FishGrowthStage.Juvenile,
            _ => FishGrowthStage.Adult
        };
    }

    /// <summary>
    ///     The function handling the consumption of tank resources.
    /// </summary>
    /// <param name="uid">Entity UID</param>
    /// <param name="tankComp">AquacultureTankComponent</param>
    /// <param name="fishComp">FishComponent</param>
    private void ConsumeTankResources(EntityUid uid, AquacultureTankComponent tankComp, FishComponent fishComp)
    {
        // Check if the fish is currently an egg or dead.
        // If either is true, then return.
        if (fishComp.GrowthStage == FishGrowthStage.Egg || fishComp.Health <= 0)
            return;

        // Now get the values that the fish consumes at.
        var waterNeeded = fishComp.TraitsBase.GetValueOrDefault("WaterConsumption", 0.2f);
        var foodNeeded = fishComp.TraitsBase.GetValueOrDefault("FoodConsumption", 0.5f);
        var wasteCreated = fishComp.TraitsBase.GetValueOrDefault("WasteProduction", 2.0f);

        // Then apply the modifiers! Check the trait data for more information.
        foreach (var trait in fishComp.Traits)
        {
            if (trait.TraitTypes.TryGetValue(FishTraitType.FoodRate, out var foodRate))
                foodNeeded *= foodRate;

            if (trait.TraitTypes.TryGetValue(FishTraitType.WasteRate, out var wasteRate))
                wasteCreated *= wasteRate;

            if (trait.TraitTypes.TryGetValue(FishTraitType.WaterRate, out var waterRate))
                waterNeeded *= waterRate;
        }

        // Now adjust the tank's resources via the tank system class!
        // Note that we need to do negative amounts for consumption.
        _fishGrowthTankSystem.AdjustWater(-waterNeeded, tankComp);
        _fishGrowthTankSystem.AdjustFood(-foodNeeded, tankComp);
        _fishGrowthTankSystem.AdjustWaste(uid, wasteCreated, tankComp);
    }

    /// <summary>
    ///     The function of breeding the adult fish.
    /// </summary>
    /// <param name="tankComp">AquacultureTankComponent</param>
    /// <returns>Egg ProtoID (string?)</returns>
    private string? TryBreedFish(AquacultureTankComponent tankComp)
    {
        // First, check if the fish count is two.
        // If not, return the egg as null.
        if (tankComp.Fish.Count != 2)
            return null;

        // Now check if they're both adults! If not, return the egg as null.
        var fishOne = tankComp.Fish[0];
        var fishTwo = tankComp.Fish[1];
        if (fishOne.GrowthStage != FishGrowthStage.Adult || fishTwo.GrowthStage != FishGrowthStage.Adult)
            return null;

        // Now the janky part. Get the egg rate trait of both fish.
        // If either is 0, return the egg as null.
        // This is also called in the timers section, so this is just to be safe. - Cookie (FatherCheese)
        var fishOneEggTrait = 1.0f;
        var fishTwoEggTrait = 1.0f;
        foreach (var trait in fishOne.Traits)
        {
            if (trait.TraitTypes.TryGetValue(FishTraitType.EggRate, out var rate) && rate > 0)
                fishOneEggTrait *= rate;
        }

        foreach (var trait in fishTwo.Traits)
        {
            if (trait.TraitTypes.TryGetValue(FishTraitType.EggRate, out var rate) && rate > 0)
                fishTwoEggTrait *= rate;
        }

        if (fishOneEggTrait == 0 || fishTwoEggTrait == 0)
            return null;

        // Now get the resulting egg based on a component map.
        // If the egg is null (which it shouldn't EVER be), return null.
        var eggFromFish = _fishGrowth.GetEggForFish(fishOne.ResultingItem);
        if (eggFromFish == null)
            return null;

        // Finally, return the egg prototype of the same fish.
        return tankComp.EggPrototype = eggFromFish;
    }

    /// <summary>
    ///     Adjust the fish's health level.
    /// </summary>
    /// <param name="amount">Amount to add or subtract (float)</param>
    /// <param name="fishComp">FishComponent</param>
    private void AdjustHealth(float amount, FishComponent fishComp)
    {
        // Check if the amount is positive.
        // If positive, do not exceed 100. If negative, do not exceed 0.
        fishComp.Health = amount > 0 ? Math.Min(fishComp.Health + amount, 100) : Math.Max(fishComp.Health + amount, 0);
    }
}
