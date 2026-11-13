Extend the existing tracking control by `/re track` to include receiving the following:

* Enlightened Coin(s)
* Strawberry Jam Jar
* Expired Trade Note
* Legendary Key
* Shattered Legendary Key
* Mythical Key
* MMD

You should track receiving from chat messages.  Look over your history or our collected data and you should find examples of all of these


In `/re report` I want you report the number of items and the hourly rate.  I also want you to use a side table to report the value of these items in Englightened Coins.  Englightened Coins themselves are easy, that's 1:1.

The other items should use the following mapping

10 Strawberry Jam Jar = 1 Enlightened Coin
1 Expired Trade Note = 1 Enlightened Coin
10 Mythical Key = 1 Enlightened Coin
20 Legendary Key = 1 Enlightened Coin
20 Shattered Legendary Key = 1 Enlightened Coin
12 MMD = 1 Enlightened Coin

## Implementation

`/re track` (and `/rf track`) resets and starts both the bank tracker and the reward tracker.  `/re report` (and
`/rf report`) writes the bank lines, then two reward tables: the counts with their hourly rate, and their value in
Enlightened Coins with a total.  Only items received at least once are listed.

Rewards are counted from `<giver> gives you [<N> ]<item>.` chat lines (`ChatParsingUtilities.TryParseGivesYou`).
Lines with quotes, `says,` or `tells you,` are ignored, so player chat can't fake a reward.  The items and their rates
are in `Listeners.Monitors/RewardCatalog.cs`:

| Item in chat | Reported as | Per Enlightened Coin |
|---|---|---|
| `Enlightened Coin(s)` | Enlightened Coin | 1 |
| `Weakly Enlightened Coin(s)` | Weakly Enlightened Coin | 10 |
| `Strawberry Jam Jar(s)` | Strawberry Jam Jar | 10 |
| `Expired Trade Note(s)` | Expired Trade Note | 1 |
| `Mythical Key(s)` | Mythical Key | 10 |
| `Aged Legendary Key(s)`, `Legendary Key(s)` | Legendary Key | 20 |
| `Durable Legendary Key(s)` (worth 10 Aged) | Durable Legendary Key | 2 |
| `Trade Note(s) (250,000)` | MMD | 12 |

Shattered Legendary Keys are not tracked.  They are a loot item and don't arrive as a `gives you` line.
