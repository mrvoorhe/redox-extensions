# Rynthid kill tasks: from anywhere to the Rynthid Area NPCs, talk to each and use the contracts they hand out.
# Recorded on Woodford 2026-11-05 (vtrecord.py).  Also used for turn-in: the NPCs reward finished tasks, and a
# /ub usei for a contract that isn't in the pack does nothing.
type once
chat /ub usei Halls of Introduction Gem
pause 5000
pt 012F0126 30.10 -60.13 6.01    # arrived
pt 012F0120 22.46 -58.85 6.01
pt 012F011F 22.87 -51.94 6.01
pt 012F0125 27.06 -45.71 6.01
pt 012F010F 29.35 -39.49 5.18
pt 012F010E 29.56 -29.99 0.47
pt 012F010E 26.07 -26.92 0.00
pt 012F0107 17.25 -29.93 0.00
pt 012F0104 7.34 -30.87 0.00
pt 012F0104 6.56 -30.93 0.00
pt 012F0102 4.93 -30.62 0.00    # before Daily Portals
portal -101.14303370813529 -64.47406624952951 -0.00026249587535858153 Daily Portals
pause 5000
pt 64440144 89.82 -301.71 -24.00    # arrived
pt 64440162 102.18 -300.15 -24.00
pt 64440173 111.87 -300.25 -24.00
pt 6444017F 121.17 -300.34 -24.00
pt 64440182 124.67 -300.37 -24.00
pt 6444018B 131.16 -300.43 -24.00
pt 64440197 140.27 -300.51 -24.00
pt 64440199 140.14 -295.04 -24.00
portal -21.366662470499673 -48.75813751220703 -0.10026249885559083 Rynthid Area
pause 5000
pt 3032001C 83.04 89.41 132.00    # arrived
pt 3032001C 78.89 84.48 132.00
pt 30320014 68.01 78.54 132.00
pt 30320014 66.68 76.88 132.00
# Hernd: Rynthid Rare Boss (contract)
talk -63.2734658241272 -61.63984540303548 0.5500207901000976 Hernd
pause 3000
chat /ub usei Contract for Kill: Rynthid Rare Boss
pause 1000
pt 30320014 65.12 76.24 132.00
pt 30320014 63.99 75.30 132.00
# Lohrn: 6 Rynthid Crystals (no contract)
talk -63.2904891649882 -61.643733755747476 0.5501207987467448 Lohrn
pause 2000
pt 30320014 60.70 75.51 132.00
# Wido: 15 Rynthid Sorcerers (contract)
talk -63.30097999572754 -61.64142166773478 0.5501207987467448 Wido
pause 3000
chat /ub usei Contract for Kill: Rynthid Sorcerers
pause 1000
pt 30320014 56.47 76.24 132.00
# Rhys: 10 Rynthid Rifts (contract)
talk -63.321983337402344 -61.63779083887736 0.5501207987467448 Rhys
pause 3000
chat /ub usei Contract for Kill: Rynthid Rifts
pause 1000
pt 30320014 58.36 87.82 132.00
pt 30320014 57.85 90.62 132.00
# Ranulf: 15 Rynthid Ragers (contract?  not seen yet; the use does nothing if there isn't one)
talk -63.31530833244324 -61.55817041397095 0.5501207987467448 Ranulf
pause 3000
chat /ub usei Contract for Kill: Rynthid Ragers
pause 1000
pt 30320014 57.50 92.06 132.00
pt 30320014 59.31 92.91 132.00
# Sylvanus: 15 Rynthid Slayers (contract)
talk -63.30078333218892 -61.558232084910074 0.5501207987467448 Sylvanus
pause 3000
chat /ub usei Contract for Kill: Rynthid Slayers
pause 1000
pt 30320014 62.80 93.81 132.00
# Marcus: 25 Rynthid Minions (contract)
talk -63.285528326034544 -61.55595458348592 0.5501207987467448 Marcus
pause 3000
chat /ub usei Contract for Kill: Rynthid Minions
pause 1000
pt 30320014 65.70 92.90 132.00
# Royal Guard: 25 Rynthid Cows
talk -63.27632417678833 -61.558100414276126 0.5500207901000976 Royal Guard
pause 2000
# Tillahan: 3 Empowered Wisps (contract)
talk -63.26781123479207 -61.567797088623045 0.5500249862670898 Tillahan
pause 3000
chat /ub usei Contract for Kill: Empowered Wisps
pause 1000
pt 30320014 68.21 89.20 132.00
pt 3032001C 74.63 88.04 132.00
pt 3032001C 79.92 86.87 132.00    # end, start of aKTRynthid-gotoloop
