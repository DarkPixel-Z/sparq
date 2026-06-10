using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace Sparq.Editor
{
    /// <summary>
    /// Wires up Feel (More Mountains) juice effects onto Karu's tap.
    /// Uses reflection so this compiles even if Feel namespaces shift between versions.
    /// </summary>
    public static class SparqFeelSetup
    {
        [MenuItem("Sparq/9. Wire Feel Juice to Karu")]
        public static void WireFeelToKaru()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null)
            {
                EditorUtility.DisplayDialog("Sparq Feel", "No Karu in current scene.", "OK");
                return;
            }

            // Locate MMF_Player type
            Type mmfPlayerType = FindType("MoreMountains.Feedbacks.MMF_Player")
                               ?? FindType("MMF_Player");
            if (mmfPlayerType == null)
            {
                EditorUtility.DisplayDialog("Sparq Feel",
                    "MMF_Player not found — is Feel imported?\nCheck Assets/Feel/ exists.", "OK");
                return;
            }

            // Add MMF_Player to Karu if missing
            var existing = karu.GetComponent(mmfPlayerType) as Component;
            if (existing == null)
            {
                existing = karu.AddComponent(mmfPlayerType);
            }

            // Feel's feedback types
            Type scaleType    = FindType("MoreMountains.Feedbacks.MMF_Scale");
            Type positionType = FindType("MoreMountains.Feedbacks.MMF_Position");
            Type flickerType  = FindType("MoreMountains.Feedbacks.MMF_Flicker");
            Type cameraType   = FindType("MoreMountains.Feedbacks.MMF_CameraShake");

            // Get the FeedbacksList field
            var feedbacksListField = mmfPlayerType.GetField("FeedbacksList",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var feedbacksList = feedbacksListField?.GetValue(existing) as System.Collections.IList;
            if (feedbacksList == null)
            {
                EditorUtility.DisplayDialog("Sparq Feel",
                    "Couldn't access MMF_Player.FeedbacksList.\nOpen Karu in Inspector, click 'Add new feedback', and add:\n• Scale\n• Position (shake)\n• Flicker\nThen save.", "OK");
                return;
            }

            // Clear any existing auto-feedbacks we may have added before
            feedbacksList.Clear();

            // Helper to construct a feedback and add it
            void AddFeedback(Type t)
            {
                if (t == null) return;
                var fb = Activator.CreateInstance(t);
                feedbacksList.Add(fb);
            }

            AddFeedback(scaleType);
            AddFeedback(positionType);
            AddFeedback(flickerType);
            AddFeedback(cameraType);

            // Mark dirty so Unity saves
            EditorUtility.SetDirty(karu);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq Feel] Added MMF_Player with 4 default feedbacks to Karu.");
            EditorUtility.DisplayDialog("Sparq Feel",
                "✅ MMF_Player added to Karu with:\n• Scale punch\n• Position shake\n• Flicker\n• Camera shake\n\n" +
                "Select Karu in Hierarchy → Inspector shows MMF Player.\n" +
                "Click the ▶ Test button next to each feedback to preview.\n\n" +
                "Tap Karu in Play mode → PetDisplay auto-triggers all of them.", "OK");
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            // Fallback — search by simple name
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                .FirstOrDefault(t => t.Name == fullName.Split('.').Last());
        }

        // ──────────────────────────────────────────────────────────────────────
        // Auto-set all Feel feedback targets on Karu
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem("Sparq/10. Auto-Set Feel Targets on Karu")]
        public static void AutoSetFeelTargets()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null) { EditorUtility.DisplayDialog("Sparq Feel", "No Karu in scene.", "OK"); return; }

            var allMb = karu.GetComponents<MonoBehaviour>();
            MonoBehaviour mmfPlayer = null;
            foreach (var mb in allMb)
            {
                if (mb != null && mb.GetType().Name == "MMF_Player") { mmfPlayer = mb; break; }
            }

            if (mmfPlayer == null)
            {
                EditorUtility.DisplayDialog("Sparq Feel", "No MMF_Player on Karu yet. Run step 9 first or add one manually.", "OK");
                return;
            }

            var playerType = mmfPlayer.GetType();
            // Try common field names where MMF_Player stores its feedbacks
            string[] candidateFields = { "FeedbacksList", "Feedbacks", "_feedbacks", "feedbacksList" };
            System.Collections.IList list = null;
            foreach (var name in candidateFields)
            {
                var field = playerType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    list = field.GetValue(mmfPlayer) as System.Collections.IList;
                    if (list != null) break;
                }
            }

            if (list == null)
            {
                // Try property instead of field
                var prop = playerType.GetProperty("FeedbacksList",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) list = prop.GetValue(mmfPlayer) as System.Collections.IList;
            }

            if (list == null || list.Count == 0)
            {
                EditorUtility.DisplayDialog("Sparq Feel",
                    "Couldn't find feedbacks on MMF_Player.\n\nManual: select Karu, in MMF Player Inspector, drag Karu GameObject onto each feedback's 'Target' or 'Animate ___ Target' field.", "OK");
                return;
            }

            int wired = 0;
            // Target field name patterns across different feedbacks
            string[] targetFieldNames = {
                "AnimateScaleTarget", "AnimatePositionTarget", "AnimateRotationTarget",
                "BoundRenderer", "TargetRenderer", "TargetSpriteRenderer",
                "TargetTransform", "Target", "TargetGameObject", "TargetObject",
                "AnimateTarget", "AnimateColorTarget"
            };

            foreach (var feedback in list)
            {
                if (feedback == null) continue;
                var fbType = feedback.GetType();

                foreach (var fname in targetFieldNames)
                {
                    var field = fbType.GetField(fname,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null) continue;

                    // Skip if already assigned
                    var currentVal = field.GetValue(feedback);
                    if (currentVal != null &&
                        !(currentVal is UnityEngine.Object uo && uo == null))
                        continue;

                    // Pick target based on field's expected type
                    var ft = field.FieldType;
                    object target = null;
                    if (ft == typeof(Transform))          target = karu.transform;
                    else if (ft == typeof(GameObject))    target = karu;
                    else if (ft == typeof(SpriteRenderer))target = karu.GetComponent<SpriteRenderer>();
                    else if (ft == typeof(Renderer))      target = karu.GetComponent<SpriteRenderer>();
                    else if (typeof(Component).IsAssignableFrom(ft))
                    {
                        // Try getting a component of matching type from Karu
                        target = karu.GetComponent(ft);
                    }

                    if (target != null)
                    {
                        field.SetValue(feedback, target);
                        wired++;
                    }
                }
            }

            EditorUtility.SetDirty(mmfPlayer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq Feel] Wired {wired} feedback target fields to Karu.");
            EditorUtility.DisplayDialog("Sparq Feel",
                $"✅ Auto-wired {wired} feedback targets to Karu.\n\nHit ▶ Play → click Karu → watch for effects.", "OK");
        }
    }
}
