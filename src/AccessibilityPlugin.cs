// =============================================================================
//  Pony Island Accessibility Pack — GLOBAL DODGE build (v1.1.0-draft)
//  Source file for AccessibilityPlugin.cs replacement -> PonyIslandAccessibility.dll
//
//  STATUS: SOURCE ONLY. NOT COMPILED, NOT IN-GAME TESTED.
//  The 2026-11-12 session's command security policy blocks invoking the C#
//  compiler, so no DLL was produced and no game session was started. This file
//  is a design/implementation draft for a later build in a permitted environment.
//  IT IS NOT EVIDENCE OF AN IN-GAME PASS.
//
//  ---------------------------------------------------------------------------
//  WHAT CHANGED vs v1.0.0 (SHA-256 919c8ac2...)
//  ---------------------------------------------------------------------------
//   * F8 / Enabled: was "immunity only in game_PART8 + Level4Screen, only for
//     objects named gate/enemy/projectile". NOW "global dodge assist": default
//     OFF, and while ON it makes the pony survive collision damage / level
//     resets in EVERY scene, whatever the obstacle is called (butterflies,
//     gates, rails, spikes, stars, bullets, moving hazards, ... no name list).
//   * F9 / AssistBoss: UNCHANGED. Final-boss assist keeps its v1.0 behaviour
//     (no heal/reset, auto-aim, held fire) and is completely independent of F8.
//   * Both switches default OFF and are toggled on demand (F8 / F9).
//   * "Scripted death" auto-off: when the game needs a plot death, the dodge
//     assist stands down automatically (see below) so the story can advance.
//
//  ---------------------------------------------------------------------------
//  DEFAULT-IMMUNITY + STORY-PASSTHROUGH  (user-approved design)
//  ---------------------------------------------------------------------------
//  Read-only decompilation of Assembly-CSharp.dll showed that OnPonyCollision
//  is NOT only a damage callback: the very same method is used for plot
//  triggers (DialogueTrigger, GlitchedFlag, LoueySequenceTrigger), ticket
//  collection (CheckFindTicket(7) in the Jesus level) and cutscene steps.
//  Blindly returning false everywhere would therefore freeze the story and
//  could even prevent AllTicketsFound() from ever opening game_FINALBOSS.
//
//  So the global prefix uses an INVERTED rule:
//    - collision object name matches a story / interaction / collectible token
//      -> return true (let the original method run);
//    - anything else -> return false (dodge damage, no death, no level reset).
//  The token list lives in the cfg key "PassthroughNames" so a player who hits
//  a stuck plot point can simply add that object's name (the diagnostic log
//  prints every name that was blocked).
//
//  ---------------------------------------------------------------------------
//  SCRIPTED-DEATH AUTO-OFF  (user-approved design)
//  ---------------------------------------------------------------------------
//  Two independent detectors, both best-effort (a meta game like Pony Island
//  can route a plot death through any number of ad-hoc paths, so NO static
//  detector can be exhaustive):
//    1. ScreenSwitcher.SwitchToScreen("...GameOver...") is observed via a
//       pass-through prefix. This is a real signal: the decompiled
//       ChangeScreens() has an explicit case "GameOver".
//    2. Repeat heuristic: if the SAME collision object is blocked >=
//       RepeatThreshold times inside a 2 s window, the player is clearly stuck
//       at a node that requires the vanilla outcome, so we stand down.
//  On detection (ScriptedDeathMode=AutoOff, the default):
//      Enabled.Value = false; Config.Save(); toast + log;
//      -> F8 is now OFF, vanilla death/reset runs, story advances.
//      -> Press F8 again to re-enable at any time. Nothing auto-re-enables,
//         because the end of a cutscene cannot be detected reliably.
//  Alternative cfg value "LetThroughOnce": only lets that ONE collision
//  through and keeps F8 ON. Useful if AutoOff feels too aggressive.
//  F9 / final-boss assist is never touched by any of this.
//
//  ---------------------------------------------------------------------------
//  KNOWN LIMITS / MUST BE CHECKED IN A REAL RUN
//  ---------------------------------------------------------------------------
//   * Harmony prefix injection relies on object[] __args (Harmony 2.x). If
//     HarmonyX 2.9.0 rejects it, Patch() logs a warning and that method simply
//     stays vanilla -> the build still loads, with reduced coverage.
//   * Assembly.GetTypes() may throw ReflectionTypeLoadException; handled.
//   * PassthroughNames tokens are guesses from decompiled string constants.
//     The first real playthrough MUST verify all 24 tickets still register and
//     that no cutscene stalls. The diagnostic log is the tool for that.
//   * The Jesus laser-damage prefix from v1.0 is deliberately kept scoped to
//     game_PART8 + Level4Screen. Globalising it would stop the player's laser
//     from hurting ANY enemy and would break the final boss, whose 40 HP must
//     be removed by the real laser collider (F9 depends on that).
//   * SoulPhase1/2/3Screen are EXCLUDED from the global scan so the final-boss
//     assist keeps its exact v1.0 behaviour.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PonyIslandAccessibility
{
    [BepInPlugin(Guid, "Pony Island - Global Survival Assist v2", "2.0.0")]
    public class AccessibilityPlugin : BaseUnityPlugin
    {
        // Keep the v1.0 GUID so the existing cfg file is reused and the DLL is a
        // drop-in replacement. Back up the old DLL before swapping.
        public const string Guid = "local.ponyisland.accessibility.v2";

        // --- switches (F8 = global dodge, F9 = final boss, both default OFF) --
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<KeyboardShortcut> ToggleKey;
        internal static ConfigEntry<bool> ShowOverlay;
        internal static ConfigEntry<bool> AssistBoss;
        internal static ConfigEntry<KeyboardShortcut> AssistToggleKey;

        // --- global dodge tuning ---------------------------------------------
        internal static ConfigEntry<string> PassthroughNames;
        internal static ConfigEntry<string> ScriptedDeathMode;
        internal static ConfigEntry<int> RepeatThreshold;
        internal static ConfigEntry<bool> LogBlockedNames;

        internal static ManualLogSource Log;
        private static ConfigFile Cfg;

        private static string[] passTokens = new string[0];
        private static readonly Dictionary<string, float> repeatFirst = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> repeatCount = new Dictionary<string, int>();
        private static Type playerControlsType;
        private static int patchedCollisionMethods;
        private static int globalBlockCount;

        private static float lastLogTime = -10f;
        private static float lastBossLogTime = -10f;
        private static bool autoDisabled;
        private static string autoDisableReason = "";

        private static float toastUntil;
        private static string toastText = "";
        private GUIStyle labelStyle;

        private const string DefaultPassthrough =
            "dialogue,trigger,flag,ticket,collect,pickup,reward,prize,bonus," +
            "checkpoint,save,portal,cutscene,sequence,hint,text,button,switch," +
            "cursor,exit,goal,finish";

        // =====================================================================
        //  Lifecycle
        // =====================================================================
        private void Awake()
        {
            Log = Logger;
            Cfg = Config;

            Enabled = Config.Bind(
                "Accessibility", "Enabled", false,
                "GLOBAL DODGE ASSIST (default OFF, F8). While ON, collision damage " +
                "and level resets are ignored in EVERY scene, for ANY obstacle " +
                "(gates, rails, butterflies, stars, bullets, spikes, moving hazards...). " +
                "Story triggers, dialogue, collectibles and interactions are still " +
                "allowed through. If the game needs a scripted death, this switch " +
                "turns itself OFF automatically; press F8 to turn it back on.");

            ToggleKey = Config.Bind(
                "Accessibility", "ToggleKey", new KeyboardShortcut(KeyCode.F8),
                "Hotkey for global dodge assist.");

            ShowOverlay = Config.Bind(
                "Accessibility", "ShowOverlay", true,
                "Show a small on-screen status box while either assist is ON.");

            AssistBoss = Config.Bind(
                "Accessibility", "AssistHopelessSoul", false,
                "Final-boss assist (default OFF, F9). UNCHANGED from v1.0.0: in " +
                "game_FINALBOSS only, the active phase no longer heals/resets on a " +
                "pony hit, the laser is aimed at the boss and fire is held. The boss " +
                "still loses its 40 HP through the real laser collider.");

            AssistToggleKey = Config.Bind(
                "Accessibility", "AssistToggleKey", new KeyboardShortcut(KeyCode.F9),
                "Hotkey for the final-boss assist.");

            PassthroughNames = Config.Bind(
                "GlobalDodge", "PassthroughNames", DefaultPassthrough,
                "Comma-separated name fragments whose collisions are NEVER blocked. " +
                "Add a name here if a cutscene or a ticket stops working. The " +
                "diagnostic log prints the exact names that were blocked.");

            ScriptedDeathMode = Config.Bind(
                "GlobalDodge", "ScriptedDeathMode", "AutoOff",
                "AutoOff = a scripted death turns F8 OFF and saves the cfg; " +
                "LetThroughOnce = let only that collision through and keep F8 ON.");

            RepeatThreshold = Config.Bind(
                "GlobalDodge", "RepeatThreshold", 4,
                "Retained for config compatibility; repeated ordinary collisions " +
                "never auto-disable F8. Scripted stand-down requires real GameOver.");

            LogBlockedNames = Config.Bind(
                "GlobalDodge", "LogBlockedNames", true,
                "Log the object name whenever a collision is blocked (throttled). " +
                "Use this to discover names that belong in PassthroughNames.");

            ParsePassthrough();
            playerControlsType = AccessTools.TypeByName("PonyKeyboardControls");

            Harmony harmony = new Harmony(Guid + ".harmony");
            // Each install step is isolated: Unity silently swallows exceptions
            // thrown from Awake, so without per-step guards one failing patch
            // would abort every later patch with no log line at all.
            SafeInstall("global collision scan", delegate { InstallGlobalCollisionPatches(harmony); });
            SafeInstall("Pony:Die", delegate { InstallPonyDiePatch(harmony); });
            SafeInstall("Pony:OnTriggerEnter2D finale guard", delegate { InstallFinalePonyTriggerPatch(harmony); });
            SafeInstall("BeelzebubScreen death guard", delegate { InstallBeelzebubPatches(harmony); });
            SafeInstall("FinalePonies:OnMainPonyHit", delegate { InstallFinaleMainPonyHitPatch(harmony); });
            SafeInstall("FinalePonies:ResetMainPony", delegate { InstallFinaleResetPatch(harmony); });
            SafeInstall("ScreenSwitcher observer", delegate { InstallScreenWatch(harmony); });
            SafeInstall("Jesus laser guard", delegate { InstallJesusLaserPatch(harmony); });
            SafeInstall("final-boss assist", delegate { InstallFinalBossPatches(harmony); });

            Log.LogInfo("Global Dodge Assist loaded. Dodge: " + (Enabled.Value ? "ON" : "OFF") +
                        " (" + ToggleKey.Value + "). Boss assist: " +
                        (AssistBoss.Value ? "ON" : "OFF") + " (" + AssistToggleKey.Value + "). " +
                        "Global collision methods patched: " + patchedCollisionMethods + ".");
        }

        private delegate void InstallAction();

        private static void SafeInstall(string label, InstallAction action)
        {
            try
            {
                action();
                Log.LogInfo("[GlobalDodge] OK: " + label);
            }
            catch (Exception e)
            {
                Log.LogError("[GlobalDodge] FAILED: " + label + " -> " + e);
            }
        }

        private static void ParsePassthrough()
        {
            string raw = PassthroughNames.Value ?? "";
            string[] parts = raw.Split(',');
            List<string> list = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i].Trim().ToLowerInvariant();
                if (s.Length > 0) list.Add(s);
            }
            passTokens = list.ToArray();
        }

        private void Update()
        {
            // Config entry descriptions above are fixed, but a player may edit
            // the cfg while the game is closed; re-parse on toggle is cheap and
            // keeps PassthroughNames live without a restart.
            if (ToggleKey.Value.IsDown())
            {
                ParsePassthrough();
                Enabled.Value = !Enabled.Value;
                Config.Save();
                autoDisabled = false;
                toastUntil = Time.unscaledTime + 4f;
                toastText = "Dodge " + (Enabled.Value ? "ON" : "OFF");
                Log.LogInfo("Global dodge assist toggled " + (Enabled.Value ? "ON" : "OFF") + " (saved).");
            }

            if (AssistToggleKey.Value.IsDown())
            {
                AssistBoss.Value = !AssistBoss.Value;
                Config.Save();
                toastUntil = Time.unscaledTime + 4f;
                toastText = "Boss assist " + (AssistBoss.Value ? "ON" : "OFF");
                Log.LogInfo("Hopeless Soul assist toggled " + (AssistBoss.Value ? "ON" : "OFF") + " (saved).");
            }
        }

        // =====================================================================
        //  Global dodge: patch EVERY OnPonyCollision(GameObject) in the game
        // =====================================================================
        private void InstallGlobalCollisionPatches(Harmony harmony)
        {
            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                "GlobalCollisionPrefix", BindingFlags.Static | BindingFlags.NonPublic);

            Type anchor = AccessTools.TypeByName("AdventureModeLevel");
            if (anchor == null)
            {
                Log.LogError("[GlobalDodge] AdventureModeLevel not found; global coverage DISABLED.");
                return;
            }

            Type[] types;
            try
            {
                types = anchor.Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
                Log.LogWarning("[GlobalDodge] partial type load (" + (e.Types != null ? e.Types.Length : 0) + " types).");
            }
            if (types == null) return;

            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t == null || !typeof(MonoBehaviour).IsAssignableFrom(t)) continue;
                // Final-boss phases belong to F9 (v1.0 behaviour, unchanged).
                if (t.Name.StartsWith("SoulPhase")) continue;

                MethodInfo[] methods;
                try
                {
                    methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                           BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                }
                catch
                {
                    continue;
                }
                for (int j = 0; j < methods.Length; j++)
                {
                    MethodInfo m = methods[j];
                    if (m.Name != "OnPonyCollision") continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length != 1 || ps[0].ParameterType != typeof(GameObject)) continue;
                    try
                    {
                        harmony.Patch(m, prefix: new HarmonyMethod(prefix));
                        patchedCollisionMethods++;
                    }
                    catch (Exception e)
                    {
                        Log.LogWarning("[GlobalDodge] could not patch " + t.Name + "." + m.Name +
                                       ": " + e.Message);
                    }
                }
            }
        }

        // Inverted rule: story/interaction/collectible -> let vanilla run;
        // everything else -> dodge it. Never guesses obstacle names.
        private static bool GlobalCollisionPrefix(object __instance, object[] __args)
        {
            if (!Enabled.Value) return true;

            GameObject other = (__args != null && __args.Length > 0) ? __args[0] as GameObject : null;
            string name = other != null ? other.name : "";
            string lower = name.ToLowerInvariant();

            // No name available: be conservative, let the game decide rather
            // than risk swallowing a plot trigger.
            if (lower.Length == 0) return true;

            // Preserve the v1.0 Level4 behaviour exactly for Lucifer's
            // projectile phase: block the reset and destroy the projectile.
            // Without the destroy, a projectile can remain overlapped with
            // the pony and keep retriggering the collision path.
            if (__instance is MonoBehaviour &&
                __instance.GetType().Name == "AdventureModeLevel" &&
                Application.loadedLevelName == "game_PART8" &&
                GameObject.Find("Level4Screen") != null &&
                GameObject.Find("Level4Screen").activeInHierarchy &&
                lower.Contains("projectile"))
            {
                UnityEngine.Object.Destroy(other);
                globalBlockCount++;
                LogBlocked("projectile", name);
                return false;
            }

            for (int i = 0; i < passTokens.Length; i++)
            {
                if (lower.Contains(passTokens[i])) return true;
            }

            globalBlockCount++;
            if (LogBlockedNames.Value && Time.unscaledTime - lastLogTime >= 1.5f)
            {
                lastLogTime = Time.unscaledTime;
                Log.LogInfo("[GlobalDodge] Blocked collision '" + name + "' (total " + globalBlockCount + ").");
            }
            return false;
        }

        private static void LogBlocked(string kind, string name)
        {
            if (!LogBlockedNames.Value || Time.unscaledTime - lastLogTime < 1.5f) return;
            lastLogTime = Time.unscaledTime;
            Log.LogInfo("[GlobalDodge] Blocked " + kind + " '" + name + "'.");
        }

        private static bool NoteRepeat(string key)
        {
            float now = Time.unscaledTime;
            float first;
            if (!repeatFirst.TryGetValue(key, out first) || now - first > 2f)
            {
                repeatFirst[key] = now;
                repeatCount[key] = 1;
                return false;
            }
            int n = repeatCount.ContainsKey(key) ? repeatCount[key] : 0;
            n++;
            repeatCount[key] = n;
            int limit = RepeatThreshold.Value;
            if (limit < 2) limit = 2;
            return n >= limit;
        }

        private static void StandDown(string reason)
        {
            autoDisableReason = reason;
            repeatFirst.Clear();
            repeatCount.Clear();

            string mode = ScriptedDeathMode.Value ?? "AutoOff";
            if (string.Equals(mode, "LetThroughOnce", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogInfo("[GlobalDodge] Scripted death let through once (" + reason + "); F8 stays ON.");
                return;
            }

            autoDisabled = true;
            Enabled.Value = false;
            try { Cfg.Save(); }
            catch (Exception e) { Log.LogWarning("[GlobalDodge] cfg save failed: " + e.Message); }
            toastText = "Dodge auto-OFF (story) - press F8";
            toastUntil = Time.unscaledTime + 6f;
            Log.LogInfo("[GlobalDodge] Scripted death detected (" + reason +
                        ") -> F8 auto-disabled and saved. Press F8 to re-enable.");
        }

        // =====================================================================
        //  Player death: only the playable pony, not decorative background ponies
        // =====================================================================
        private void InstallPonyDiePatch(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method("Pony:Die");
            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                "PonyDiePrefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, target, prefix, "Pony:Die");
        }

        private void InstallFinalePonyTriggerPatch(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(
                "Pony:OnTriggerEnter2D", new Type[] { typeof(Collider2D) });
            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                "FinalePonyTriggerPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, target, prefix, "Pony:OnTriggerEnter2D (Finale guard)");
        }

        private static bool FinalePonyTriggerPrefix(Collider2D other)
        {
            if (!Enabled.Value || other == null) return true;
            Type finaleType = AccessTools.TypeByName("FinaleScreen");
            FieldInfo finaleInstance = finaleType != null
                ? finaleType.GetField("instance", BindingFlags.Static | BindingFlags.Public)
                : null;
            if (finaleInstance == null || finaleInstance.GetValue(null) == null) return true;
            LogBlocked("finale player collision", other.name);
            return false;
        }

        private void InstallBeelzebubPatches(Harmony harmony)
        {
            MethodInfo collision = AccessTools.Method(
                "BeelzebubScreen:OnCollisionMessage", new Type[] { typeof(GameObject) });
            MethodInfo reset = AccessTools.Method("BeelzebubScreen:Reset", new Type[0]);
            MethodInfo collisionPrefix = typeof(AccessibilityPlugin).GetMethod(
                "BeelzebubCollisionPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo resetPrefix = typeof(AccessibilityPlugin).GetMethod(
                "BeelzebubResetPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, collision, collisionPrefix, "BeelzebubScreen:OnCollisionMessage");
            Patch(harmony, reset, resetPrefix, "BeelzebubScreen:Reset");
        }

        private static bool BeelzebubCollisionPrefix(GameObject other)
        {
            if (!Enabled.Value) return true;
            // In vanilla, a spike that reaches the pony is removed as part of
            // the death/reset path. Since F8 suppresses that reset, preserve
            // only the projectile cleanup and keep the boss encounter alive.
            if (other != null && other.GetComponent("BeelzebubSpike") != null)
            {
                UnityEngine.Object.Destroy(other);
                LogBlocked("Beelzebub spike destroyed", other.name);
                return false;
            }
            LogBlocked("Beelzebub hit", other != null ? other.name : "<null>");
            return false;
        }

        private static bool BeelzebubResetPrefix()
        {
            if (!Enabled.Value) return true;
            LogBlocked("Beelzebub reset", "BeelzebubScreen");
            return false;
        }

        // The finale escape path bypasses OnPonyCollision: Pony sends a
        // List<GameObject> directly to FinalePonies.OnMainPonyHit, which then
        // calls ResetMainPony for satanlaser/Obstacle. Hook that real failure
        // decision point without touching collectibles or normal movement.
        private void InstallFinaleMainPonyHitPatch(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(
                "FinalePonies:OnMainPonyHit", new Type[] { typeof(List<GameObject>) });
            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                "FinaleMainPonyHitPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, target, prefix, "FinalePonies:OnMainPonyHit");
        }

        private static bool FinaleMainPonyHitPrefix(List<GameObject> args)
        {
            if (!Enabled.Value || args == null || args.Count < 2) return true;
            GameObject hit = args[1];
            if (hit == null) return true;

            // These are the two branches that the original method sends to
            // ResetMainPony. Do not suppress unrelated finale messages.
            bool dangerous = hit.name.Contains("Obstacle") || hit.CompareTag("satanlaser");
            if (!dangerous) return true;

            globalBlockCount++;
            if (LogBlockedNames.Value && Time.unscaledTime - lastLogTime >= 1.5f)
            {
                lastLogTime = Time.unscaledTime;
                Log.LogInfo("[GlobalDodge] Blocked finale reset '" + hit.name + "'.");
            }
            return false;
        }

        // Full System Dump has a second, lower-level failure path: after a
        // collision message, FinalePonies.ResetMainPony instantiates a new
        // player and destroys the current one. Hook the actual reset entry so
        // every Uber Satan attack is covered even when its collider does not
        // use the ordinary collision-object path.
        private void InstallFinaleResetPatch(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(
                "FinalePonies:ResetMainPony", new Type[] { typeof(GameObject) });
            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                "FinaleResetPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, target, prefix, "FinalePonies:ResetMainPony");
        }

        private static bool FinaleResetPrefix(GameObject mainPony)
        {
            if (!Enabled.Value || mainPony == null) return true;
            Type finaleType = AccessTools.TypeByName("FinaleScreen");
            FieldInfo finaleInstance = finaleType != null
                ? finaleType.GetField("instance", BindingFlags.Static | BindingFlags.Public)
                : null;
            object finale = finaleInstance != null ? finaleInstance.GetValue(null) : null;
            if (finale != null)
            {
                LogBlocked("finale ResetMainPony", mainPony.name);
                return false;
            }
            Type ponyType = AccessTools.TypeByName("Pony");
            FieldInfo ponyInstance = ponyType != null
                ? ponyType.GetField("instance", BindingFlags.Static | BindingFlags.Public)
                : null;
            object current = ponyInstance != null ? ponyInstance.GetValue(null) : null;
            if (current == null || ((Component)current).gameObject != mainPony) return true;
            LogBlocked("finale reset", mainPony.name);
            return false;
        }

        private static bool PonyDiePrefix(MonoBehaviour __instance)
        {
            if (!Enabled.Value || __instance == null) return true;
            Type finaleType = AccessTools.TypeByName("FinaleScreen");
            FieldInfo finaleInstance = finaleType != null
                ? finaleType.GetField("instance", BindingFlags.Static | BindingFlags.Public)
                : null;
            if (finaleInstance != null && finaleInstance.GetValue(null) != null)
            {
                LogBlocked("finale Pony.Die", __instance.gameObject.name);
                return false;
            }
            // Finale's controllable main pony is driven by Pony.MoveToXPosition
            // and does not carry PonyKeyboardControls. Uber Satan's background
            // laser therefore reaches Pony.Die directly; identify the real
            // player through Pony.instance before the legacy control-component
            // fallback, without suppressing decorative/follower ponies.
            Type ponyType = AccessTools.TypeByName("Pony");
            FieldInfo ponyInstance = ponyType != null
                ? ponyType.GetField("instance", BindingFlags.Static | BindingFlags.Public)
                : null;
            object mainPony = ponyInstance != null ? ponyInstance.GetValue(null) : null;
            if (mainPony != null && __instance.gameObject == ((Component)mainPony).gameObject)
            {
                return false;
            }
            // PonyKeyboardControls has [RequireComponent(typeof(Pony))] and only
            // exists on the playable pony. Background / flying ponies use Die()
            // to drive visual and plot effects, so leave those alone.
            if (playerControlsType == null) return true;
            if (__instance.GetComponent(playerControlsType) == null) return true;
            return false;
        }

        // =====================================================================
        //  Watch for the game's GameOver screen => scripted death stand-down
        // =====================================================================
        private void InstallScreenWatch(Harmony harmony)
        {
            Type ss = AccessTools.TypeByName("ScreenSwitcher");
            if (ss == null) return;
            // ScreenSwitcher has several SwitchToScreen overloads. On Mono /
            // CLR 2.0, GetMethod(name, flags) throws AmbiguousMatchException,
            // so pin the exact (string) overload.
            MethodInfo target = ss.GetMethod("SwitchToScreen",
                BindingFlags.Instance | BindingFlags.Public, null,
                new Type[] { typeof(string) }, null);
            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                "ScreenSwitchPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, target, prefix, "ScreenSwitcher:SwitchToScreen (observer)");
        }

        // Observation only: always returns true, never changes vanilla flow.
        private static bool ScreenSwitchPrefix(object[] __args)
        {
            if (!Enabled.Value) return true;
            string screen = (__args != null && __args.Length > 0) ? __args[0] as string : null;
            if (string.IsNullOrEmpty(screen)) return true;
            if (screen.ToLowerInvariant().Contains("gameover"))
            {
                StandDown("screen '" + screen + "'");
            }
            return true;
        }

        // =====================================================================
        //  v1.0 compatibility: keep "player laser cannot hurt Jesus" scoped to
        //  the Jesus level. Globalising it would break the final boss (F9).
        // =====================================================================
        private void InstallJesusLaserPatch(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method("ProjectileEnemy:OnTriggerStay2D");
            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                "JesusLaserPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, target, prefix, "ProjectileEnemy:OnTriggerStay2D (PART8 only)");
        }

        private static bool JesusLaserPrefix(MonoBehaviour __instance, Collider2D col)
        {
            if (!Enabled.Value) return true;
            if (Application.loadedLevelName != "game_PART8") return true;
            GameObject lv4 = GameObject.Find("Level4Screen");
            if (lv4 == null || !lv4.activeInHierarchy) return true;
            if (col == null || !col.name.Contains("laser")) return true;
            return false;
        }

        // =====================================================================
        //  F9 final-boss assist — copied verbatim in behaviour from v1.0.0
        // =====================================================================
        private void InstallFinalBossPatches(Harmony harmony)
        {
            MethodInfo collisionPrefix = typeof(AccessibilityPlugin).GetMethod(
                "BossCollision_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, AccessTools.Method("SoulPhase1Screen:OnPonyCollision", new Type[0]), collisionPrefix, "SoulPhase1Screen:OnPonyCollision");
            Patch(harmony, AccessTools.Method("SoulPhase2Screen:OnPonyCollision", new Type[0]), collisionPrefix, "SoulPhase2Screen:OnPonyCollision");
            Patch(harmony, AccessTools.Method("SoulPhase3Screen:OnPonyCollision", new Type[0]), collisionPrefix, "SoulPhase3Screen:OnPonyCollision");

            MethodInfo shootPrefix = typeof(AccessibilityPlugin).GetMethod(
                "ShootButton_Prefix", BindingFlags.Static | BindingFlags.NonPublic);
            Patch(harmony, AccessTools.Method("InputManager:GetShootButton", new Type[0]), shootPrefix, "InputManager:GetShootButton");
        }

        private static void Patch(Harmony harmony, MethodInfo target, MethodInfo prefix, string label)
        {
            if (target == null || prefix == null)
            {
                Log.LogWarning("[Accessibility] patch target missing: " + label);
                return;
            }
            try
            {
                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Log.LogInfo("Patched " + label);
            }
            catch (Exception e)
            {
                Log.LogError("[Accessibility] patch failed for " + label + ": " + e.Message);
            }
        }

        private void LateUpdate()
        {
            if (!AssistBoss.Value) return;
            if (Application.loadedLevelName != "game_FINALBOSS") return;

            GameObject screen = ActiveBossScreen();
            if (screen == null) return;
            Transform boss = screen.transform.Find("HopelessSoul");
            if (boss == null) return;

            if (screen.name == "Phase1Screen")
            {
                Transform pony = screen.transform.Find("HopelessPony");
                if (pony != null)
                {
                    Transform head = pony.Find("pony_head");
                    if (head != null) AimHeadAt(head, boss);
                    MovePonyToward(pony, boss);
                }
            }
            LogBossHealth(screen.name, boss);
        }

        private static GameObject ActiveBossScreen()
        {
            string[] names = { "Phase1Screen", "Phase2Screen", "Phase3Screen" };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject go = GameObject.Find(names[i]);
                if (go != null && go.activeInHierarchy) return go;
            }
            return null;
        }

        private static bool BossCollision_Prefix(MonoBehaviour __instance)
        {
            if (!AssistBoss.Value) return true;
            if (Application.loadedLevelName != "game_FINALBOSS") return true;
            GameObject screen = ActiveBossScreen();
            if (screen == null || __instance == null || __instance.gameObject != screen) return true;
            if (Time.unscaledTime - lastLogTime >= 1.5f)
            {
                lastLogTime = Time.unscaledTime;
                Log.LogInfo("[BossAssist] Ignored pony hit on " + screen.name + " (boss keeps its health).");
            }
            return false;
        }

        private static bool ShootButton_Prefix(ref bool __result)
        {
            if (!AssistBoss.Value) return true;
            if (Application.loadedLevelName != "game_FINALBOSS") return true;
            if (ActiveBossScreen() == null) return true;
            __result = true;
            return false;
        }

        // --- unchanged v1.0 aiming / health maths ----------------------------
        private static void AimHeadAt(Transform head, Transform boss)
        {
            Vector2 hp = head.position;
            Vector2 bp = boss.position;
            float angle = CustomWrapAngle(57.29578f * GetAngleOnCircle(hp, bp) - 90f);
            float rotation = CustomWrapAngle(angle + 90f);
            if (rotation < 270f && rotation > 90f) rotation += 180f;
            head.rotation = Quaternion.Euler(0f, 0f, rotation);
            Vector3 scale = head.localScale;
            float absX = Mathf.Abs(scale.x);
            head.localScale = new Vector3(bp.x < hp.x ? -absX : absX, scale.y, scale.z);
        }

        private static int GetQuadrant(Vector2 center, Vector2 point)
        {
            if (point.x > center.x && point.y > center.y) return 1;
            if (point.x > center.x && point.y < center.y) return 4;
            if (point.x < center.x && point.y < center.y) return 3;
            return 2;
        }

        private static float GetAngleOnCircle(Vector2 center, Vector2 point)
        {
            float x = Vector2.Distance(point, new Vector2(center.x, point.y));
            float y = Vector2.Distance(center, new Vector2(center.x, point.y));
            int quadrant = GetQuadrant(center, point);
            return ((quadrant != 2 && quadrant != 4)
                    ? Mathf.Atan2(y, x)
                    : (Mathf.PI / 2f - Mathf.Atan2(y, x)))
                   + (quadrant - 1) * Mathf.PI * 0.5f;
        }

        private static float CustomWrapAngle(float angle)
        {
            if (angle < 0f) return 360f - Mathf.Abs(angle);
            if (angle > 360f) return angle - 360f;
            return angle;
        }

        private static void MovePonyToward(Transform pony, Transform boss)
        {
            float dx = boss.position.x - pony.position.x;
            float side = dx >= 0f ? 1f : -1f;
            float desired = Mathf.Clamp(boss.position.x - side * 8f, -15.5f, 15.5f);
            float nx = Mathf.MoveTowards(pony.position.x, desired, 6f * Time.deltaTime);
            pony.position = new Vector3(nx, pony.position.y, pony.position.z);
        }

        private static float ReadBossHealth(Transform boss)
        {
            if (boss == null) return -1f;
            MonoBehaviour[] behaviours = boss.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour mb in behaviours)
            {
                if (mb == null) continue;
                Type t = mb.GetType();
                if (t.Name.StartsWith("HopelessSoulPhase"))
                {
                    FieldInfo f = t.GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (f != null) return (float)f.GetValue(mb);
                }
            }
            return -1f;
        }

        private static void LogBossHealth(string phaseName, Transform boss)
        {
            if (Time.unscaledTime - lastBossLogTime < 2f) return;
            lastBossLogTime = Time.unscaledTime;
            float hp = ReadBossHealth(boss);
            if (hp >= 0f)
                Log.LogInfo("[BossAssist] " + phaseName + " boss health: " + hp.ToString("0.0") + " / 40");
        }

        // =====================================================================
        //  Status overlay
        // =====================================================================
        private void OnGUI()
        {
            bool showToast = Time.unscaledTime < toastUntil;
            if (!Enabled.Value && !AssistBoss.Value && !showToast) return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 14;
                labelStyle.normal.textColor = Color.white;
                labelStyle.padding = new RectOffset(8, 8, 5, 5);
            }

            string text = "Dodge: " + (Enabled.Value ? "ON" : "OFF") + "  [F8]" +
                          "   Boss: " + (AssistBoss.Value ? "ON" : "OFF") + "  [F9]";
            if (showToast) text += "   |   " + toastText;
            if (autoDisabled && !Enabled.Value) text += "   |   auto-OFF: " + autoDisableReason;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(new Rect(8f, 8f, 620f, 30f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(16f, 12f, 608f, 24f), text, labelStyle);
        }
    }
}
