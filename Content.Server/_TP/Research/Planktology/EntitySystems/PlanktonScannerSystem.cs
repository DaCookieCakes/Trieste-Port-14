using System.Linq;
using System.Text;
using Content.Shared._TP.Research.Planktology;
using Content.Shared._TP.Research.Planktology.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server._TP.Research.Planktology.EntitySystems;

public sealed partial class PlanktonScannerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<PlanktonScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<PlanktonComponent>(target, out var plankton))
            return;

        Scan(ent, (target, plankton), args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetUtilityVerbs(Entity<PlanktonScannerComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<PlanktonComponent>(args.Target, out var plankton))
            return;

        var target = args.Target;
        var user = args.User;
        args.Verbs.Add(new UtilityVerb
        {
            Text = Loc.GetString("plankton-scanner-scan-verb"),
            Act = () => Scan(ent, (target, plankton), user),
        });
    }

    [SubscribeLocalEvent]
    private void OnGetActivationVerbs(Entity<PlanktonScannerComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("plankton-scanner-toggle-mode-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => ToggleMode(ent, user),
            Priority = -1,
        });
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<PlanktonScannerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.AnalysisMode
            ? "plankton-scanner-examine-analysis-mode"
            : "plankton-scanner-examine-scan-mode"));
    }

    private void ToggleMode(Entity<PlanktonScannerComponent> ent, EntityUid user)
    {
        ent.Comp.AnalysisMode = !ent.Comp.AnalysisMode;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString(ent.Comp.AnalysisMode
                ? "plankton-scanner-switched-analysis-mode"
                : "plankton-scanner-switched-scan-mode"),
            ent,
            user);
    }

    private void Scan(Entity<PlanktonScannerComponent> scanner, Entity<PlanktonComponent> sample, EntityUid user)
    {
        if (TryComp<UseDelayComponent>(scanner, out var useDelay)
            && !_useDelay.TryResetDelay((scanner, useDelay), true))
            return;

        if (scanner.Comp.AnalysisMode)
            Analyze(scanner, sample, user);
        else
            PrintReport(scanner, sample, user);
    }

    private void PrintReport(Entity<PlanktonScannerComponent> scanner, Entity<PlanktonComponent> sample, EntityUid user)
    {
        var species = sample.Comp.Species;
        var count = Loc.GetString("plankton-scanner-species-count", ("count", species.Count));
        _popup.PopupEntity(count, sample, user);

        var report = new StringBuilder();
        report.AppendLine(count);

        if (species.Count > 0)
        {
            report.AppendLine(Loc.GetString("plankton-scanner-report-species-header"));
            foreach (var s in species)
            {
                report.AppendLine(Loc.GetString("plankton-scanner-report-species-entry",
                    ("name", s.Name),
                    ("alive", s.Alive),
                    ("size", (int) MathF.Round(s.Size))));
            }

            report.AppendLine(Loc.GetString("plankton-scanner-report-dead",
                ("amount", (int) MathF.Round(sample.Comp.DeadPlankton))));
        }

        var paper = SpawnAtPosition(scanner.Comp.ReportPrototype, Transform(scanner).Coordinates);
        _metaData.SetEntityName(paper, Loc.GetString("plankton-scanner-report-title"));
        _paper.SetContent(paper, report.ToString());
        _audio.PlayPvs(scanner.Comp.PrintSound, scanner);
    }

    /// <summary>
    ///     Converts a single large living culture into a research disk.
    ///     The analyzed species is removed from the sample.
    /// </summary>
    private void Analyze(Entity<PlanktonScannerComponent> scanner, Entity<PlanktonComponent> sample, EntityUid user)
    {
        var living = sample.Comp.Species.Where(s => s.Alive).ToList();

        switch (living.Count)
        {
            case 0:
                _popup.PopupEntity(Loc.GetString("plankton-scanner-no-plankton"), sample, user);
                return;
            case > 1:
                _popup.PopupEntity(Loc.GetString("plankton-scanner-too-many-plankton"), sample, user);
                return;
        }

        var species = living[0];
        if (species.Size < scanner.Comp.MinimumAnalysisSize)
        {
            _popup.PopupEntity(Loc.GetString("plankton-scanner-too-small"), sample, user);
            return;
        }

        var reward = species.Has(PlanktonCharacteristics.HyperExoticSpecies)
            ? scanner.Comp.AdvancedRewardPrototype
            : scanner.Comp.RewardPrototype;

        sample.Comp.Species.Remove(species);
        Dirty(sample);

        SpawnAtPosition(reward, Transform(scanner).Coordinates);
        _popup.PopupEntity(Loc.GetString("plankton-scanner-reward"), scanner, user);
        _audio.PlayPvs(scanner.Comp.PrintSound, scanner);
    }
}
