# PonyIsland-Accessibility-Pack-Ultra

**A game-wide survival assist mod for Pony Island**

[简体中文](README.md) | **English**

A small mod I made for *Pony Island* to make repeated deaths from jumping obstacles, enemy attacks, and certain fights a little less frustrating.

## Why I Made This

I really like *Pony Island* for its story, puzzles, and all its wonderfully weird ideas. But some levels were driving me up the wall. Oh my God, that Jesus guy in particular was getting on my nerves. (Seriously.)

Some sections make you dodge the same obstacles over and over. I just wanted to see more of the story, uncover secrets, and collect tickets—not die dozens of times in the same spot.

So I made this mod.

I originally made it for the **Act III ticket challenge where you have to reach the end without harming Jesus**. Dodging everything while trying not to hit him was driving me crazy. It eventually grew into a game-wide survival assist.

The goal is simple: **I want to enjoy the game without getting stuck on the same action sequence forever.**

## Features

| Key | Feature | Default |
|---|---|---|
| **F8** | Game-wide survival assist (especially for the Jesus ticket challenge) | OFF |
| **F9** | Hidden encounter assist (after all 24 tickets) | OFF |

### F8 — Game-Wide Survival Assist

When enabled, the mod attempts to prevent ordinary hazard collisions, enemy attacks, and other damage events from killing the player or restarting a level.

You can still move, jump, shoot, collect items, and solve puzzles yourself. It doesn't autoplay levels or skip them for you.

**Why I made F8:** In Act III, one ticket requires you to reach the end of the Jesus level **without harming Jesus**. F8 can help you survive the hazards along the way, but **it does not complete the ticket requirement for you**. You still have to play the level and avoid hitting him yourself.

F8 isn't limited to a particular butterfly, gate, or chapter anymore.

**Important: Some story sequences require you to die or fail normally. If you run into one, turn off F8 yourself, complete the sequence, and turn it back on afterward.**

### F9 — Hidden Encounter Assist (Minor Unlock Hint)

Collecting **all 24 tickets** unlocks an optional hidden boss encounter. F9 provides assistance specifically for that fight; **it is not an assist for the regular story's final boss**. I won't spoil the opponent's identity or what happens in that encounter.

It helps with collision handling, aiming, and attacking, while the opponent still loses health through the game's own damage mechanics.

F8 and F9 can be toggled independently.

Both start **OFF every time you launch the game**, so you only activate them when needed.

## Installation

For the Windows Steam version of *Pony Island*.

1. Download and extract the complete project.
2. Make sure *Pony Island* is fully closed.
3. Double-click `install.bat`.
4. Follow the prompts to locate your game installation.
5. Launch the game normally through Steam.
6. Press **F8** or **F9** in-game when you want to use the assist.

The package includes the plugin DLL and necessary runtime components. You don't need to compile the source code or run anything as administrator.

Keep the directory structure intact; downloading `install.bat` alone is not enough.

## Disabling and Uninstalling

**Temporarily disable the mod:**

Close the game completely, then run `patch-off.bat`. To enable it again, run `patch-on.bat`.

**Uninstall the mod:**

Close the game completely, then run `uninstall.bat`.

If you have other BepInEx plugins installed, be careful not to manually delete files belonging to those plugins.

### What if the game stops working after uninstalling?

Try Steam's built-in file verification:

**Steam → Library → Pony Island → Properties → Installed Files → Verify integrity of game files**

Steam will check the official game files and redownload any that are missing or damaged.

**Note:** Steam verification does not necessarily remove extra files installed by third-party mods. If the issue persists, check for remaining BepInEx components or other plugins in the game directory.

## Testing and Notes

This isn't just a proof of concept sitting in a source file. I've tested the mod in my own Steam game environment.

That said, *Pony Island* is full of unusual events and unconventional game mechanics. I can't promise perfect compatibility with every level and every story branch.

A few things to keep in mind:

- If the story requires a death, turn F8 off manually.
- If dialogue, ticket collection, or story interactions stop working, try disabling the assist and playing that section normally.
- Don't install, uninstall, or replace plugins while the game is running.
- This mod does not automatically skip levels, edit save files, or directly unlock Steam achievements.

For detailed historical test notes, see `docs/TEST-REPORT.md`. Earlier test results aren't proof that every scenario in the current version has been covered.

## Source Code

The main C# source file is:

`src/AccessibilityPlugin.cs`

The mod uses BepInEx and Harmony for runtime patching without directly modifying the original game assemblies.

Build scripts are included for anyone who wants to study or compile the plugin themselves.

## About This Project

This is an unofficial, non-commercial mod I made to improve my own experience with the game.

It is not affiliated with Daniel Mullins Games, the developer of *Pony Island*, or the game's publisher.

The game and its assets belong to their respective rights holders. This repository does not include the full game, pirated copies, or cracks.

If this little tool also helps someone else who wants to enjoy the story but struggles with the action sections, that's even better.

---

**All I wanted was to enjoy a game, not get bullied by a little pony over and over again.**

*(Furiously bites the pony.)*

Enjoy the game! 🐴
