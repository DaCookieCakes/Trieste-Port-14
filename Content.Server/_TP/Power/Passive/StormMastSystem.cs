using Content.Server.DoAfter;
using Content.Shared._TP.Power.Generator;
using Content.Shared._TP.Power.Passive;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Server._TP.Power.Passive;

/// <summary>
///     Part of the Engi overhaul for Trieste.
///     Must be activated along-side Superconducting Coils to run the Storm Array.
/// </summary>
public sealed class StormMastSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StormMastComponent, GetVerbsEvent<ActivationVerb>>(OnVerbActivation);
        SubscribeLocalEvent<StormMastComponent, StormMastEnableDoAfter>(OnStormMastEnabled);
        SubscribeLocalEvent<StormMastComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<StormMastComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp is { Damaged: true })
        {
            if (MetaData(args.Used).EntityPrototype?.ID == "SheetSteel1")
            {
                S&
            }
        }
    }

    private void OnStormMastEnabled(Entity<StormMastComponent> ent, ref StormMastEnableDoAfter args)
    {
        if (args.Cancelled)
            return;

        ent.Comp.Enabled = true;

        _popup.PopupEntity(Loc.GetString("storm-mast-message-enabled"), ent.Owner, args.User, PopupType.Medium);

        _appearance.SetData(ent.Owner, StormArrayVisuals.Idle, false);
        _appearance.SetData(ent.Owner, StormArrayVisuals.Active, true);
    }

    private void OnVerbActivation(Entity<StormMastComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.Enabled || ent.Comp.Damaged || ent.Comp.Patched)
            return;

        var user = args.User;
        var verb = new ActivationVerb()
        {
            Act = () => HandleEnabling(ent, user),
            Text = Loc.GetString("storm-mast-verb-enable"),
            Message = Loc.GetString("storm-mast-verb-enabling"),
        };

        args.Verbs.Add(verb);
    }

    private void HandleEnabling(Entity<StormMastComponent> ent, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("storm-mast-message-enabling"), ent.Owner, user, PopupType.Medium);

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            TimeSpan.FromSeconds(5),
            new StormMastEnableDoAfter(),
            ent.Owner,
            ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }
}
