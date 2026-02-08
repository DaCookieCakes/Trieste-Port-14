using System.Linq;
using Content.Shared._TP.Plankton;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Radiation.Components;
using Content.Shared.Speech;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._TP.Plankton;

public sealed class PlanktonSeparatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanktonSeparatorComponent, GetVerbsEvent<Verb>>(AddVerbs);
        SubscribeLocalEvent<PlanktonSeparatorComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<PlanktonSeparatorComponent, ExaminedEvent>(OnExamined);
    }

    private const float UpdateInterval = 1f;
    private float _updateTimer;
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer >= UpdateInterval)
        {
            var query = EntityQueryEnumerator<PlanktonSeparatorComponent>();
            while (query.MoveNext(out var separatorUid, out var separatorComp))
            {
                if (!_container.TryGetContainer(separatorUid, "plankton_container_slot", out var slot)
                    || slot.ContainedEntities.Count == 0)
                    continue;

                if (!TryComp<PlanktonComponent>(slot.ContainedEntities[0], out var planktonComp))
                    continue;

                if (planktonComp.SpeciesInstances.Count <= 0)
                    continue;

                if (separatorComp.NextSeparatorTime <= _timing.CurTime && !separatorComp.Separated)
                {
                    _audio.PlayPvs(separatorComp.SeparationSound, Transform(separatorUid).Coordinates);
                    separatorComp.Separated = true;
                }
            }

            _updateTimer = 0;
        }
    }

    private void OnExamined(EntityUid separatorUid, PlanktonSeparatorComponent separatorComp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_container.TryGetContainer(separatorUid, "plankton_container_slot", out var slot)
            || slot.ContainedEntities.Count == 0)
        {
            args.PushMarkup(Loc.GetString("comp-plankton-separator-no-container"));
            return;
        }

        if (!TryComp<PlanktonComponent>(slot.ContainedEntities[0], out var planktonComp))
            return;

        if (planktonComp.SpeciesInstances.Count <= 0)
        {
            args.PushMarkup(Loc.GetString("plankton-separator-no-plankton"));
            return;
        }

        var separation = (separatorComp.NextSeparatorTime - _timing.CurTime).TotalSeconds;
        args.PushMarkup(separatorComp.NextSeparatorTime > _timing.CurTime
            ? Loc.GetString("plankton-separator-timer", ("time", $"{separation:F0}"))
            : Loc.GetString("plankton-separator-ready"));
    }

    private void OnContainerInserted(EntityUid separatorUid, PlanktonSeparatorComponent separatorComp, EntInsertedIntoContainerMessage args)
    {
        separatorComp.NextSeparatorTime = _timing.CurTime + TimeSpan.FromSeconds(separatorComp.SeparateInterval);
        separatorComp.Separated = false;
    }

    private void AddVerbs(EntityUid separatorUid, PlanktonSeparatorComponent separatorComp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Separator must have plankton component to hold species
        if (!TryComp<PlanktonComponent>(separatorUid, out var separatorPlankton))
            return;

        // Must have a container inserted
        if (!_container.TryGetContainer(separatorUid, "plankton_container_slot", out var slot)
            || slot.ContainedEntities.Count == 0)
            return;

        var containerEntity = slot.ContainedEntities[0];

        if (!TryComp<PlanktonComponent>(containerEntity, out var containerPlankton))
            return;

        foreach (var species in containerPlankton.SpeciesInstances.ToList())
        {
            if (separatorComp.NextSeparatorTime > _timing.CurTime)
                continue;

            Verb insertVerb = new()
            {
                Text = Loc.GetString("plankton-separator-insert-species", ("species", species.SpeciesName.ToString())),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () =>
                {
                    containerPlankton.SpeciesInstances.Remove(species);
                    separatorPlankton.SpeciesInstances.Add(species);
                    separatorComp.NextSeparatorTime = _timing.CurTime + TimeSpan.FromSeconds(separatorComp.SeparateInterval);

                    if (!TryComp<MindContainerComponent>(containerPlankton.Owner, out var mindContainer))
                        return;

                    if (mindContainer.Mind == null)
                        return;

                    _mind.TransferTo(mindContainer.Mind.Value, separatorPlankton.Owner);
                    EnsureComp<SpeechComponent>(separatorPlankton.Owner);
                    EnsureComp<TypingIndicatorComponent>(separatorPlankton.Owner);

                    RemComp<PointLightComponent>(containerPlankton.Owner);
                    RemComp<ElectrifiedComponent>(containerPlankton.Owner);
                    RemComp<RadiationSourceComponent>(containerPlankton.Owner);
                },
                Priority = -2,
            };
            args.Verbs.Add(insertVerb);
        }

        foreach (var species in separatorPlankton.SpeciesInstances.ToList())
        {
            Verb extractVerb = new()
            {
                Text = Loc.GetString("plankton-separator-extract-species", ("species", species.SpeciesName.ToString())),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    separatorPlankton.SpeciesInstances.Remove(species);
                    containerPlankton.SpeciesInstances.Add(species);
                    separatorComp.NextSeparatorTime = _timing.CurTime + TimeSpan.FromSeconds(separatorComp.SeparateInterval);

                    _audio.PlayPvs(separatorComp.ExtractSound, separatorUid);
                    _popup.PopupEntity(Loc.GetString("plankton-separator-extracted", ("species", species.SpeciesName)), separatorUid);

                    if (!TryComp<MindContainerComponent>(separatorPlankton.Owner, out var mindContainer))
                        return;

                    if (mindContainer.Mind == null)
                        return;

                    _mind.TransferTo(mindContainer.Mind.Value, containerPlankton.Owner);
                    EnsureComp<SpeechComponent>(containerPlankton.Owner);
                    EnsureComp<TypingIndicatorComponent>(containerPlankton.Owner);

                    RemComp<PointLightComponent>(separatorPlankton.Owner);
                    RemComp<ElectrifiedComponent>(separatorPlankton.Owner);
                    RemComp<RadiationSourceComponent>(separatorPlankton.Owner);
                },
                Priority = -1
            };
            args.Verbs.Add(extractVerb);
        }
    }
}
