namespace Content.Shared._TP.Plankton;

[RegisterComponent]
public sealed partial class PlanktonComponent : Component
{
    /// <summary>
    ///     Dead plankton amount
    /// </summary>
     [DataField]
    public float DeadPlankton { get; set; }

    /// <summary>
    ///     A list of species instances in this component
    /// </summary>
    public List<PlanktonSpeciesInstance> SpeciesInstances { get; set; } = new();

    /// <summary>
    ///     Plankton class creation.
    /// </summary>
    /// <param name="speciesName">Species colony name</param>
    /// <param name="diet">Species diet</param>
    /// <param name="characteristics">Characteristics enum</param>
    /// <param name="currentSize">Current size of the colony</param>
    /// <param name="currentHunger">Current hunger of the colony</param>
    /// <param name="isAlive">Whether this colony is alive</param>
    public sealed class PlanktonSpeciesInstance(
        PlanktonName speciesName,
        PlanktonDiet diet,
        PlanktonCharacteristics characteristics,
        float currentSize,
        float currentHunger,
        bool isAlive)
    {
        public PlanktonName SpeciesName { get; set; } = speciesName;
        public PlanktonDiet Diet { get; set; } = diet;
        public PlanktonCharacteristics Characteristics { get; set; } = characteristics;
        public float CurrentSize { get; set; } = currentSize;
        public float CurrentHunger { get; set; } = currentHunger;
        public bool IsAlive { get; set; } = isAlive;
    }

    /// <summary>
    ///     <para>Carnivore - Eats other plankton colonies</para>
    ///     <para>Chemophage - Eats a specific, or category, of chemicals</para>
    ///     <para>Decomposer - Eats the corpses of dead cultures</para>
    ///     <para>Electrophage - Uses electricity from lightning bolts</para>
    ///     <para>Photosynthetic - Eats from light</para>
    ///     <para>Radiophage - Eats radiation</para>
    ///     <para>Saguinophage - Eats blood - Possibly a parasite?</para>
    ///     <para>Scavenger - Eats waste and trash objects - Useful for janitors!</para>
    ///     <para>Symbiotroph - Thrives with other plankton and eats their byproduct.</para>
    /// </summary>
    public enum PlanktonDiet
    {
        Carnivore,
        Chemophage,
        Decomposer,
        Electrophage,
        Photosynthetic,
        Radiophage,
        Saguinophage,
        Scavenger,
        Symbiotroph,
    }

    /// <summary>
    ///     <para>AerosolSpores - Releases spores into a 3x3 grid around it. Anything not wearing internals will be infested with some sort of growth or cellular damage.</para>
    ///     <para>Aggressive - Kills other plankton in the same enclosure.</para>
    ///     <para>Bioluminescent - Glows</para>
    ///     <para>Charged - Handling their container requires insuls</para>
    ///     <para>ChemicalProduction - Has a byproduct of a specific chemical</para>
    ///     <para>Cryophilic - Loves cold environments</para>
    ///     <para>Hallucinogenic - Causes a crazy high if ingested(?)</para>
    ///     <para>HyperExotic - Has strange mutations and hard to work with. Worth a lot of points.</para>
    ///     <para>MagneticField - Interferes with Electronics and Jellids</para>
    ///     <para>Mimicry - Disguises effectively, can be confused for other present species</para>
    ///     <para>Parasitic - Touching a living being infests them with the species, causing bleed damage and draining hunger</para>
    ///     <para>PheromoneGlands - Alters the behavior of other species, either positively or negatively</para>
    ///     <para>PolypColony - Grows coral around itself</para>
    ///     <para>Pyrophilic - Thrives in hot environments</para>
    ///     <para>Pyrotechnic - Causes explosions with other plankton</para>
    ///     <para>Radioactive - Spits out small dosages of radiation</para>
    ///     <para>Sentience -  single organism gathered that exhibits behavior of an intelligent being - He wants the Krabby Patty formula</para>
    /// </summary>
    [Flags]
    public enum PlanktonCharacteristics
    {
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
        Sentience = 1 << 16,
    }

    /// <summary>
    ///     The stored diet of the colony
    /// </summary>
    [DataField("diet"), ViewVariables(VVAccess.ReadWrite)]
    public PlanktonDiet Diet { get; set; }

    /// <summary>
    ///     The stored characteristics of the colony
    /// </summary>
    [DataField("characteristics"), ViewVariables(VVAccess.ReadWrite)]
    public PlanktonCharacteristics Characteristics { get; set; }

    #region extra
    /// <summary>
    ///     Minimum temperature tolerance
    /// </summary>
    [DataField("temperatureToleranceLow"), ViewVariables(VVAccess.ReadWrite)]
    public float TemperatureToleranceLow { get; set; } = 0.0f;

    /// <summary>
    ///     Maximum temperature tolerance
    /// </summary>
    [DataField("temperatureToleranceHigh"), ViewVariables(VVAccess.ReadWrite)]
    public float TemperatureToleranceHigh { get; set; } = 30.0f;

    public static readonly string[] PlanktonFirstNames =
    [
        "Acanthocystis",    "Actinophrys",  "Amphora",          "Apistosporus",     "Aulacodiscus",
        "Brachionus",       "Cladocera",    "Coscinodiscus",    "Didinium",         "Diatoma",
        "Entomorpha",       "Euglena",      "Gloeocapsa",       "Leptocylindrus",   "Mastigophora",
        "Mesorhizobium",    "Navicula",     "Nitzschia",        "Oscillatoria",     "Phaeodactylum",
        "Phacus",           "Platymonas",   "Protoperidinium",  "Pyramimonas",      "Spirulina",
        "Synedra",          "Tetradontia",  "Trachelomonas",    "Volvox",           "Vorticella",
        "Ratilus",          "Betamios",     "Noctliuca",        "Terminidia",       "Democracia",
        "Meridia",          "Malevalon",    "ERROR",            "Kerbalius",        "Raptura",
        "Bill",             "Kharaa",       "Sheldon",
    ];

    public static readonly string[] PlanktonSecondNames =
    [
        "longispina",   "latifolia",    "quadricaudata",    "gracilis",             "deloriana",
        "radiata",      "honkliens",    "cystiformis",      "fimbriata",            "planctonica",
        "viridis",      "globosa",      "aurelia",          "pulchra",              "reducta",
        "tuberculata",  "subtilis",     "hyalina",          "cephalopodiformis",    "corymbosa",
        "unobtania",    "tri-tachia",   "xenofila",         "macrospora",           "apogeelia",
        "lucida",       "triesta",      "rounyens",         "tcarotenoides",        "ectoplasmica",
        "thingius",     "cordycepsia",  "krabby",           "jones",                "4546B",
        "nomaia",       "exadv1ia",     "florania",         "hylotlia",             "thargoidis",
        "rottia",       "hearthiata",   "celesteia",            "backenis",         "j. plankton",
    ];
    #endregion

    // Class to combine the first and second name for plankton species
    public sealed class PlanktonName(string firstName, string secondName)
    {
        public string FirstName { get; set; } = firstName;
        public string SecondName { get; set; } = secondName;

        // Overriding ToString to provide a formatted name
        public override string ToString()
        {
            return $"{FirstName} {SecondName}";
        }
    }
}
