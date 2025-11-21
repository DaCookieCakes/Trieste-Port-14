tp14-reagent-boiling-point = [bold]The boiling point of {$reagent} is {$temp}[/bold]
tp14-reagent-compound-type = [bold]{$reagent} has the compound type(s) of {$types}[/bold]

tp14-reagent-separation-methods =
    { $method ->
        [Gas] [bold]{$reagent} must be distilled and collected in a gas bottle[/bold]
        [Liquid] [bold]{$reagent} must be distilled and collected in a container[/bold]
        [Metal] [bold]{$reagent} must be separated using electrolysis[/bold]
        [Solid] [bold]{$reagent} must be filtered out from liquids[/bold]
       *[other] Unknown
    }
