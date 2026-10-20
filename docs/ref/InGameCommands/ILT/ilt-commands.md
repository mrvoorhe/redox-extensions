# Infinite Leaftide Server Commands

> Source: <https://www.leaftidewiki.com/Custom_Functionality_on_Infinite_Leaftide> (last edited 2026-07-19; retrieved 2026-10-20). The wiki requires a login.

Custom chat commands provided by the Infinite Leaftide (ILT) server. They are handled by the server, not a client plugin, so no plugin needs to be loaded. Type them in the in-game chat box.

## Notation

- `<value>`: a required value you supply.
- `[value]`: an optional value.
- `a/b/c`: choose exactly one of the listed values.
- `"<Character Name>"`: the bank transfer commands quote character names.

## Experience and Attributes

ILT has no level cap and no attribute cap. Past level 275 the Attributes panel no longer shows "XP for next level" correctly, so use `/xp` instead.

| Command | Description |
| --- | --- |
| `/xp` | Shows the XP needed for your next level. |
| `/xp <str/end/coo/qui/foc/sel/hea/sta/man> <count>` | Shows the XP cost of raising the given attribute or vital by `count` points. |
| `/xp all` | Shows the cost of the next point for every attribute and vital. |
| `/attr [<str/end/coo/qui/foc/sel/hea/sta/man> <count>]` | Raises a maxed attribute or vital (past the retail cap of 190 for attributes or 196 for vitals) by `count` points, if you have enough unassigned XP. `count` defaults to 1. |

## Enlightenment

| Command | Description |
| --- | --- |
| `/enl` | Starts enlightenment once you meet the requirements. You must be outdoors and standing still, and you are teleported to your lifestone. If the game keeps saying you are moving when you aren't, jump and try again, or relog. |

Enlightenment unequips all items and untrains all skills. It keeps attributes, Aetheria flags, and XP and Luminance augmentations. See the source page for the level, token, augmentation, society, and Luminance requirements.

## UI Layout

| Command | Description |
| --- | --- |
| `/saveui` | Saves the current UI layout to `UI-Default.txt` in your preferences folder. |
| `/loadui` | Loads the UI layout from `UI-Default.txt`. |

## Fellowships

Fellowships can have up to 29 members.

| Command | Description |
| --- | --- |
| `/fship create <Name>` | Creates a fellowship with the given name. Also available from the Fellowship panel. |
| `/fship add [Character Name]` | Invites the named player, who can be anywhere as long as they are online. Depending on that player's settings, they may be prompted to accept. With no name, invites your current target. |
| `/fship remove <Character Name>` | Removes the named player from the fellowship. |
| `/fship leave` | Leaves the fellowship. Also available from the Fellowship panel. |
| `/fship disband` | Disbands the fellowship. Also available from the Fellowship panel. |
| `/fship landblock` | Invites every player in your landblock. |
| `/t <Character Name>, xp` | Automatically adds you to the named player's fellowship, if applicable. The source page doesn't explain the mechanism; it appears to be a tell with the text `xp` to a member of that fellowship. |

## Enlightened Coins

| Command | Description |
| --- | --- |
| `/clap a` | Converts Aetheria and trinkets into Enlightened Coins and deposits them in your bank. Each Aetheria color is paired with its trinket: red Coalesced Aetheria with Ancient Empyrean Trinkets (Enlightened Coins), and blue Coalesced Aetheria with Ancient Falatacot Trinkets (Weakly Enlightened Coins, per the manual recipe). Each pair uses up whichever of the two you have fewer of. For example, 50 red Aetheria and 30 Empyrean Trinkets make 30 Enlightened Coins. Each coin costs 1 MMD from your banked pyreals. Revealed Aetheria is never consumed. Free, with no QB requirement, and works anywhere (the marketplace is no longer required). |

## Bank

Each character has its own bank. Banks are not shared across the account. A bank can hold Pyreals, Luminance, Enlightened Coins, Weakly Enlightened Coins, Legendary Keys, and Mythical Keys, with no upper limit. Luminance passed up from vassals goes straight to the bank.

