using System.Linq;
using System.Text.RegularExpressions;
using Content.Server._TP.Planktology.Components;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared._TP.Planktology;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._TP.Planktology.Systems.MachineSystems;

/// <summary>
///     The system tracking and validating plankton research.
/// </summary>
public sealed partial class PlanktonResearchSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.Machines.PlanktonAnalysisComputerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<Components.Machines.PlanktonAnalysisComputerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<Components.Machines.PlanktonAnalysisComputerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    /// <summary>
    ///     The method handling 'verbs', aka the context menu actions.
    /// </summary>
    /// <param name="uid">PlanktonAnalysisComputer UID</param>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    /// <param name="args">GetVerbsEvent Arguments (Alternative)</param>
    private void OnGetVerbs(EntityUid uid, Components.Machines.PlanktonAnalysisComputerComponent computerComp, GetVerbsEvent<AlternativeVerb> args)
    {
        // Returns if the user can't interact with the computer
        // Also returns if the species name starts with 'Unknown'
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (computerComp.ScannedSpeciesName.FirstName == "Unknown")
            return;

        // Paper printing verb.
        if (!computerComp.PaperGenerated)
        {
            var printVerb = new AlternativeVerb
            {
                Act = () => GenerateResearchPaper(uid, computerComp, args.User),
                Text = Loc.GetString("plankton-computer-generate-paper"),
                Priority = 1
            };
            args.Verbs.Add(printVerb);
        }

        // Sample purging verb.
        // Set lower than the paper printing verb, just so it's
        // less likely to be clicked at first.
        var purgeVerb = new AlternativeVerb()
        {
            Act = () => PurgePlanktonData(uid, computerComp),
            Text = Loc.GetString("plankton-computer-purge-data"),
            Priority = 0
        };
        args.Verbs.Add(purgeVerb);
    }

    /// <summary>
    ///     The method handling the "Purge Data" verb.
    ///     Essentially, this just resets the loaded sample and paper.
    /// </summary>
    /// <param name="uid">PlanktonAnalysisComputer UID</param>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    private void PurgePlanktonData(EntityUid uid, Components.Machines.PlanktonAnalysisComputerComponent computerComp)
    {
        // Handle the scanned data first, then proceed with the
        // paper-generated flag and sample load time.
        computerComp.ScannedSpeciesName = new PlanktonName("Unknown", "species");
        computerComp.ScannedCharacteristics = PlanktonCharacteristics.None;
        computerComp.ScannedDiet = null;
        computerComp.ScannedTempRangeLow = 0.0f;
        computerComp.ScannedTempRangeHigh = 0.0f;

        computerComp.PaperGenerated = false;
        computerComp.SampleLoadTime = TimeSpan.Zero;

        // public popup message
        _popup.PopupEntity(Loc.GetString("plankton-computer-purge-data-message"), uid, PopupType.Medium);
    }

    /// <summary>
    ///     The method handling the full generation of the research paper.
    /// </summary>
    /// <param name="uid">PlanktonAnalysisComputer UID</param>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    /// <param name="user">The user</param>
    private void GenerateResearchPaper(EntityUid uid, Components.Machines.PlanktonAnalysisComputerComponent computerComp, EntityUid user)
    {
        // A 'null' check for the plankton species name, aka if it
        // matches 'Unknown'. We also check if the paper has already been generated.
        if (computerComp.ScannedSpeciesName.FirstName == "Unknown" || computerComp.PaperGenerated)
            return;

        // Now we set up some variables for the paper generation!
        // This includes the plankton's species, the user's name, and
        // the current time as seen in the PDA. (hopefully)
        var planktonName = computerComp.ScannedSpeciesName;
        var username = MetaData(user).EntityName;
        var timestamp = _gameTiming.CurTime.ToString("HH:mm:ss");

        // Now spawn a jellid-proof paper entity and fill it using another method.
        // Once finished, we set the paper-generated flag to true.
        // We also play the extract sound effect (a print sound) and display a public popup message.
        var paperEnt = Spawn("TP14PaperJellid", Transform(uid).Coordinates);
        if (TryComp<PaperComponent>(paperEnt, out var paperComp))
            paperComp.Content = GeneratePaperContent(planktonName, username, timestamp);

        computerComp.PaperGenerated = true;

        _audio.PlayPvs(computerComp.ExtractSound, uid);
        _popup.PopupEntity(Loc.GetString("plankton-computer-print-paper-message"), uid, PopupType.Medium);
    }

    /// <summary>
    ///     The method handling the generation of research paper contents.
    /// </summary>
    /// <param name="planktonName">Plankton Name Data</param>
    /// <param name="username">User's name</param>
    /// <param name="timestamp">The time when the paper was generated</param>
    /// <param name="formId">A nullable formID (int?)</param>
    /// <returns>The research paper contents</returns>
    private string GeneratePaperContent(PlanktonName planktonName, string username, string timestamp, int? formId = null)
    {
        var newFormId = formId ?? _random.Next(9999);

        return
            $"""
            [color=#92beca]▀█▀[/color] [color=#af7244]█▀█[/color]   │  [head=3][color=#141d2b]Trieste Planktology Research Form[/color][/head]
            [color=#ffffff]░[/color][color=#92beca]█[/color][color=#ffffff]░[/color] [color=#af7244]█▀▀[/color]   │  [color=#141d2b][bold]Research Form PL-{newFormId:D4}[/bold][/color]
            ─────────────────────────────────────────
            [head=2]Observations Field[/head]
            [head=3]Species: {planktonName}

            1. Diet: ________________

            2. Temperature Range: ___ to ___ °C

            3. Characteristics: ________  / ________  / ________

            4. Size: ________[/head]

            ─────────────────────────────────────────
            [head=2]Researcher Field[/head]

            [head=3]Researcher Name: {username}
            Scan Time: {timestamp}[/head]

            ─────────────────────────────────────────
            {GetGuideSection()}
            """;
    }

    /// <summary>
    ///     The guides section for the paper. Aka the enum values.
    /// </summary>
    /// <returns>Entire guide section (String)</returns>
    private static string GetGuideSection()
    {
        return """
            [head=2]Characteristics Guide[/head]
            [bold][italic]Please note that higher risk = more reward![/italic][/bold]
            [bold]Low Risk                             Medium Risk[/bold]
            [italic]1.1 Cryophilic                      2.1 Agressive
            1.2 Pyrophilic                      2.2 Charged
            1.3 Bioluminescent            2.3 Radioactive
            1.4 Magnetic Field              2.4 Hallucinogenic
            1.5 Chemical Production   2.5 Pheromone Glands[/italic]

            [bold]High Risk                             Ultra-Rare[/bold]
            [italic]3.1 Polyp Colony                  4.1 Hyper-Exotic Species
            3.2 Parasitic                          4.2 Sapience
            3.3 Pyrotechnic
            3.4 Mimicry
            3.5 Aerosol Spores
            3.6 Violent Symbiote[/italic]
            ─────────────────────────────────────────
            [head=2]Diets Guide[/head]
            [bold][italic]Please note that more complex = more reward[/italic][/bold]
            [bold]Simple                                  Complex[/bold]
            [italic]1.1 Photosynthetic              2.1 Carnivore
            1.2 Decomposer                  2.2 Symbiotroph
            1.3 Scavenger                      2.3 Chemotroph[/italic]

            [bold]Specialized[/bold]
            [italic]3.1 Saguinophage
            3.2 Electrotroph
            3.3 Radiotroph
            3.4 Parasite[/italic]
            """;
    }

    /// <summary>
    ///     Display some text if the computer has a sample loaded.
    ///     Text is set to priority 10. (high)
    /// </summary>
    /// <param name="uid">PlanktonAnalysisComputer UID</param>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    /// <param name="args">ExaminedEvent Arguments</param>
    private void OnExamined(EntityUid uid, Components.Machines.PlanktonAnalysisComputerComponent computerComp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (computerComp.ScannedSpeciesName.FirstName == "Unknown")
            return;

        args.AddMarkup(Loc.GetString("plankton-research-loaded-sample"), 10);
    }

    /// <summary>
    ///     Interacting with the computer will either load a sample or a generated paper.
    /// </summary>
    /// <param name="uid">PlanktonAnalysisComputer UID</param>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    /// <param name="args">InteractUsingEvent Arguments</param>
    private void OnInteractUsing(EntityUid uid, Components.Machines.PlanktonAnalysisComputerComponent computerComp, InteractUsingEvent args)
    {
        // First we check if the used item is a vial, then run the scan method.
        // If, instead, it's a paper, we run the paper validation method.
        if (TryComp<PlanktonVialComponent>(args.Used, out var vialComp))
        {
            HandlePlanktonScan(computerComp, vialComp, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<PaperComponent>(args.Used, out var paperComp))
        {
            HandlePaperValidation(uid, computerComp, paperComp);
            QueueDel(args.Used);
            args.Handled = true;
            return;
        }
    }

    /// <summary>
    ///     Handles the validation of the research paper and
    ///     awards the Science department points.
    /// </summary>
    /// <param name="uid">PlanktonAnalysisComputer UID</param>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    /// <param name="paperComp">Paper Component</param>
    private void HandlePaperValidation(EntityUid uid,
        Components.Machines.PlanktonAnalysisComputerComponent computerComp,
        PaperComponent paperComp)
    {
        // If there's no sample in the computer, just return.
        // Also return if the plankton sample in the computer is invalid.
        if (computerComp.ScannedSpeciesName.FirstName == "Unknown")
            return;

        var points = ValidateResearch(paperComp.Content, computerComp);

        AwardResearchPoints(uid, computerComp, points);
        PurgePlanktonData(uid, computerComp);
    }

    // These were made to stop the strange warnings in Rider about static calls.
    // This is apparently more performant than said regex calls, as well. - Cookie (FatherCheese)
    [GeneratedRegex(@"1\.\s*Diet:\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex DietRegex();

    [GeneratedRegex(@"2\.\s*Temperature Range:\s*(\d+)\s*to\s*(\d+)\s*°C")]
    private static partial Regex TempRegex();

    [GeneratedRegex(@"3\.\s*Characteristics:\s*([^\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CharRegex();

    [GeneratedRegex(@"4\.\s*Size:\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex SizeRegex();

    /// <summary>
    ///     This function validates the research paper and compares
    ///     the answers to the loaded Plankton.
    /// </summary>
    /// <param name="content">Paper Content</param>
    /// <param name="computerComp">PlanktonAnaylsisComputer Component</param>
    /// <returns>Total research points based on correct answers (integer)</returns>
    private int ValidateResearch(string content, Components.Machines.PlanktonAnalysisComputerComponent computerComp)
    {
        // Null checks so it stops being stupid with nullability types.
        if (computerComp.ScannedDiet == null ||
            computerComp.ScannedCharacteristics == PlanktonCharacteristics.None ||
            computerComp.ScannedSpeciesName.FirstName == "Unknown")
            return 0;

        // Setup points, correct answers, and the total questions
        // The total questions are: Diet, Temperature, Characteristics, and Size
        var totalPoints = 0;
        var correctAnswers = 0;
        const int totalQuestions = 4;

        // Check the Diet answer and try to parse it
        var dietMatch = DietRegex().Match(content);
        if (dietMatch.Success)
        {
            var answer = dietMatch.Groups[1].Value;
            if (Enum.TryParse<PlanktonDiet>(answer, true, out var parsedDiet) && parsedDiet == computerComp.ScannedDiet)
            {
                totalPoints += SharedPlanktonSpeciesData.DietResearchValues[computerComp.ScannedDiet.Value];
                correctAnswers++;
            }
        }

        // Check the Temperature range and try to parse with a +5 tolerance
        // These don't have any data for the tolerance levels, so we'll give 10 (100) points for each.
        var tempMatch = TempRegex().Match(content);
        if (tempMatch.Success)
        {
            if (float.TryParse(tempMatch.Groups[1].Value, out var minTemp) &&
                float.TryParse(tempMatch.Groups[2].Value, out var maxTemp))
            {
                if (Math.Abs(minTemp - computerComp.ScannedTempRangeLow) <= 5 &&
                    Math.Abs(maxTemp - computerComp.ScannedTempRangeHigh) <= 5)
                {
                    totalPoints += 10;
                    correctAnswers++;
                }
            }
        }

        // Check the three Characteristics answers, then compare to the
        // plankton's characteristics. Reward points for each correct answer.
        // This makes sure to also split the answer by '/' and trim the whitespace.
        // This is also case-insensitive.
        var charMatch = CharRegex().Match(content);
        if (charMatch.Success)
        {
            var answer = charMatch.Groups[1].Value.ToLower();
            var characteristics = answer.Split('/').Select(c => c.Trim()).ToArray();
            var foundCharacteristics = new List<PlanktonCharacteristics>();

            foreach (var characteristic in Enum.GetValues<PlanktonCharacteristics>())
            {
                if (characteristic == PlanktonCharacteristics.None)
                    continue;

                if (computerComp.ScannedCharacteristics.HasFlag(characteristic))
                {
                    var charName = characteristic.ToString().ToLower();
                    if (characteristics.Any(c => c.Contains(charName)))
                    {
                        foundCharacteristics.Add(characteristic);
                    }
                }
            }

            foreach (var characteristic in foundCharacteristics)
            {
                totalPoints += SharedPlanktonSpeciesData.CharacteristicResearchValues[characteristic];
            }

            if (foundCharacteristics.Count > 0)
                correctAnswers++;
        }

        // Check the Size field - this is a new field that needs handling
        var sizeMatch = SizeRegex().Match(content);
        if (sizeMatch.Success)
        {
            var answer = sizeMatch.Groups[1].Value;
            if (Enum.TryParse<PlanktonSize>(answer, true, out var parsedSize) && parsedSize == computerComp.ScannedSize)
            {
                totalPoints += 5;
                correctAnswers++;
            }
        }

        // Correct completion bonus, and total point multiplier.
        // 195 points total is bad, to put it simply. So we multiply it by 30.
        // TODO - Balance out the multiplier, as Nodes can give 10k now!
        if (correctAnswers == totalQuestions)
        {
            totalPoints = (int)(totalPoints * 1.5f); // 50% bonus for perfect completion
        }

        return totalPoints * 30;
    }

    /// <summary>
    ///     This method handles the award points to the Science department.
    /// </summary>
    /// <param name="computerUid">PlanktonAnalysisComputer UID</param>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    /// <param name="points">Number of points to add (integer)</param>
    private void AwardResearchPoints(EntityUid computerUid, Components.Machines.PlanktonAnalysisComputerComponent computerComp, int points)
    {
        if (points <= 0)
            return;

        if (!_research.TryGetClientServer(computerUid, out var server, out var serverComp))
            return;

        _research.ModifyServerPoints(server.Value, points, serverComp);

        _audio.PlayPvs(computerComp.ExtractSound, computerUid);
        _popup.PopupEntity(Loc.GetString("plankton-research-success-message", ("points", points)),
            computerUid,
            PopupType.Medium);
    }

    /// <summary>
    ///     This method handles the loading of a sample into the computer.
    /// </summary>
    /// <param name="computerComp">PlanktonAnalysisComputer Component</param>
    /// <param name="vialComp">PlanktonVial Component</param>
    /// <param name="user">User UID</param>
    private void HandlePlanktonScan(Components.Machines.PlanktonAnalysisComputerComponent computerComp, PlanktonVialComponent vialComp, EntityUid user)
    {
        // Check if the vial has a plankton component inside. If not, then return.
        if (!TryComp<PlanktonComponent>(vialComp.ContainedSpecimen, out var planktonComp))
            return;

        // Check if the computer already has a sample. If so, then return
        // and display a client-side popup message.
        if (computerComp.ScannedSpeciesName.FirstName != "Unknown")
        {
            _popup.PopupCursor(Loc.GetString("plankton-analysis-component-sample-not-null-message"));
            return;
        }

        // Set the computer sample to a copy of the plankton sample values,
        // set the paper-generated flag to false,
        // and set the sample load time to the current time.
        computerComp.ScannedSpeciesName = planktonComp.SpeciesName;
        computerComp.ScannedCharacteristics = planktonComp.Characteristics;
        computerComp.ScannedDiet = planktonComp.Diet;
        computerComp.ScannedTempRangeLow = planktonComp.TemperatureToleranceLow;
        computerComp.ScannedTempRangeHigh = planktonComp.TemperatureToleranceHigh;

        computerComp.PaperGenerated = false;
        computerComp.SampleLoadTime = _gameTiming.CurTime;

        // client-only popup message
        _popup.PopupCursor(Loc.GetString("plankton-analysis-component-sample-loaded-message"),
            PopupType.Medium);

        // public popup message
        _popup.PopupEntity(Loc.GetString("plankton-analysis-component-sample-loaded-others-message", ("user", user)),
            user,
            Filter.PvsExcept(user),
            true,
            PopupType.Medium);
    }
}
