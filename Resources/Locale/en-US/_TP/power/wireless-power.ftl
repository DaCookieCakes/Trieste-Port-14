wireless-power-dialog-title = Set the Wireless Channel
wireless-power-dialog-prompt = Channel (0-{ $max })
wireless-power-channel-set = Channel set to { $channel }.
wireless-power-channel-invalid = The channel must be between 0 and { $max }.

wireless-power-examine-channel = It is tuned to channel [color=cyan]{ $channel }[/color].
wireless-power-examine-unanchored = It needs to be [color=yellow]anchored[/color] to link.
wireless-power-examine-no-link = It isn't linked to anything on this channel.
wireless-power-examine-transmitting = It is transmitting [color=yellow]{ POWERWATTS($power) }[/color] to { $count } { $count ->
    [one] receiver
   *[other] receivers
} at [color=orange]{ $efficiency }%[/color] efficiency.
wireless-power-examine-receiving = It is receiving [color=yellow]{ POWERWATTS($power) }[/color] from { $count } { $count ->
    [one] transmitter
   *[other] transmitters
}.
