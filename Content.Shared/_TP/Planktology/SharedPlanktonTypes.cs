namespace Content.Shared._TP.Planktology;

/// <summary>
///     What characteristics the plankton will have.
///     A lot of these are dangerous, some are utility, and some
///     affect the plankton stats.
/// </summary>
[Flags]
public enum PlanktonCharacteristics
{
    None = 0,
    AerosolSpores = 1 << 0,
    Aggressive = 1 << 1,
    Bioluminescent = 1 << 2,
    Charged = 1 << 3,
    ChemicalProduction = 1 << 4,
    Cryophilic = 1 << 5,
    Hallucinogenic = 1 << 6,
    HyperExoticSpecies = 1 << 7,
    MagneticField = 1 << 8,
    Mimicry = 1 << 9,
    Parasitic = 1 << 10,
    PheromoneGlands = 1 << 11,
    PolypColony = 1 << 12,
    Pyrophilic = 1 << 13,
    Pyrotechnic = 1 << 14,
    Radioactive = 1 << 15,
    Sapience = 1 << 16,
    ViolentSymbiote = 1 << 17,
}

/// <summary>
///     What diet the plankton will have.
///     These can conflict with characteristics,
///     and plankton will only have a single diet.
/// </summary>
public enum PlanktonDiet
{
    Carnivore,
    Chemotroph,
    Decomposer,
    Electrotroph,
    Parasite,
    Photosynthetic,
    Radiotroph,
    Saguinophage,
    Scavenger,
    Symbiotroph,
}

public enum PlanktonSize
{
    Tiny,
    Small,
    Medium,
    Large,
    Huge,
    Giant,
    Colossal,
}

/// <summary>
///     The plankton name, saved as a record.
/// </summary>
public record PlanktonName(string FirstName, string SecondName)
{
    public override string ToString()
    {
        return $"{FirstName} {SecondName}";
    }
}
