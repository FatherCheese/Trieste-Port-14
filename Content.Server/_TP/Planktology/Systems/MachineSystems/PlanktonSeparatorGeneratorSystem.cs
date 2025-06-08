using System.Linq;
using Content.Shared._TP.Planktology;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._TP.Planktology.Systems.MachineSystems;

public sealed class PlanktonSeparatorGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PrototypeManager _protoManager = default!;

    /// <summary>
    ///     A method to generate a plankton name.
    /// </summary>
    /// <returns>Returns a new first and last name</returns>
    private PlanktonName GenerateName()
    {
        var firstName = _random.Pick(SharedPlanktonSpeciesData.PlanktonFirstNames);
        var secondName = _random.Pick(SharedPlanktonSpeciesData.PlanktonSecondNames);
        return new PlanktonName(firstName, secondName);
    }

    /// <summary>
    ///     A method to generate a random plankton size.
    /// </summary>
    /// <returns>Returns a random size enum</returns>
    private PlanktonSize GenerateSize()
    {
        var sizes = Enum.GetValues<PlanktonSize>();
        return _random.Pick(sizes);
    }

    /// <summary>
    ///     A method to generate a plankton diet.
    /// </summary>
    /// <param name="existingCharacteristics">The existing characteristics of the plankton.</param>
    /// <returns>Returns a new generated weight.</returns>
    private PlanktonDiet GenerateDiet(PlanktonCharacteristics existingCharacteristics = PlanktonCharacteristics.None)
    {
        var weights = new Dictionary<PlanktonDiet, float>(SharedPlanktonSpeciesData.DietWeights);

        // Apply synergy bonuses
        foreach (var synergy in SharedPlanktonSpeciesData.CharacteristicDietSynergies)
        {
            if (!existingCharacteristics.HasFlag(synergy.Key))
                continue;

            foreach (var diet in synergy.Value)
            {
                if (weights.ContainsKey(diet))
                    weights[diet] *= 3f; // 3x more likely
            }
        }

        // Remove conflicting diets
        var validDiets = weights.Where(kvp => !HasDietConflict(existingCharacteristics, kvp.Key))
                               .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return GenerateWeightedEnum(validDiets);
    }

    /// <summary>
    ///     A method to generate plankton characteristics.
    /// </summary>
    /// <param name="count">The number of characteristics to generate. (Capped to 3)</param>
    /// <param name="diet">The PlanktonDiet enum to generate.</param>
    /// <returns></returns>
    private PlanktonCharacteristics GenerateCharacteristics(int count = 3, PlanktonDiet diet = PlanktonDiet.Photosynthetic)
    {
        var result = PlanktonCharacteristics.None;
        var availableCharacteristics = SharedPlanktonSpeciesData.CharacteristicWeights.Keys.ToList();

        for (var i = 0; i < count && availableCharacteristics.Count > 0; i++)
        {
            var weights = new Dictionary<PlanktonCharacteristics, float>();

            foreach (var characteristic in availableCharacteristics)
            {
                var baseWeight = SharedPlanktonSpeciesData.CharacteristicWeights[characteristic];
                if (SharedPlanktonSpeciesData.CharacteristicDietSynergies.ContainsKey(characteristic) &&
                    SharedPlanktonSpeciesData.CharacteristicDietSynergies[characteristic].Contains(diet))
                {
                    baseWeight *= 2f;
                }

                weights[characteristic] = baseWeight;
            }

            // Remove conflicting characteristics
            var validCharacteristics = weights.Where(kvp =>
                !HasCharacteristicConflict(result, kvp.Key) &&
                !HasDietConflict(kvp.Key, diet))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (validCharacteristics.Count == 0)
                break;

            var selected = GenerateWeightedEnum(validCharacteristics);
            result |= selected;
            availableCharacteristics.Remove(selected);
        }

        return result;
    }

    /// <summary>
    ///     A method for handling the checks of plankton characteristics.
    /// </summary>
    /// <param name="existing">Existing PlanktonCharacteristic Enum</param>
    /// <param name="newCharacteristic">New PlanktonCharacteristic Enum</param>
    /// <returns>Returns false if a reverse and normal check passes</returns>
    private bool HasCharacteristicConflict(PlanktonCharacteristics existing, PlanktonCharacteristics newCharacteristic)
    {
        if (SharedPlanktonSpeciesData.CharacteristicConflicts.TryGetValue(newCharacteristic, out var conflicts))
        {
            foreach (var conflict in conflicts)
            {
                if (existing.HasFlag(conflict))
                    return true;
            }
        }

        // Check reverse conflicts
        foreach (var kvp in SharedPlanktonSpeciesData.CharacteristicConflicts)
        {
            if (existing.HasFlag(kvp.Key) && kvp.Value.Contains(newCharacteristic))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     A method checking for diet conflicts.
    /// </summary>
    /// <param name="characteristics">PlanktonCharacteristics Enum</param>
    /// <param name="diet">PlanktonDiet Enum</param>
    /// <returns>Returns false if there are no conflicts.</returns>
    private bool HasDietConflict(PlanktonCharacteristics characteristics, PlanktonDiet diet)
    {
        foreach (var kvp in SharedPlanktonSpeciesData.CharacteristicDietConflicts)
        {
            if (characteristics.HasFlag(kvp.Key) && kvp.Value.Contains(diet))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     A method handling weighted generation of enums.
    /// </summary>
    /// <param name="weights">An Enum/Float Dictionary</param>
    /// <typeparam name="T">Type of Enum</typeparam>
    /// <returns>The first weighted key</returns>
    /// <exception cref="InvalidOperationException">Throws if there are no valid options</exception>
    private T GenerateWeightedEnum<T>(Dictionary<T, float> weights) where T : Enum
    {
        if (weights.Count == 0)
            throw new InvalidOperationException("No valid options available for weighted generation");

        var totalWeight = weights.Values.Sum();
        var randomValue = _random.NextFloat(0f, totalWeight);
        var currentWeight = 0f;

        foreach (var kvp in weights)
        {
            currentWeight += kvp.Value;
            if (randomValue <= currentWeight)
                return kvp.Key;
        }

        return weights.Keys.First(); // Fallback
    }

    /// <summary>
    ///     A method to handle generating Reagents, if possible.
    /// </summary>
    /// <param name="existingCharacteristics"></param>
    /// <returns></returns>
    private ReagentId? GenerateReagent(PlanktonCharacteristics existingCharacteristics = PlanktonCharacteristics.None)
    {
        if (existingCharacteristics.HasFlag(PlanktonCharacteristics.ChemicalProduction))
        {
            var allReagents = _protoManager.EnumeratePrototypes<ReagentPrototype>().ToList();

            if (allReagents.Count > 0)
            {
                var randomReagent = _random.Pick(allReagents);
                return new ReagentId(randomReagent.ID,);
            }
        }

        return null;
    }

    /// <summary>
    ///     A method to generate a completely random plankton species.
    /// </summary>
    public (PlanktonName name, PlanktonSize size, PlanktonDiet diet, PlanktonCharacteristics characteristics)
        GenerateCompletePlankton()
    {
        var name = GenerateName();
        var size = GenerateSize();
        var diet = GenerateDiet();
        var characteristics = GenerateCharacteristics(3, diet);

        return (name, size, diet, characteristics);
    }
}
