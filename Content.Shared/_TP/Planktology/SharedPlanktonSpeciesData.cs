namespace Content.Shared._TP.Planktology;

/// <summary>
///     Static data and generation methods for the Plankton Species
/// </summary>
public static class SharedPlanktonSpeciesData
{
    #region Names
    public static readonly string[] PlanktonFirstNames =
    [
        "Acanthocystis", "Actinophrys", "Amphora", "Apistosporus", "Aulacodiscus",
        "Brachionus", "Cladocera", "Coscinodiscus", "Didinium", "Diatoma",
        "Entomorpha", "Euglena", "Gloeocapsa", "Leptocylindrus", "Mastigophora",
        "Mesorhizobium", "Navicula", "Nitzschia", "Oscillatoria", "Phaeodactylum",
        "Phacus", "Platymonas", "Protoperidinium", "Pyramimonas", "Spirulina",
        "Synedra", "Tetradontia", "Trachelomonas", "Volvox", "Vorticella", "Bill",
        "Ratilus", "Betamios", "Noctliuca", "Terminidia", "Democracia", "Kharaa",
        "Meridia", "Malevalon", "ERROR", "Kerbalius", "Raptura",
        "Sheldon"
    ];

    public static readonly string[] PlanktonSecondNames =
    [
        "longispina", "latifolia", "quadricaudata", "gracilis", "deloriana",
        "radiata", "honkliens", "cystiformis", "fimbriata", "planctonica",
        "viridis", "globosa", "aurelia", "pulchra", "reducta",
        "tuberculata", "subtilis", "hyalina", "cephalopodiformis", "corymbosa",
        "unobtania", "tri-tachia", "xenofila", "macrospora", "apogeelia",
        "lucida", "triesta", "rounyens", "tcarotenoides", "ectoplasmica",
        "thingius", "cordycepsia", "krabby", "jones", "4546B", "rottia", "hearthiata",
        "nomaia", "exadv1ia", "florania", "hylotlia", "thargoidis", "celesteia", "brackenis",
    ];
    #endregion

    #region Research Values
    /// <summary>
    ///     How many research points are gotten per characteristic.
    ///     This is listed in order from 'Low Risk' to 'Ultra Rare'.
    /// </summary>
    public static readonly Dictionary<PlanktonCharacteristics, int> CharacteristicResearchValues = new()
    {
        // Low Risk and Utility
        { PlanktonCharacteristics.Cryophilic, 3 },
        { PlanktonCharacteristics.Pyrophilic, 3 },
        { PlanktonCharacteristics.Bioluminescent, 5 },
        { PlanktonCharacteristics.MagneticField, 8 },
        { PlanktonCharacteristics.ChemicalProduction, 10 },
        // Medium Risk and Complex
        { PlanktonCharacteristics.Aggressive, 15 },
        { PlanktonCharacteristics.Charged, 18 },
        { PlanktonCharacteristics.Radioactive, 20 },
        { PlanktonCharacteristics.Hallucinogenic, 22 },
        { PlanktonCharacteristics.PheromoneGlands, 22 },
        // High Risk and Exotic
        { PlanktonCharacteristics.PolypColony, 30 },
        { PlanktonCharacteristics.Parasitic, 35 },
        { PlanktonCharacteristics.Pyrotechnic, 40 },
        { PlanktonCharacteristics.Mimicry, 45 },
        { PlanktonCharacteristics.AerosolSpores, 50 },
        { PlanktonCharacteristics.ViolentSymbiote, 50 },
        // Ultra Rare
        { PlanktonCharacteristics.HyperExoticSpecies, 100 },
        { PlanktonCharacteristics.Sapience, 150 },
    };

    /// <summary>
    ///     How many research points are gotten per Diet entry.
    ///     Ordered from 'Simple' to 'Specialized'.
    /// </summary>
    public static readonly Dictionary<PlanktonDiet, int> DietResearchValues = new()
    {
        // Simple
        { PlanktonDiet.Photosynthetic, 5 },
        { PlanktonDiet.Decomposer, 6 },
        { PlanktonDiet.Scavenger, 8 },
        // Complex
        { PlanktonDiet.Carnivore, 12 },
        { PlanktonDiet.Symbiotroph, 10 },
        { PlanktonDiet.Chemotroph, 15 },
        // Specialized
        { PlanktonDiet.Saguinophage, 20 },
        { PlanktonDiet.Electrotroph, 22 },
        { PlanktonDiet.Radiotroph, 25 },
        { PlanktonDiet.Parasite, 28 }
    };
    #endregion

