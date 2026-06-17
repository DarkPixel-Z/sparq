using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Procedural tile grid for the explore-map backdrop. Builds a flat,
    /// fully-walkable carpet of ground tiles from the Fantasy World 2D pack,
    /// optionally overlaid with a winding center path.
    ///
    /// Replaces the FantasyMaps painted backdrop, which had irregular cliffs
    /// short of the playable rect — testers walked into them reaching for
    /// loot. With a tilemap the entire MAP_W × MAP_H rect is ground, so the
    /// player clamp matches what's visible: no cliffs, no out-of-bounds loot.
    /// </summary>
    public static class TileTerrain
    {
        // UI size each source tile renders at. Source PNGs are 512×512;
        // 256 keeps them crisp without blowing up the GameObject count.
        // 1620×3800 / 256 ≈ 7 × 15 ≈ 105 tiles — well inside UGUI's batching
        // budget since they're static (no layout group, no per-frame rebuilds).
        private const float TILE_UI = 256f;

        private const string TILES_ROOT = "Assets/Fantasy World 2D/Sprites/PNG/tiles/";

        // Tile pools per biome. Six variants each + rotation by 0/90/180/270
        // gives 24 visually-distinct cells before any pattern repeats.
        private static readonly string[] GRASS = {
            "tile_grass_1/tile_grass_1_1", "tile_grass_1/tile_grass_1_2",
            "tile_grass_1/tile_grass_1_3", "tile_grass_1/tile_grass_1_4",
            "tile_grass_1/tile_grass_1_5", "tile_grass_1/tile_grass_1_6",
        };
        private static readonly string[] EARTH = {
            "tile_earth_1/tile_earth_1_1", "tile_earth_1/tile_earth_1_2",
            "tile_earth_1/tile_earth_1_3", "tile_earth_1/tile_earth_1_4",
            "tile_earth_1/tile_earth_1_5", "tile_earth_1/tile_earth_1_6",
        };
        private static readonly string[] SWAMP = {
            "tile_swamp/tile_swamp_1", "tile_swamp/tile_swamp_2",
            "tile_swamp/tile_swamp_3", "tile_swamp/tile_swamp_4",
            "tile_swamp/tile_swamp_5", "tile_swamp/tile_swamp_6",
        };
        private static readonly string[] PATH = {
            "tile_path/tile_path_1", "tile_path/tile_path_2",
            "tile_path/tile_path_3", "tile_path/tile_path_4",
        };

        /// <summary>
        /// Fill `parent`'s rect (w × h, centered on its origin) with a grid of
        /// biome-appropriate ground tiles. `tint` lets the caller match the
        /// zone's existing color treatment. Returns the carrier GameObject so
        /// the caller can re-parent or destroy it without hunting children.
        /// </summary>
        public static GameObject BuildGround(RectTransform parent, float w, float h,
                                             string biome, Color tint, int seed)
        {
            var pool = PickPool(biome);
            var carrier = MakeCarrier(parent, "TileGround", w, h);
            carrier.transform.SetAsFirstSibling();   // ground sits at the back

            int cols = Mathf.CeilToInt(w / TILE_UI);
            int rows = Mathf.CeilToInt(h / TILE_UI);
            var rng = new System.Random(seed);
            float originX = -w * 0.5f + TILE_UI * 0.5f;
            float originY = -h * 0.5f + TILE_UI * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var sp = LoadTile(pool[rng.Next(pool.Length)]);
                    if (sp == null) continue;
                    PlaceTile(carrier.transform, $"T_{r}_{c}", sp,
                              new Vector2(originX + c * TILE_UI, originY + r * TILE_UI),
                              90f * rng.Next(4), tint);
                }
            }
            return carrier;
        }

        /// <summary>
        /// Paint a winding center path on top of the ground using path tiles.
        /// Adds visual hierarchy ("walk this way") without blocking movement —
        /// players can still wander freely.
        /// </summary>
        public static GameObject BuildPath(RectTransform parent, float w, float h, int seed)
        {
            var carrier = MakeCarrier(parent, "TilePath", w, h);

            int rows = Mathf.CeilToInt(h / TILE_UI);
            var rng = new System.Random(seed ^ 0x5EED);
            float originY = -h * 0.5f + TILE_UI * 0.5f;
            float ampX = w * 0.18f;
            float freq = 6.28318f * 2.5f / Mathf.Max(1, rows);   // ~2.5 waves over the map

            for (int r = 0; r < rows; r++)
            {
                float offsetX = Mathf.Sin(r * freq) * ampX;
                for (int side = -1; side <= 0; side++)
                {
                    var sp = LoadTile(PATH[rng.Next(PATH.Length)]);
                    if (sp == null) continue;
                    PlaceTile(carrier.transform, $"P_{r}_{side}", sp,
                              new Vector2(offsetX + (side + 0.5f) * TILE_UI,
                                          originY + r * TILE_UI),
                              0f, Color.white);
                }
            }
            return carrier;
        }

        // ── internals ────────────────────────────────────────────────────────

        private static GameObject MakeCarrier(RectTransform parent, string name, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        private static void PlaceTile(Transform parent, string name, Sprite sp,
                                      Vector2 pos, float rotZ, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(TILE_UI, TILE_UI);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0, 0, rotZ);
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.color = tint;
            img.raycastTarget = false;
        }

        private static Sprite LoadTile(string tail)
            => Sparq.Core.SpriteLoader.Load(TILES_ROOT + tail + ".png");

        private static string[] PickPool(string biome)
        {
            switch ((biome ?? "forest").ToLowerInvariant())
            {
                case "haunted":  return SWAMP;   // swampy moors read as haunted
                case "rocky":    return EARTH;
                case "moonlit":  return EARTH;   // earth tinted blue reads icy/moonlit
                default:         return GRASS;   // forest + fallback
            }
        }
    }
}
