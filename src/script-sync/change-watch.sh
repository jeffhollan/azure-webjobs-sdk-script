#!/bin/bash

src=$SCRIPT_SYNC_SOURCE
dst=$SCRIPT_SYNC_DEST

echo "Observing changes in directory  $src"
echo "Publishing changes to directory $dst"

if [ -z "{$SCRIPT_SYNC_WATCH_ONLY}" ]; then
    echo "Watch mode only; files will not be copied"
    copy_enabled=false
else
    echo "Copying enabled"
    copy_enabled=true
fi

if [ -n "${SCRIPT_SYNC_MONITOR}" ]; then
    echo "Specific monitor override selected; using monitor $SCRIPT_SYNC_MONITOR"
    monitor="--monitor=$SCRIPT_SYNC_MONITOR"
else
    echo "Using default platform monitor"
fi 

if [ -n "${SCRIPT_SYNC_NO_RECURSE}" ]; then
    echo "Top level monitoring only (will not recurse subdirectories)"
    recurse=""
else
    echo "Using default platform monitor"
    recurse="--recursive"
fi 

if [ -n "${SCRIPT_SYNC_EVENT_TYPES}" ] then
    echo "Specifying event types: $SCRIPT_SYNC_EVENT_TYPES"
    event_types="--event=$SCRIPT_SYNC_EVENT_TYPES"
else
    event_types=""
fi

echo "Starting filewatcher with "
echo fswatch -0 $src $monitor $recurse 

fswatch -0 $src $monitor $recurse | while read -d "" event \
do \
    echo ${event}
done

fswatch -0 /ltmp/funcsamples/ --recursive | while read -d "" event \
do \
    echo "RECV: ${event}"
done

# Options:
#  -0, --print0          Use the ASCII NUL character (0) as line separator.
#  -1, --one-event       Exit fswatch after the first set of events is received.
#      --allow-overflow  Allow a monitor to overflow and report it as a change event.
#      --batch-marker    Print a marker at the end of every batch.
#      --event=TYPE      Filter the event by the specified type.
#  -a, --access          Watch file accesses.
#  -d, --directories     Watch directories only.
#  -e, --exclude=REGEX   Exclude paths matching REGEX.
#  -E, --extended        Use extended regular expressions.
#      --format=FORMAT   Use the specified record format.
#  -f, --format-time     Print the event time using the specified format.
#      --fire-idle-event Fire idle events.
#  -h, --help            Show this message.
#  -i, --include=REGEX   Include paths matching REGEX.
#  -I, --insensitive     Use case insensitive regular expressions.
#  -l, --latency=DOUBLE  Set the latency.
#  -L, --follow-links    Follow symbolic links.
#  -M, --list-monitors   List the available monitors.
#  -m, --monitor=NAME    Use the specified monitor.
#      --monitor-property name=value
#                        Define the specified property.
#  -n, --numeric         Print a numeric event mask.
#  -o, --one-per-batch   Print a single message with the number of change events.
#  -r, --recursive       Recurse subdirectories.
#  -t, --timestamp       Print the event timestamp.
#  -u, --utc-time        Print the event time as UTC time.
#  -v, --verbose         Print verbose output.
#      --version         Print the version of fswatch and exit.
#  -x, --event-flags     Print the event flags.
#      --event-flag-separator=STRING
#                        Print event flags using the specified separator.

#fswatch -0 path | while read -d "" event \
  #do \
    #// do something with ${event}
#  done