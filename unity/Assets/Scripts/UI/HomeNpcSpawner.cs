using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Spawns 3 wandering NPCs on the home screen using monster-pack sprites.
    /// Each NPC patrols + chats. Idempotent — won't spawn twice.
    /// </summary>
    public static class HomeNpcSpawner
    {
        private const string MP = "Assets/2D Fantasy Monster Sprite Pack/Monsters/";
        // NOTE: Sweet-Droplet removed from this pool because it's the player's
        // pet (Drip) sprite — having it appear as an ambient NPC creates a
        // confusing duplicate "second Drip" on the home screen.
        private static readonly string[] FRIENDLY_NPCS = {
            MP + "Chick/Fluffy-Chick.png",
            MP + "Chick/Orange-Chick.png",
            MP + "Beetle/Bush-Beetle.png",
            MP + "Cloud/Happy-Cloud.png",
            MP + "Hen/Furious-Hen.png",
        };

        public static void Ensure()
        {
            // Skip if already spawned
            if (GameObject.Find("HomeNpc_0") != null) return;

            // Find the home canvas to parent under
            Canvas canvas = null;
            foreach (var c in Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.name == "UI Canvas" || c.name.ToLower().Contains("ui"))
                { canvas = c; break; }
            }
            if (canvas == null) return;

            // Squirrel chat lines (used by the twitchy NPC)
            string[] squirrelChat = {
                "SQUIRREL!  SQUIRREL!",
                "Did you SEE that?!",
                "ACORNS, where?!",
                "Ohh, shiny!",
                "Was that a leaf?? AHH!",
                "Heyy!  HEY!  HEY!",
                "Is that food? What about NOW?",
                "Squrr- squrr- squirrel!",
            };

            // Dragon chat lines (used by the majestic NPC)
            string[] dragonChat = {
                "Greetings, mortal.",
                "The ancient runes whisper...",
                "Your destiny calls.",
                "Make today legendary.",
                "I see great things ahead.",
                "Wisdom favors the brave.",
                "A new dawn rises.",
                "The flames know your name.",
            };

            // Pool of dragon sprites — pick one randomly
            string[] dragonPool = {
                MP + "Dragon/Green-Dragon.png",
                MP + "Dragon/Red-Dragon.png",
                MP + "Dragon/Purple-Dragon.png",
                MP + "Dragon/Eclipse-Dragon.png",
            };

            // Spawn 2 NPCs: [0]=Squirrel, [1]=random friendly
            for (int i = 0; i < 2; i++)
            {
                bool isSquirrel = (i == 0);
                bool isDragon   = false;

                #if UNITY_EDITOR
                string path =
                    isSquirrel ? MP + "Beetle/Lucky-Beetle.png"
                  : isDragon   ? dragonPool[Random.Range(0, dragonPool.Length)]
                               : FRIENDLY_NPCS[Random.Range(0, FRIENDLY_NPCS.Length)];
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp == null) continue;
                #else
                Sprite sp = null;
                #endif

                var go = new GameObject($"HomeNpc_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0);
                // Dragon spawns center-back near the doorway light;
                // ground NPCs (squirrel + friendly) spawn along the lower band.
                if (isDragon)
                {
                    rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    rt.anchoredPosition = new Vector2(0, 460);
                }
                else
                {
                    // Spread the two ground NPCs across the bottom — one left, one right
                    int slot = isSquirrel ? 0 : 1;
                    float[] xs = { 220, 760 };
                    rt.anchoredPosition = new Vector2(xs[slot], 110 + Random.Range(-15, 15));
                }
                int sz = isDragon ? 170 : (isSquirrel ? 100 : 110);
                rt.sizeDelta = new Vector2(sz, sz);
                var img = go.GetComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.raycastTarget = false;

                var npc = go.AddComponent<HomeNpc>();
                if (isSquirrel)
                {
                    npc.twitchy = true;
                    npc.walkSpeed = 90f;       // zips around fast
                    npc.bounceAmplitude = 7f;
                    npc.bounceSpeed = 9f;
                    npc.patrolMin = new Vector2(80, 80);
                    npc.patrolMax = new Vector2(1000, 200);
                    npc.customChat = squirrelChat;
                }
                else if (!isDragon)
                {
                    // Friendly NPC — make sure it actually walks across the screen
                    npc.walkSpeed = 45f;
                    npc.patrolMin = new Vector2(80, 80);
                    npc.patrolMax = new Vector2(1000, 200);
                }
                else if (isDragon)
                {
                    npc.majestic = true;
                    npc.walkSpeed = 22f;
                    npc.bounceAmplitude = 12f;
                    npc.bounceSpeed = 1.5f;
                    // Hovers around the doorway light at the back of the scene
                    npc.patrolMin = new Vector2(-180, 420);
                    npc.patrolMax = new Vector2(180, 540);
                    npc.customChat = dragonChat;
                }
                npc.Init(rt, img);
            }
        }
    }
}
