using System.Linq;
using Content.Server.Popups;
using Content.Shared._TP.Plankton;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._TP.Planktonics;

public sealed partial class PlanktonScannerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedGameTicker _gameTicker = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private MetaDataSystem _metaSystem = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlanktonScannerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<PlanktonScannerComponent, GetVerbsEvent<UtilityVerb>>(AddScanVerb);
        SubscribeLocalEvent<PlanktonScannerComponent, GetVerbsEvent<ActivationVerb>>(AddToggleAnalysisVerb);
        SubscribeLocalEvent<PlanktonScannerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnBeforeRangedInteract(EntityUid uid,
        PlanktonScannerComponent component,
        BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !args.Target.HasValue)
            return;

        var target = args.Target.Value;
        if (!TryComp<PlanktonComponent>(target, out var plankton))
            return;

        CreatePopup(uid, target, plankton, component);

        args.Handled = true;
    }


    private void AddScanVerb(EntityUid uid, PlanktonScannerComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!TryComp<PlanktonComponent>(args.Target, out var plankton))
            return;

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                CreatePopup(uid, args.Target, plankton, component);
            },
            Text = Loc.GetString("plankton-scan-tooltip")
        };

        args.Verbs.Add(verb);
    }

    private void AddToggleAnalysisVerb(EntityUid uid,
        PlanktonScannerComponent component,
        GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString("toggle-analysis-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => TryToggleAnalysis((uid, component), args.User),
            Priority = -1
        };

        args.Verbs.Add(verb);
    }


    private void TryToggleAnalysis((EntityUid, PlanktonScannerComponent) data, EntityUid user)
    {
        var (_, component) = data;
        component.AnalysisMode = !component.AnalysisMode;
    }

    private void CreatePopup(EntityUid uid,
        EntityUid target,
        PlanktonComponent component,
        PlanktonScannerComponent scanner)
    {
        if (TryComp(uid, out UseDelayComponent? useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        var reportContent = GenerateReportContent(component);

        if (scanner.AnalysisMode && component.SpeciesInstances.Count == 1)
        {
            var species = component.SpeciesInstances.First();
            if (species.CurrentSize >= 50)
            {
                SpawnReward(uid, target, species, scanner);
            }
            else
            {
                _popupSystem.PopupEntity("plankton-too-small-alert", target);
            }
        }
        else
        {
            ShowMultipleSpeciesAlert(target, component.SpeciesInstances.Count);
            CreatePaperReport(uid, target, scanner, reportContent);
        }
    }

    private string GenerateReportContent(PlanktonComponent component)
    {
        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        var header = "Scan time: " + stationTime.ToString("hh\\:mm\\:ss");

        var message = Loc.GetString("plankton-scan-popup", ("count", $"{component.SpeciesInstances.Count}"));
        header += "\n\n" + message + "\n";

        // Format each species with its details
        foreach (var species in component.SpeciesInstances)
        {
            if ((species.Characteristics & PlanktonComponent.PlanktonCharacteristics.Mimicry) != 0)
            {
                if (_random.Prob(0.6F) && species.IsAlive)
                    continue;
            }

            var status = species.IsAlive ? "ALIVE" : "DEAD";
            header += $"\n{species.SpeciesName} - {status}";
            header += $"\n  Size: {species.CurrentSize:F1}";
            header += $"\n  Hunger: {species.CurrentHunger:F1}";
        }

        header += $"\n\nTotal dead plankton: {component.DeadPlankton}";

        return header;
    }

    private void CreatePaperReport(EntityUid uid, EntityUid target, PlanktonScannerComponent scanner, string content)
    {
        var report = Spawn(scanner.PlanktonReportEntityId, Transform(uid).Coordinates);
        _metaSystem.SetEntityName(report,
            Loc.GetString("plankton-analysis-report-title", ("id", $"Plankton Scan Report")));
        _paper.SetContent(report, content);

        _popupSystem.PopupEntity(Loc.GetString("plankton-scan-popup"), target);
        _audioSystem.PlayPvs(scanner.PrintSound, uid);
    }

    private void SpawnReward(EntityUid uid,
        EntityUid target,
        PlanktonComponent.PlanktonSpeciesInstance species,
        PlanktonScannerComponent scanner)
    {
        var rewardId = (species.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0
            ? scanner.PlanktonAdvancedRewardEntityId
            : scanner.PlanktonRewardEntityId;

        Spawn(rewardId, Transform(uid).Coordinates);
        _popupSystem.PopupEntity(Loc.GetString("plankton-reward-popup"), target);
        _audioSystem.PlayPvs(scanner.PrintSound, uid);
    }

    private void ShowMultipleSpeciesAlert(EntityUid target, int count)
    {
        if (count > 1)
            _popupSystem.PopupEntity("too-many-plankton-alert", target);
        else if (count == 0)
            _popupSystem.PopupEntity("no-plankton-alert", target);
    }

    private void OnExamine(EntityUid uid, PlanktonScannerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = component.AnalysisMode
            ? "analysis-mode-on"
            : "analysis-mode-off";

        args.PushMarkup(Loc.GetString(text));
    }
}
