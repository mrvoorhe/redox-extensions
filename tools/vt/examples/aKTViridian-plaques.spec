# aKTViridian: from where the entrance portal drops you in zone 1, use the three kill task plaques.
# Points are VRMetaZone1.nav's nodes 0, 136 and 0-5, which pass 3 m (Brellegia), 4.5 m (Aangesilus) and 10 m
# (Swift) from the markers.  Brellegia's Marker is class Foci, so the uses are UB commands, not talk nodes.
type once
navpt 41.9319 -43.1630 0.4667
navpt 41.9347 -43.1664 0.4667
navpt 41.9674 -43.1956 0.4667
pause 1000
chat /ub usel Brellegia's Marker
pause 3000
navpt 41.9319 -43.1630 0.4667
navpt 41.9105 -43.1399 0.4667
navpt 41.8995 -43.1019 0.4667
navpt 41.8970 -43.0849 0.4667
pause 1000
chat /ub usel Aangesilus' Marker
pause 3000
navpt 41.9153 -43.0533 0.4667
navpt 41.9402 -43.0126 0.4729
pause 1000
chat /ub usel Swift's Marker
pause 3000
