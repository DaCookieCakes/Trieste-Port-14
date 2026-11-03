## Plankton samples

plankton-generate-verb = Generate plankton

## Plankton scanner

plankton-scanner-scan-verb = Scan plankton sample
plankton-scanner-toggle-mode-verb = Toggle scanner mode

plankton-scanner-examine-analysis-mode = It is in [color=purple]analysis mode[/color].
plankton-scanner-examine-scan-mode = It is in [color=cyan]scan mode[/color].
plankton-scanner-switched-analysis-mode = The scanner is switched to analysis mode!
plankton-scanner-switched-scan-mode = The scanner is switched to scan mode!

plankton-scanner-species-count = Number of plankton species present: {$count}
plankton-scanner-report-title = plankton sample overview
plankton-scanner-report-species-header = Species names:
plankton-scanner-report-species-entry = {$name} - {$alive ->
    [true] ALIVE
    *[false] DEAD
}, size {$size}
plankton-scanner-report-dead = Amount of dead plankton: {$amount}

plankton-scanner-no-plankton = There are no living plankton in the sample!
plankton-scanner-too-many-plankton = There are too many plankton species in the sample!
plankton-scanner-too-small = The plankton culture is too small!
plankton-scanner-reward = The analyzer prints a disk full of data!

## Plankton harvester

plankton-harvester-done = The harvester chimes and shuts off. Fresh colonies are in the container!
plankton-harvester-examine-no-container = There is no [color=yellow]plankton container[/color] inserted.
plankton-harvester-examine-progress = The current harvest is [color=cyan]{$percent}%[/color] filtered.

## Plankton culture tank

plankton-tank-verb-lights-on = Turn on tank lights
plankton-tank-verb-lights-off = Turn off tank lights
plankton-tank-verb-heat = Raise temperature
plankton-tank-verb-cool = Lower temperature
plankton-tank-verb-radiation = Radiation flash

plankton-tank-no-power = The tank controls are unpowered.
plankton-tank-temperature = Tank temperature set to {$temperature}°C.
plankton-tank-radiation-flash = The tank flashes with a sickly green light!
plankton-tank-radiation-cooldown = The radiation emitter is still recharging.

plankton-tank-poured = You pour {$count ->
    [one] a colony
    *[other] {$count} colonies
} into the tank.
plankton-tank-collected = You collect {$count ->
    [one] a colony
    *[other] {$count} colonies
} from the tank.
plankton-tank-both-empty = There's nothing to pour or collect.

plankton-tank-examine-lights-on = The tank lights are [color=yellow]on[/color].
plankton-tank-examine-lights-off = The tank lights are [color=gray]off[/color].
plankton-tank-examine-temperature = The water is [color=cyan]{$temperature}°C[/color].

## Plankton separator

plankton-separator-window-title = Plankton Separator
plankton-separator-deposit = Deposit all
plankton-separator-extract = Extract
plankton-separator-frozen-header = Frozen colonies
plankton-separator-empty = Nothing frozen.
plankton-separator-no-container = No container inserted.
plankton-separator-container = Container: {$count ->
    [one] 1 colony
    *[other] {$count} colonies
}
plankton-separator-entry = {$name} ({$alive ->
    [true] alive
    *[false] dead
}, size {$size})
