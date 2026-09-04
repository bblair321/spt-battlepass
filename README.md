# SPT Battle Pass

Combination mod for **SPT 4.1.x**: daily / weekly / monthly challenges → paper tickets → shop exchange, shown on a Character / inventory tab (not a trader).

## Install

Requires **SPT 4.1.x** (built against 4.1.4).

1. Download `SPT-BattlePass-0.2.0.zip` from [Releases](https://github.com/bblair321/spt-battlepass/releases).
2. Extract into your SPT folder so you get:
   - `SPT_Runtime/user/mods/com.bblai.battlepass/`
   - `BepInEx/plugins/com.bblai.battlepass/`
3. Restart **SPT.Server** and the game.

Fika: put the **server** mod on the host and the **client** plugin on every player (including headless).

## Loop

- 3 dailies, 3 weeklies, 3 monthlies rolled per period (UTC day / ISO week / calendar month).
- Completing challenges also fills a **10-level track**. Free rewards always pay; premium rewards unlock after a **750,000 ₽** stash purchase (once per month).
- Tickets live on the profile, not in stash.
- Buy sends items to **Messages → SYSTEM**. Collect them into stash from there.
- Unspent tickets at month rollover convert to a consolation crate, also mailed to SYSTEM.
- Challenges never appear in vanilla Tasks.

## Layout

```
src/SptBattlePass.Server   SPT user/mods DLL + db JSON
src/SptBattlePass.Client   BepInEx plugin
```

## Requirements

- SPT 4.1.4 (or another 4.1.x)
- .NET 10 SDK
- Game install at `D:\SPT` (override with `/p:SptInstallPath=...`)

## Build

```powershell
dotnet build SptBattlePass.sln -c Release
```

Release builds copy:

- Server → `D:\SPT\SPT_Runtime\user\mods\com.bblai.battlepass\`
- Client → `D:\SPT\BepInEx\plugins\com.bblai.battlepass\`

Restart **both** the SPT server and the game after a build. Copy fails if `SPT.Server` is still running.

## In game

- Open **Character** (Gear / Health / Skills / Tasks). **BATTLE PASS** is the extra tab on the right, with a punched-ticket icon (not the Weekend Drops crate). Ticket count is in the panel header.
- **Challenges**, **Track**, **Exchange**, **Season**, and **Settings** are the five views. Esc, Close, or a click on the dim backdrop dismisses the panel. Each view keeps its own scroll position. Exchange search and category chips stay pinned above the list. Challenges lists in-progress first and dims completed cards. Exchange is a two-column grid.
- **Track** is a free/premium reward ladder. Pass XP is separate from PMC XP. Buying premium mid-month mails premium rewards for levels you already reached. Item rewards go to **Messages → SYSTEM**. Reopen Character if stash roubles look stale after the purchase.
- **Exchange** has a search box, category chips (including **KEYS**), and **CAN AFFORD**. CLEAR resets the filters. Ammo matches the shop guns (9x18 / 9x19 / 12/70 / 7.62x39 / 7.62x54R / 5.45 / 5.56). Marked keys are not sold.
- **Season** tracks this month’s completions, tickets earned/spent, leftover-crate mail, and lifetime totals. The header bar is monthly-challenge progress (`1 / 3`).
- After a raid, a left-side card on the results screen lists what moved and tickets earned.
- In raid, a top-right widget shows live progress. **F8** toggles it (rebind `InRaidWidgetKey` in BepInEx config). Finishing a challenge pops a toast. **PIN** a challenge on the Challenges tab to keep it at the top of the widget even on the wrong map. Turn the widget, toasts, raid-results card, and UI sounds on or off under **Settings**.

## Catalog

Edit JSON under `db/` (copied next to the server DLL):

- `daily.json` / `weekly.json` / `monthly.json` — challenge pools
- `shop.json` / `crate.json` — exchange and season-end crate. Set `"preset": true` on a weapon tpl to mail the **default assembled build**, not a stripped receiver.
- `track.json` — 10-level free/premium rewards. `"xp"` is the pass XP needed for that level. A reward is `"tickets": N` and/or an item `{ "name", "tpl", "count", "preset?" }`.
- `config.json` — `debugGrants`, `grantAmount`, reroll costs/limits, challenges per set, monthly bonus, XP per completed daily/weekly/monthly (`xpDaily` / `xpWeekly` / `xpMonthly`) plus `xpMonthlyBonus` when every monthly is done. Pass-track XP is `trackXpDaily` / `trackXpWeekly` / `trackXpMonthly` / `trackXpMonthlyBonus`. Premium costs `premiumCostRoubles` (stash roubles). Set any XP field to `0` to disable it.

Completing a challenge grants **PMC XP** immediately at raid end (not mail). In-raid toasts preview that XP; the raid-results card shows the confirmed amount. Vanilla Tarkov UI sounds play on panel open/close, buys, rerolls, toasts, and raid feedback.

Daily reroll costs **1 ticket** (3 max per day). Weekly reroll costs **2** (1 max per week). You can't reroll a set after any challenge in that set is complete. When `debugGrants` is true, the panel shows **GRANT 10** and **UNLOCK (DEBUG)** for premium. Leave it `false` unless you are testing.

## Notes

- Ticket edits in `user/profileData/<id>/com.bblai.battlepass.json` need a **server restart** (profile data is cached). Prefer the GRANT button.
- New challenge types appear on the next daily / weekly / monthly reset, not mid-period. Weapon-class challenges (`KillWeapon`, `KillScavsWeapon`, `KillPmcsWeapon`, `HeadshotWeapon`) use Tarkov `WeapClass` values like `pistol`, `smg`, `shotgun`, `assaultRifle`, `assaultCarbine`, `sniperRifle`.
- `KillMelee` / `KillGrenade` count lethal melee and grenade/explosion kills.
- `SurviveNight` / `SurviveDay` use the raid's time of day. Any challenge can also set `"timeOfDay": "night"` or `"day"`.
- `FindInRaid` counts FIR items of `"tpl"` you extract with (you keep them). `HandOver` is a **TURN IN** on the challenge card and removes items from stash.

## Fika / coop

The pass is per player, not per squad. Your kills and extracts go to **your** tickets.

- Install the **server** mod on the SPT host (the machine running `SPT.Server`).
- Install the **client** plugin on every player, including a Fika headless client.
- Headless does not show the tab or report raids for the dedicated profile.
- Teammate kills do not count as PMC / scav challenge kills.
- Settings → **COOP** shows whether this instance is solo, Fika host, Fika client, or headless.

Joiners talk to the host SPT over the same `/client/battlepass/*` routes as solo. If a teammate is missing the client plugin, they will not see the tab and their raids will not count.
