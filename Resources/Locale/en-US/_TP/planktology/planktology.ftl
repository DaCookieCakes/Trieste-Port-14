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
}
plankton-scanner-report-dead = Amount of dead plankton: {$amount}

plankton-scanner-no-plankton = There are no living plankton in the sample!
plankton-scanner-too-many-plankton = There are too many plankton species in the sample!
plankton-scanner-too-small = The plankton culture is too small!
plankton-scanner-reward = The analyzer prints a disk full of data!
