// Place this file anywhere under an Editor/ folder or leave it here — it uses EditorWindow.
// After Unity compiles, go to Tools > Shadow > Generate Particle Textures.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class ShadowTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Shadow/Generate Particle Textures")]
    static void Generate()
    {
        string dir = "Assets/VFX/EyesOfShadow/Textures";
        Directory.CreateDirectory(dir);

        SavePNG(BuildInflowTexture(),   dir + "/tex_ShadowInflow.png");
        SavePNG(BuildSparkTexture(),    dir + "/tex_RedWoundSpark.png");
        SavePNG(BuildEmanationTexture(),dir + "/tex_DarkEmanation.png");

        AssetDatabase.Refresh();
        Debug.Log("[Shadow] Particle textures generated in " + dir);
    }

    // ─────────────────────────────────────────────────────────────
    // SYSTEM 1 — Shadow Inflow
    // Elongated symmetric streak: bright tight core, fades at both ends.
    // Narrow along X so particles look like thin threads being consumed.
    // ─────────────────────────────────────────────────────────────
    static Texture2D BuildInflowTexture()
    {
        int w = 64, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (float)(w - 1)) - 0.5f; // -0.5 .. 0.5
                float ny = (y / (float)(h - 1)) - 0.5f; // -0.5 .. 0.5

                // Very narrow Gaussian along X
                float xFall = Mathf.Exp(-(nx * nx) / (2f * 0.04f * 0.04f));

                // Soft Gaussian along Y — brighter in centre, long fade
                float yFall = Mathf.Exp(-(ny * ny) / (2f * 0.28f * 0.28f));

                // Slight taper: ends are even thinner than the middle
                float taper = 1f - Mathf.Pow(Mathf.Abs(ny) * 1.8f, 3f);
                taper = Mathf.Clamp01(taper);

                float alpha = xFall * yFall * taper;
                alpha = Mathf.Pow(Mathf.Clamp01(alpha), 0.7f); // lift shadows slightly

                px[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ─────────────────────────────────────────────────────────────
    // SYSTEM 2 — Red Wound Spark
    // 4-pointed star with a blazing core.
    // Sharp rays that die quickly — violent, not decorative.
    // ─────────────────────────────────────────────────────────────
    static Texture2D BuildSparkTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)(size - 1)) - 0.5f;
                float ny = (y / (float)(size - 1)) - 0.5f;

                float dist  = Mathf.Sqrt(nx * nx + ny * ny);
                float angle = Mathf.Atan2(ny, nx);

                // 4-pointed star: rays along 0°, 90°, 180°, 270°
                // cos²(2θ) gives 4 lobes, power sharpens the tips
                float star = Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * angle)), 6f);

                // Ray length — shorter between the four arms
                float rayReach = 0.38f * Mathf.Lerp(0.1f, 1f, star);
                float ray = Mathf.Clamp01(1f - dist / Mathf.Max(rayReach, 0.001f));
                ray = Mathf.Pow(ray, 1.8f);

                // Blazing core — tight Gaussian, always bright regardless of angle
                float core = Mathf.Exp(-(dist * dist) / (2f * 0.035f * 0.035f));

                float alpha = Mathf.Clamp01(ray + core * 1.5f);
                px[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ─────────────────────────────────────────────────────────────
    // SYSTEM 3 — Dark Emanation
    // Organic asymmetric smoke puff.
    // Multiple overlapping soft blobs offset from each other,
    // producing an irregular silhouette that never looks like a circle.
    // ─────────────────────────────────────────────────────────────
    static Texture2D BuildEmanationTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];

        // Each blob: (cx, cy, radius, weight)
        float[,] blobs = {
            { 0.50f, 0.50f, 0.22f, 1.0f },   // main body
            { 0.44f, 0.58f, 0.15f, 0.8f },   // upper-left lobe
            { 0.57f, 0.42f, 0.13f, 0.7f },   // lower-right lobe
            { 0.38f, 0.44f, 0.10f, 0.5f },   // left tendril
            { 0.60f, 0.60f, 0.09f, 0.45f },  // upper-right wisp
            { 0.52f, 0.33f, 0.08f, 0.4f },   // bottom wisp
            { 0.63f, 0.48f, 0.07f, 0.35f },  // right wisp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)(size - 1);
                float ny = y / (float)(size - 1);

                float total = 0f;
                for (int b = 0; b < blobs.GetLength(0); b++)
                {
                    float dx = nx - blobs[b, 0];
                    float dy = ny - blobs[b, 1];
                    float r  = blobs[b, 2];
                    float w  = blobs[b, 3];
                    total += w * Mathf.Exp(-(dx * dx + dy * dy) / (2f * r * r));
                }

                // Remap so the body is solid and edges dissolve softly
                float alpha = Mathf.Clamp01(total);
                alpha = Mathf.Pow(alpha, 1.4f); // sharpen centre slightly

                px[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ─────────────────────────────────────────────────────────────
    static void SavePNG(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);
    }
}
#endif
