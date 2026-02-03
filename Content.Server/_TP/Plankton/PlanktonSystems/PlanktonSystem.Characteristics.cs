using System.Linq;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._TP.Plankton;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._TP.Plankton.PlanktonSystems;

public sealed partial class PlanktonSystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static bool HasCharacteristic(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, PlanktonComponent.PlanktonCharacteristics characteristics)
    {
        return (planktonSpecies.Characteristics & characteristics) != 0;
    }

    public void UpdateCharacteristics(PlanktonComponent planktonComp, EntityUid planktonUid)
    {
        foreach (var planktonSpecies in planktonComp.SpeciesInstances.Where(planktonSpecies => planktonSpecies.IsAlive))
        {
            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.AerosolSpores) && _random.Prob(0.1F))
                PerformAerosolSpores(planktonSpecies, planktonUid);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.Bioluminescent))
                PerformBioluminescence(planktonSpecies, planktonUid);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.Charged))
                PerformCharged(planktonSpecies, planktonUid);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.MagneticField) && _random.Prob(0.1F))
                PerformMagneticField(planktonSpecies, planktonUid);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.PolypColony) && _random.Prob(0.05F))
                PerformPolypSpread(planktonSpecies, planktonUid);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.Radioactive))
                PerformRadiation(planktonSpecies, planktonUid);

            if (HasCharacteristic(planktonSpecies, PlanktonComponent.PlanktonCharacteristics.Sentience) && _random.Prob(0.1F))
                PerformSentience(planktonComp, planktonSpecies, planktonUid);
        }
    }

    private void PerformSentience(PlanktonComponent planktonComp, PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, EntityUid planktonUid)
    {
        if (!TryComp<GhostRoleComponent>(planktonUid, out var ghostRoleComp))
        {
            // Yes this typo is intentional. It's funny.
            var gost = EnsureComp<GhostRoleComponent>(planktonUid);
            gost.RoleName = Loc.GetString("plankton-component-ghost-role-name");
            gost.RoleDescription = Loc.GetString("plankton-component-ghost-role-description", ("species", planktonSpecies.SpeciesName));
            gost.RoleRules = Loc.GetString("plankton-component-ghost-role-rules");

            _ghostRole.RegisterGhostRole((planktonUid, gost));
            return;
        }

        _popup.PopupEntity(Loc.GetString("plankton-component-ghost-waking"), planktonUid);
    }

    private void PerformRadiation(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, EntityUid planktonUid)
    {
        if (!TryComp<RadiationSourceComponent>(planktonUid, out var radComp))
        {
            EnsureComp<RadiationSourceComponent>(planktonUid);
            return;
        }

        if (radComp.Enabled)
            return;

        radComp.Enabled = true;
        radComp.Intensity = planktonSpecies.CurrentSize * 0.2F;
    }

    private void PerformAerosolSpores(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, EntityUid planktonUid)
    {
        var xform = Transform(planktonUid);
        const float radius = 1.5F;

        var nearbyMobs = _lookup.GetEntitiesInRange<MobStateComponent>(xform.Coordinates, radius);

        foreach (var (mobUid, _) in nearbyMobs)
        {
            // Check for internals
            if (TryComp<InternalsComponent>(mobUid, out var internals) && internals.BreathTools.Count != 0)
                continue;

            // Infect them
            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Cellular", 2);
            _damageable.TryChangeDamage(mobUid, damage);

            _popup.PopupEntity(Loc.GetString("plankton-inhaled-spores"), mobUid, mobUid);
        }
    }

    /// <summary>
    ///     Spreads coral in a 3x3 area
    /// </summary>
    /// <param name="planktonSpecies">Plankton Instance Species</param>
    /// <param name="planktonUid">Plankton UID</param>
    private void PerformPolypSpread(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, EntityUid planktonUid)
    {
        var xForm = Transform(planktonUid);
        if (!TryComp<MapGridComponent>(xForm.GridUid, out var gridComp))
            return;

        var position = _transform.GetGridOrMapTilePosition(planktonUid, xForm);
        var emptyTiles = new List<Vector2i>();
        for (var x = -1; x <= 1; x++)
        {
            for (var z = -1; z <= 1; z++)
            {
                var offset = new Vector2i(x, z);
                var tilePos = position + offset;

                if (!gridComp.TryGetTileRef(tilePos, out _))
                    continue;

                var checkCoords = new EntityCoordinates(xForm.GridUid.Value, tilePos.X + 0.5f, tilePos.Y + 0.5f);
                var entitiesOnTile = _lookup.GetEntitiesInRange(checkCoords, 0.5f);

                if (entitiesOnTile.All(e => e == planktonUid))
                    emptyTiles.Add(tilePos);
            }
        }

        if (emptyTiles.Count == 0)
            return;

        var randTile = _random.Pick(emptyTiles);
        var gridCoords = new EntityCoordinates(xForm.GridUid.Value, randTile.X + 0.5f, randTile.Y + 0.5f);
        Spawn("TP14CoralWallRed", gridCoords);
    }

    /// <summary>
    ///     Performs magnetic field disruptions
    /// </summary>
    /// <param name="planktonSpecies">The Plankton Instance</param>
    /// <param name="planktonUid">The Plankton UID</param>
    private void PerformMagneticField(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, EntityUid planktonUid)
    {
        // Check the nearby radius (3x3) for Electronic components.
        const float radius = 1.5F;
        var xForm = Transform(planktonUid);
        var nearbyBatteries = _lookup.GetEntitiesInRange<BatteryComponent>(xForm.Coordinates, radius);

        foreach (var (batteryUid, _) in nearbyBatteries)
        {
            const float baseAmount = 2F;
            var scaledAmount = baseAmount * planktonSpecies.CurrentSize;
            var chargeChange = _random.Prob(0.5f) ? scaledAmount : -scaledAmount;

            _battery.ChangeCharge(batteryUid, chargeChange);
        }
    }

    /// <summary>
    ///     Electrifies plankton
    /// </summary>
    /// <param name="planktonSpecies"></param>
    /// <param name="planktonUid"></param>
    private void PerformCharged(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, EntityUid planktonUid)
    {
        if (!TryComp<ElectrifiedComponent>(planktonUid, out _))
        {
            var electrified = EnsureComp<ElectrifiedComponent>(planktonUid);
            electrified.RequirePower = false;
        }
    }

    private void PerformBioluminescence(PlanktonComponent.PlanktonSpeciesInstance planktonSpecies, EntityUid planktonUid)
    {
        if (!TryComp<PointLightComponent>(planktonUid, out var lightComp))
        {
            var light = EnsureComp<PointLightComponent>(planktonUid);

            if (!light.Enabled)
            {
                _pointLight.SetEnergy(planktonUid, 16F);
                _pointLight.SetRadius(planktonUid, 0.25F);
                _pointLight.SetEnabled(planktonUid, true);
            }
        }
    }
}
