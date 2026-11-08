## Plankton samples

plankton-generate-verb = Generate plankton

## Plankton harvester

plankton-harvester-done = The harvester chimes and shuts off. Fresh colonies are in the container!
plankton-harvester-examine-no-container = There is no [color=yellow]plankton container[/color] inserted.
plankton-harvester-examine-progress = The current harvest is [color=cyan]{$percent}%[/color] filtered.

## Plankton culture tank

plankton-tank-no-power = The tank controls are unpowered.
plankton-tank-radiation-flash = The tank flashes with a sickly green light!
plankton-tank-full = The tank only holds {$max} colonies!
plankton-tank-no-server = The tank isn't linked to a research server.
plankton-tank-submit-correct = Research logged! The server gains {$points} points.
plankton-tank-submit-incorrect = The research was logged incorrectly. No points awarded.

plankton-tank-examine-lights-on = The tank lights are [color=yellow]on[/color].
plankton-tank-examine-lights-off = The tank lights are [color=gray]off[/color].
plankton-tank-examine-temperature = The water is [color=cyan]{$temperature}°C[/color].

plankton-tank-window-title = Plankton Culture Tank

plankton-tank-ui-beaker = Beaker
plankton-tank-ui-no-beaker = No beaker
plankton-tank-ui-inject-5 = +5u
plankton-tank-ui-inject-10 = +10u
plankton-tank-ui-tank-volume = Tank: {$volume}/{$max}u

plankton-tank-ui-container = Canister
plankton-tank-ui-no-container = No canister
plankton-tank-ui-container-count = {$count ->
    [one] 1 colony
    *[other] {$count} colonies
}
plankton-tank-ui-dump = Dump
plankton-tank-ui-extract = Extract

plankton-tank-ui-lights = Lights
plankton-tank-ui-cool = -10
plankton-tank-ui-heat = +10
plankton-tank-ui-temperature = {$temperature}°C
plankton-tank-ui-radiation = Radiation flash
plankton-tank-ui-radiation-cooldown = Recharging ({$seconds}s)
plankton-tank-ui-unpowered = [Unpowered]

plankton-tank-ui-form-empty = No colonies in the tank.
plankton-tank-ui-form-name = #{$number} {$name}
plankton-tank-ui-diet = DIET:
plankton-tank-ui-characteristics = CHAR:
plankton-tank-ui-submit = Submit research
plankton-tank-ui-diet-list = Pick one diet. X rules one out.
plankton-tank-ui-characteristic-list = Pick up to three characteristics ({$count}/3). X rules one out.
plankton-tank-ui-cross-out = Rule out

plankton-tank-ui-status-ready = Ready to submit.
plankton-tank-ui-status-small = Colony too small to submit.
plankton-tank-ui-status-dead = Colony is dead.
plankton-tank-ui-status-no-server = Not linked to a research server.
plankton-tank-ui-status-correct = Logged: correct.
plankton-tank-ui-status-incorrect = Logged: incorrect.

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