### Balance and help

| Command | Description |
| --- | --- |
| `/b` | Shows your bank balances. |
| `/b help` | Lists every banking command in game. |

### Deposit

| Command | Description |
| --- | --- |
| `/b d` | Deposits all loose Pyreals, Luminance, Legendary Keys (Arwic type), and Mythical Keys (VoD T9). |
| `/b d n` | Deposits all Pyreal trade notes (e.g. MMDs). |
| `/b d e` | Deposits all Enlightened Coins. |
| `/b d we` | Deposits all Weakly Enlightened Coins. |
| `/b d ps` | Deposits all Pyreal, Gold, and Silver peas as Pyreals. |
| `/b d l <Amount>` | Deposits the given amount of Luminance. Example: `/b d l 5.5b`. |

### Withdraw

| Command | Description |
| --- | --- |
| `/b w p <Amount>` | Withdraws Pyreals into your pack. |
| `/b w k <Amount>` | Withdraws Legendary Keys. |
| `/b w mk <Amount>` | Withdraws Mythical Keys. |
| `/b w e <Amount>` | Withdraws Enlightened Coins. |
| `/b w we <Amount>` | Withdraws Weakly Enlightened Coins. |
| `/b w n <denomination> <count>` | Withdraws a number of trade notes of the given denomination. Examples: `/b w n mmd 200`, `/b w n d 5`. |

If you withdraw more than 10 Legendary or Mythical Keys, they come out as Durable Keys. For example, withdrawing 26 gives a key with 25 uses.

### Transfer to another character

| Command | Description |
| --- | --- |
| `/b t p <Amount> "<Character Name>"` | Transfers Pyreals. |
| `/b t l <Amount> "<Character Name>"` | Transfers Luminance. |
| `/b t k <Amount> "<Character Name>"` | Transfers Legendary Keys. |
| `/b t mk <Amount> "<Character Name>"` | Transfers Mythical Keys. |
| `/b t e <Amount> "<Character Name>"` | Transfers Enlightened Coins. |
| `/b t we <Amount> "<Character Name>"` | Transfers Weakly Enlightened Coins. |
| `/b t n <denomination> <count> "<Character Name>"` | Transfers trade notes rather than Pyreals. Example: `/b t n mmd 50 "MyAlt"`. |

### Amount abbreviations

Every bank command accepts these suffixes on amounts:

| Suffix | Multiplier |
| --- | --- |
| `k` | thousand (1,000) |
| `m` | million (1,000,000) |
| `b` | billion (1,000,000,000) |
| `t` | trillion (1,000,000,000,000) |
| `q` | quadrillion (1,000,000,000,000,000) |

Examples: `/b w p 250m` withdraws 250 million Pyreals, and `/b t l 1t "MyAlt"` transfers 1 trillion Luminance to MyAlt.

## Top Lists

Each command shows the top 25 characters in its category.

| Command | Description |
| --- | --- |
| `/top level` | Current character level. |
| `/top enl` | Enlightenments. |
| `/top qb` | Quest Bonus (QB). |
| `/top augs` | Number of custom Luminance augmentations. |
| `/top title` | Most titles. |
| `/top bank` | Pyreals in the bank. |
| `/top lum` | Luminance in the bank. |
| `/top attr` | Number of primary attributes. |
| `/top deaths` | Most deaths. |
| `/top Gymnos` | #1 Gymnos. |

## Source notes

The source page has some gaps and inconsistencies. This document handles them as follows:

- `/b d l <Amount>` appears only as an example (`/b d l 5.5b`), not in the main list of deposit commands. It is included above as written.
- The `d` denomination in `/b w n d 5` is never explained.
- The page tells you to "speak to Thalor Quickforge" for coin autocrafting but also says the marketplace is no longer required. It does not say whether `/clap a` needs the NPC.
- The page refers to "the `/bank transfer` command", which suggests `/bank` is a long form of `/b`, but it doesn't confirm this.