    #region Weights
    /// <summary>
    ///     The weighted chances of each characteristic.
    ///     Goes from "Low Risk" (high) to "Ultra Rare" (low).
    /// </summary>
    public static readonly Dictionary<PlanktonCharacteristics, float> CharacteristicWeights = new()
    {
        // Low Risk (common) - weight 30-50
        { PlanktonCharacteristics.Cryophilic, 50f },
        { PlanktonCharacteristics.Pyrophilic, 50f },
        { PlanktonCharacteristics.Bioluminescent, 40f },
        { PlanktonCharacteristics.MagneticField, 30f },
        { PlanktonCharacteristics.ChemicalProduction, 25f },

        // Medium Risk - weight 10-20
        { PlanktonCharacteristics.Aggressive, 20f },
        { PlanktonCharacteristics.Charged, 15f },
        { PlanktonCharacteristics.Radioactive, 12f },
        { PlanktonCharacteristics.Hallucinogenic, 10f },
        { PlanktonCharacteristics.PheromoneGlands, 10f },

        // High Risk - weight 3-8
        { PlanktonCharacteristics.PolypColony, 8f },
        { PlanktonCharacteristics.Parasitic, 6f },
        { PlanktonCharacteristics.Pyrotechnic, 5f },
        { PlanktonCharacteristics.Mimicry, 4f },
        { PlanktonCharacteristics.AerosolSpores, 3f },
        { PlanktonCharacteristics.ViolentSymbiote, 3f },

        // Ultra Rare - weight 0.5-1
        { PlanktonCharacteristics.HyperExoticSpecies, 1f },
        { PlanktonCharacteristics.Sapience, 0.5f },
    };

    /// <summary>
    ///     The weighted chances of diets.
    ///     Goes from "Simple" (common) to "Specialized" (rare).
    /// </summary>
    public static readonly Dictionary<PlanktonDiet, float> DietWeights = new()
    {
        // Simple (common)
        { PlanktonDiet.Photosynthetic, 35f },
        { PlanktonDiet.Decomposer, 30f },
        { PlanktonDiet.Scavenger, 25f },

        // Complex
        { PlanktonDiet.Carnivore, 15f },
        { PlanktonDiet.Symbiotroph, 18f },
        { PlanktonDiet.Chemotroph, 12f },

        // Specialized (rare)
        { PlanktonDiet.Saguinophage, 8f },
        { PlanktonDiet.Electrotroph, 6f },
        { PlanktonDiet.Radiotroph, 5f },
        { PlanktonDiet.Parasite, 3f }
    };
    #endregion

    #region Conflicts
    /// <summary>
    ///     Conflicting characteristics and diets, this is for removal later!
    /// </summary>
    public static readonly Dictionary<PlanktonCharacteristics, List<PlanktonDiet>> CharacteristicDietConflicts = new()
    {
        { PlanktonCharacteristics.Aggressive, [PlanktonDiet.Symbiotroph] },
        {
            PlanktonCharacteristics.Parasitic, [
                PlanktonDiet.Photosynthetic,
                PlanktonDiet.Scavenger,
                PlanktonDiet.Symbiotroph,
                PlanktonDiet.Chemotroph,
            ]
        },

        {
            PlanktonCharacteristics.ViolentSymbiote, [
            PlanktonDiet.Photosynthetic,
            PlanktonDiet.Scavenger,
            PlanktonDiet.Symbiotroph,
            PlanktonDiet.Chemotroph,
            ]
        },
    };

    /// <summary>
    ///     Conflicting characteristics between each other,
    ///     this is for removal later!
    /// </summary>
    public static readonly Dictionary<PlanktonCharacteristics, List<PlanktonCharacteristics>> CharacteristicConflicts = new()
    {
        // Temperature conflicts
        { PlanktonCharacteristics.Cryophilic, [PlanktonCharacteristics.Pyrophilic, PlanktonCharacteristics.Pyrotechnic] },
        { PlanktonCharacteristics.Pyrophilic, [PlanktonCharacteristics.Cryophilic] },
        { PlanktonCharacteristics.Pyrotechnic, [PlanktonCharacteristics.Cryophilic] },

        // Behavioral conflicts
        { PlanktonCharacteristics.Aggressive, [PlanktonCharacteristics.ViolentSymbiote] },
        { PlanktonCharacteristics.Parasitic, [PlanktonCharacteristics.ViolentSymbiote] },

        // Energy conflicts
        { PlanktonCharacteristics.Charged, [PlanktonCharacteristics.MagneticField] },
    };

    /// <summary>
    ///     Synergistic characteristics. Aka two that
    ///     have a higher chance of going with each other.
    /// </summary>
    // Not really a conflict, but close enough. - Cookie (FatherCheese)
    public static readonly Dictionary<PlanktonCharacteristics, List<PlanktonDiet>> CharacteristicDietSynergies = new()
    {
        { PlanktonCharacteristics.Radioactive, [PlanktonDiet.Radiotroph] },

        { PlanktonCharacteristics.Charged, [PlanktonDiet.Electrotroph] },

        { PlanktonCharacteristics.ChemicalProduction, [PlanktonDiet.Chemotroph] },

        { PlanktonCharacteristics.Parasitic, [PlanktonDiet.Parasite] },

        { PlanktonCharacteristics.Aggressive, [PlanktonDiet.Carnivore, PlanktonDiet.Saguinophage] }
    };
    #endregion
}
