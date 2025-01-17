#!/bin/bash

interface=$1
duration=$2

sum="$(tshark \
    -i $interface \
    -a duration:$duration \
    -T fields \
    -e frame.len \
    -Y 'mptcp and not tcp.len==0' | \
    awk '{s+=$1} END {print s}'\
    )"

res=$(bc <<< "scale=2; ($sum * 8)/($duration * 1000000)")
echo "$interface: $res Mbps"