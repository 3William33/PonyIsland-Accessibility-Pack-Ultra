using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PonyIslandAccessibility
{
    [BepInPlugin(Guid, "Pony Island - Accessibility Mode", "1.0.0")]
    public class AccessibilityPlugin : BaseUnityPlugin
    {
        public const string Guid = "local.ponyisland.accessibility";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<KeyboardShortcut> ToggleKey;
        internal static ConfigEntry<bool> ShowOverlay;
        internal static ConfigEntry<bool> AssistBoss;
        internal static ConfigEntry<KeyboardShortcut> AssistToggleKey;
        internal static ManualLogSource Log;

        private static float lastLogTime = -10f;
        private static float lastBossLogTime = -10f;
        private float toastUntil;
        private GUIStyle labelStyle;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind(
                "Accessibility",
                "Enabled",
                false,
                "Master switch (default OFF). While ON, only the Jesus ticket level " +
                "(game_PART8 / screen 'Level4') grants immunity: gates/rails, butterflies, " +
                "and Jesus' stars no longer kill or reset the pony, and the player's laser " +
                "cannot damage Jesus. Everything else (scenes, spawns, progress, animations) " +
                "is untouched. This value is saved by BepInEx and survives restarts.");

            ToggleKey = Config.Bind(
                "Accessibility",
                "ToggleKey",
                new KeyboardShortcut(KeyCode.F8),
                "Hotkey that toggles Accessibility Mode in game. The new state is saved immediately.");

            ShowOverlay = Config.Bind(
                "Accessibility",
                "ShowOverlay",
                true,
                "Show a small on-screen status box while Accessibility Mode is ON.");

            AssistBoss = Config.Bind(
                "Accessibility",
                "AssistHopelessSoul",
                false,
                "Final-boss assist (default OFF). In game_FINALBOSS only: the active " +
                "Hopeless Soul phase no longer heals/resets its health when the pony is " +
                "hit, and this plugin aims the pony's laser at the boss and keeps the " +
                "fire button held. The boss still has to lose its 40 HP from the real " +
                "laser collider; nothing is written to the save. Press the assist hotkey " +
                "to toggle it live; the value is saved by BepInEx.");

            AssistToggleKey = Config.Bind(
                "Accessibility",
                "AssistToggleKey",
                new KeyboardShortcut(KeyCode.F9),
                "Hotkey that toggles the Hopeless Soul boss assist in game.");

            Harmony harmony = new Harmony(Guid + ".harmony");
            InstallPatch(harmony, "AdventureModeLevel:OnPonyCollision", "OnPonyCollision_Prefix");
            InstallPatch(harmony, "Pony:Die", "PonyDie_Prefix");
            InstallPatch(harmony, "ProjectileEnemy:OnTriggerStay2D", "ProjectileEnemyStay_Prefix");

            InstallPatch(harmony, "SoulPhase1Screen:OnPonyCollision", "BossCollision_Prefix", new Type[0]);
            InstallPatch(harmony, "SoulPhase2Screen:OnPonyCollision", "BossCollision_Prefix", new Type[0]);
            InstallPatch(harmony, "SoulPhase3Screen:OnPonyCollision", "BossCollision_Prefix", new Type[0]);
            InstallPatch(harmony, "InputManager:GetShootButton", "ShootButton_Prefix", new Type[0]);

            Log.LogInfo("Accessibility Mode loaded. Current state: " + (Enabled.Value ? "ON" : "OFF") +
                        ". Toggle key: " + ToggleKey.Value +
                        ". Boss assist: " + (AssistBoss.Value ? "ON" : "OFF") +
                        " (" + AssistToggleKey.Value + ").");
        }

        private static void InstallPatch(Harmony harmony, string target, string prefixName)
        {
            InstallPatch(harmony, target, prefixName, null);
        }

        private static void InstallPatch(Harmony harmony, string target, string prefixName, Type[] parameters)
        {
            MethodInfo targetMethod = parameters == null
                ? AccessTools.Method(target)
                : AccessTools.Method(target, parameters);
            if (targetMethod == null)
            {
                Log.LogError("Patch target not found: " + target + " (patch skipped).");
                return;
            }

            MethodInfo prefix = typeof(AccessibilityPlugin).GetMethod(
                prefixName, BindingFlags.Static | BindingFlags.NonPublic);
            harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
            Log.LogInfo("Patched " + target);
        }

        private void Update()
        {
            if (ToggleKey.Value.IsDown())
            {
                Enabled.Value = !Enabled.Value;
                Config.Save();
                toastUntil = Time.unscaledTime + 4f;
                Log.LogInfo("Accessibility Mode toggled " + (Enabled.Value ? "ON" : "OFF") + " (saved).");
            }

            if (AssistToggleKey.Value.IsDown())
            {
                AssistBoss.Value = !AssistBoss.Value;
                Config.Save();
                toastUntil = Time.unscaledTime + 4f;
                Log.LogInfo("Hopeless Soul assist toggled " + (AssistBoss.Value ? "ON" : "OFF") + " (saved).");
            }
        }

        // Boss assist: only in game_FINALBOSS and only while one of the three
        // Hopeless Soul phase screens is active. This does not write health; it
        // aims the real laser collider and lets the boss's own OnTriggerStay2D
        // deplete its 40 HP.
        private void LateUpdate()
        {
            if (!AssistBoss.Value)
            {
                return;
            }
            if (Application.loadedLevelName != "game_FINALBOSS")
            {
                return;
            }

            GameObject screen = ActiveBossScreen();
            if (screen == null)
            {
                return;
            }

            Transform boss = screen.transform.Find("HopelessSoul");
            if (boss == null)
            {
                return;
            }

            if (screen.name == "Phase1Screen")
            {
                Transform pony = screen.transform.Find("HopelessPony");
                if (pony != null)
                {
                    Transform head = pony.Find("pony_head");
                    if (head != null)
                    {
                        AimHeadAt(head, boss);
                    }
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
                if (go != null && go.activeInHierarchy)
                {
                    return go;
                }
            }
            return null;
        }

        // Same angle math as PonyHead.Update, but aimed at the boss transform.
        private static void AimHeadAt(Transform head, Transform boss)
        {
            Vector2 hp = head.position;
            Vector2 bp = boss.position;
            float angle = CustomWrapAngle(57.29578f * GetAngleOnCircle(hp, bp) - 90f);
            float rotation = CustomWrapAngle(angle + 90f);
            if (rotation < 270f && rotation > 90f)
            {
                rotation += 180f;
            }
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
            if (boss == null)
            {
                return -1f;
            }
            MonoBehaviour[] behaviours = boss.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour mb in behaviours)
            {
                if (mb == null)
                {
                    continue;
                }
                Type t = mb.GetType();
                if (t.Name.StartsWith("HopelessSoulPhase"))
                {
                    FieldInfo f = t.GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (f != null)
                    {
                        return (float)f.GetValue(mb);
                    }
                }
            }
            return -1f;
        }

        private static void LogBossHealth(string phaseName, Transform boss)
        {
            if (Time.unscaledTime - lastBossLogTime < 2f)
            {
                return;
            }
            lastBossLogTime = Time.unscaledTime;
            float hp = ReadBossHealth(boss);
            if (hp >= 0f)
            {
                Log.LogInfo("[BossAssist] " + phaseName + " boss health: " + hp.ToString("0.0") + " / 40");
            }
        }

        private static bool BossCollision_Prefix(MonoBehaviour __instance)
        {
            if (!AssistBoss.Value)
            {
                return true;
            }
            if (Application.loadedLevelName != "game_FINALBOSS")
            {
                return true;
            }

            GameObject screen = ActiveBossScreen();
            if (screen == null || __instance == null || __instance.gameObject != screen)
            {
                return true;
            }

            if (Time.unscaledTime - lastLogTime >= 1.5f)
            {
                lastLogTime = Time.unscaledTime;
                Log.LogInfo("[BossAssist] Ignored pony hit on " + screen.name + " (boss keeps its health).");
            }
            return false;
        }

        private static bool ShootButton_Prefix(ref bool __result)
        {
            if (!AssistBoss.Value)
            {
                return true;
            }
            if (Application.loadedLevelName != "game_FINALBOSS")
            {
                return true;
            }
            if (ActiveBossScreen() == null)
            {
                return true;
            }
            __result = true;
            return false;
        }

        // True only when the switch is ON and the code belongs to the Jesus ticket
        // level (scene game_PART8, screen Level4). Other levels are never altered.
        private static bool IsTargetLevel(MonoBehaviour behaviour)
        {
            if (!Enabled.Value)
            {
                return false;
            }
            if (Application.loadedLevelName != "game_PART8")
            {
                return false;
            }
            if (behaviour == null)
            {
                return false;
            }

            GameObject screen = GameObject.Find("Level4Screen");
            if (screen == null || !screen.activeInHierarchy)
            {
                return false;
            }
            return behaviour.gameObject == screen || behaviour.transform.IsChildOf(screen.transform);
        }

        private static void LogPrevented(string kind, string objectName)
        {
            if (Time.unscaledTime - lastLogTime < 1.5f)
            {
                return;
            }
            lastLogTime = Time.unscaledTime;
            Log.LogInfo("[Accessibility] Prevented " + kind + " damage from '" + objectName +
                        "' (no death, no level reset).");
        }

        // AdventureModeLevel.OnPonyCollision is what resets the level when the pony
        // touches a gate, a butterfly enemy or one of Jesus' star projectiles.
        private static bool OnPonyCollision_Prefix(MonoBehaviour __instance, GameObject other)
        {
            if (!IsTargetLevel(__instance) || other == null)
            {
                return true;
            }

            string name = other.name.ToLowerInvariant();
            bool isGate = name.Contains("gate");
            bool isEnemy = name.Contains("enemy");
            bool isProjectile = name.Contains("projectile");

            if (!isGate && !isEnemy && !isProjectile)
            {
                return true;
            }

            if (isProjectile)
            {
                // Vanilla removes the star on contact; keep that so it cannot
                // retrigger, but skip the reset/flash that follows it.
                UnityEngine.Object.Destroy(other);
            }

            LogPrevented(isGate ? "gate/rail" : (isEnemy ? "butterfly/enemy" : "star/projectile"), other.name);
            return false;
        }

        // Safety net in case a Pony on this level is configured to lose lives
        // through Pony.Die instead of the adventure-mode reset.
        private static bool PonyDie_Prefix(MonoBehaviour __instance)
        {
            if (!IsTargetLevel(__instance))
            {
                return true;
            }

            LogPrevented("fatal pony collision", __instance.gameObject.name);
            return false;
        }

        // Jesus (ActIII_ProjectileEnemy) loses health only to colliders whose name
        // contains "laser" - i.e. the player's head laser. Skipping the damage keeps
        // his health, collision and survival logic untouched while making it
        // impossible for the player to kill him.
        private static bool ProjectileEnemyStay_Prefix(MonoBehaviour __instance, Collider2D col)
        {
            if (!IsTargetLevel(__instance))
            {
                return true;
            }
            if (col == null || !col.name.Contains("laser"))
            {
                return true;
            }

            if (Time.unscaledTime - lastLogTime >= 1.5f)
            {
                lastLogTime = Time.unscaledTime;
                Log.LogInfo("[Accessibility] Blocked player laser damage to Jesus (health untouched).");
            }
            return false;
        }

        private void OnGUI()
        {
            if (!Enabled.Value && Time.unscaledTime >= toastUntil)
            {
                return;
            }
            if (!ShowOverlay.Value && Time.unscaledTime >= toastUntil)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 14;
                labelStyle.normal.textColor = Color.white;
                labelStyle.padding = new RectOffset(8, 8, 5, 5);
            }

            string text = "Accessibility Mode: " + (Enabled.Value ? "ON" : "OFF") + "  [F8]" +
                          "   Boss assist: " + (AssistBoss.Value ? "ON" : "OFF") + "  [F9]";
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(new Rect(8f, 8f, 470f, 30f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(16f, 12f, 458f, 24f), text, labelStyle);
        }
    }
}
