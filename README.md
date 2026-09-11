# Service Stats HUD

Standalone PlateUp mod that adds a configurable text HUD with per-player daily service stats. Player names use their chosen profile colors and sort alphabetically. The player list supports larger lobbies, including MMO Kitchen.

Download the compiled mod from [GitHub Releases](https://github.com/cschubiner/PlateUp-Mod-ServiceStatsHUD/releases).

Steam Workshop item: [Service Stats HUD](https://steamcommunity.com/sharedfiles/filedetails/?id=3799437092). The initial upload is awaiting Steam's automated content review and is not publicly visible yet. Use the GitHub release for installation meanwhile.

## What It Tracks

- `srv`: customer order items served at the delivery moment
- `ord`: orders taken manually or by order machine
- `wash`: dishes washed and floor messes cleaned by completed player cleaning processes
- `act`: generic player-attributed actions, including serving, ordering, washing, chopping-style interactions, dispenser/provider use, and compatible transfer/combine interactions
- `dist`: player movement distance during the day
- `idle`: idle time after the configured threshold, defaulting to 1 second without an attributed action
- `asleep`: idle time after the configured threshold, defaulting to 5 seconds without an attributed action

Players are hidden until they have served at least one dish by default. Other totals are still tracked while hidden, so they appear once that player earns their first serve.

## Preferences Menu

Open `Preferences` from the main menu or pause menu, then open `Service Stats HUD`.

- Toggle the HUD on or off.
- Hide or show served, orders, washed, actions, distance, and idle/asleep timers.
- Configure idle and asleep thresholds from `1s` through `20s`.
- Keep zero-serve players hidden or show everyone.
- Adjust text size from `30%` through `130%`.
- Choose a loaded TMP font. `Alt Font 1` is the default, and `Default Font` remains selectable.
- Move the HUD down with `Current Y` or `10% Down` through `90% Down`.

## HUD Behavior

- Screen-space Canvas overlay.
- Plain right-aligned TextMeshPro text, like debug text.
- Default position: upper right with a small safe margin.
- The size selector changes font size directly.
- The y-offset selector moves the text block down from the top of the reference screen.
- Label format uses the resolved profile/session name when available, otherwise `P#`.
- Stats remain visible through the next preparation phase and reset when you start gameplay for the next day. Returning to HQ may clear them.
- Washed and actions are hidden by default; enable them in Preferences.

## Attribution Rules

- `GroupPromptForOrder.Perform(...)`: increments orders and actions only for confirmed order-taking.
- `UseOrderMachine.Perform(...)`: increments orders and actions only for valid order-machine use.
- Meal serves increment from confirmed order acceptances, so standing near customers does not count.
- Drink serves increment from confirmed drink transfers into a customer table/grab point, not later passive customer consumption.
- Completed player cleaning processes always increment actions.
- Completed clean-appliance processes and floor/mess cleaning processes also increment washed.
- Generic player interaction/transfer hooks increment actions for supported interaction types, including provider/dispenser results such as coffee-machine use.
- Automation-only outcomes without a player actor do not earn per-player credit.

## Editing The Mod

Source lives at the root of this repository.

- Entry point: `Mod.cs`
- Runtime stats state: `Helpers/ServiceStatsRuntime.cs`
- Pure HUD/stat logic: `Helpers/ServiceStatsHudLogic.cs`
- Settings menu: `Helpers/ServiceStatsSettings.cs`
- Harmony patches: `Patches/ServiceStatsInteractionPatches.cs`
- ECS systems: `Systems/`
- HUD renderer: `Visuals/ServiceStatsHudManager.cs`
- Tests: `ServiceStatsHUD.Tests/ServiceStatsHudLogicTests.cs`

Do not edit generated files under `bin`, `obj`, `TestResults`, or built DLL/PDB files under `workshop\content`.

## Where To Put It

For local PlateUp play, the mod folder should be:

```text
C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\Mods\ServiceStatsHUD
```

The compiled DLL should be inside:

```text
C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\Mods\ServiceStatsHUD\content\ServiceStatsHUD.dll
```

For a downloaded release, extract the ZIP into PlateUp's `Mods` folder. It contains `ServiceStatsHUD/content/ServiceStatsHUD.dll` and metadata. Keep only one installed copy of Service Stats HUD, including Workshop subscriptions, to avoid loading it twice.

When building from source, the build script installs the DLL automatically. `Sync-WorkshopToMods.ps1` can also copy the repository's `workshop` folder into `Mods/ServiceStatsHUD`.

## Local Stack Verified In This Workspace

- PlateUp install: `C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp`
- HarmonyX Workshop dependency: `2898033283`
- KitchenLib Workshop dependency: `2898069883`
- PreferenceSystem Workshop dependency: `2949018507`
- ModUploader path: `PlateUp_Data\ModUploader.exe`

Subscribe to [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2898033283), [KitchenLib](https://steamcommunity.com/sharedfiles/filedetails/?id=2898069883), and [PreferenceSystem](https://steamcommunity.com/sharedfiles/filedetails/?id=2949018507). These dependencies and the game's assemblies are not bundled with this mod.

## Build And Test

Run these from the repository root. Building requires Windows, .NET Framework 4.7.2 targeting tools, MSBuild/Visual Studio Build Tools, and a local PlateUp installation with the dependencies above. Copy `Local.props.example` to `Local.props` to override installation paths if needed.

1. Check setup:
   - `powershell.exe -ExecutionPolicy Bypass -File .\Check-PlateUpSetup.ps1`
2. Run pure logic tests:
   - `powershell.exe -ExecutionPolicy Bypass -File .\Run-ServiceStatsHUDTests.ps1`
3. Build the mod:
   - `powershell.exe -ExecutionPolicy Bypass -File .\Build-ServiceStatsHUD.ps1`
4. Sync workshop content into the local PlateUp mods folder:
   - `powershell.exe -ExecutionPolicy Bypass -File .\Sync-WorkshopToMods.ps1`

`Build-ServiceStatsHUD.ps1 -Configuration Release` compiles the Release DLL and copies it to both `workshop/content` and the local PlateUp mod folder. Close PlateUp before rebuilding. Do not distribute the whole `bin` folder: it contains game/dependency assemblies.

## Steam Workshop Publishing

Build Release, then run `Open-ModUploader.ps1` to open PlateUp's bundled uploader. Select this repository's `workshop` folder. Publish only this mod's DLL and metadata as content, add a real in-game screenshot as the preview, and list Harmony, KitchenLib, and PreferenceSystem as required items. Preserve the resulting Workshop item ID for subsequent updates instead of creating duplicate listings.

The existing item ID is saved in `workshop/plateup_mod_metadata.json`; use the uploader's **Update** tab. `Publish-Workshop.ps1 -DependenciesOnly` configures the three required items. After Steam's content review clears, `Publish-Workshop.ps1 -PreviewPath <screenshot-path>` updates the preview and requests public visibility. It uses the Steamworks library bundled with your installed game and requires Steam to be running and signed in as the item owner. It never creates a second listing.

## In-Game Smoke Checklist

- Manual order-taking increments the correct player.
- Order-machine use increments the correct player.
- Serves credit only the serving player.
- Washing dishes or cleaning floor messes increments both `wash` and `act` for the cleaning player.
- Chopping and combining count as actions where the underlying interaction is supported.
- Taking or creating items from provider-style machines, such as coffee machines, counts as an action.
- Distance increases while players move during daytime.
- Idle starts increasing after the configured threshold, defaulting to 1 second without an attributed action.
- Asleep starts increasing after the configured threshold, defaulting to 5 seconds without an attributed action.
- Players with zero serves stay hidden unless `Show Everyone` is selected.
- Once a player serves, their earlier orders, washes, actions, distance, idle, and asleep totals appear.
- Previous-day stats remain through preparation and clear when the next gameplay phase starts; HQ transitions may clear them.
