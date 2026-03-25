using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EyeWallManager : MonoBehaviour
{
    [SerializeField] GameObject eyePrefab;
    [SerializeField] Camera cam;

    [Header("Concentric Rings")]
    [SerializeField] int ringCount = 5;
    [SerializeField] float ringSpacing = 2.0f;
    [SerializeField] float eyeSpacing = 1.8f;
    [SerializeField] bool centerEye = true;

    [Header("Tracking")]
    [SerializeField] float trackingSpeed = 0.8f;

    [Header("Depth & Scale")]
    [SerializeField] float wallDepth = 4f;
    [SerializeField] float depthVariation = 0.4f;
    [SerializeField] float minScale = 1.2f;
    [SerializeField] float maxScale = 2.0f;

    [Header("Organic Drift")]
    [SerializeField] float driftAmount    = 0.18f;  // world units of positional sway
    [SerializeField] float driftSpeed     = 0.06f;  // base frequency (slow, creeping)

    [Header("Scale Pulse")]
    [SerializeField] float pulseAmount    = 0.12f;  // fraction of base scale
    [SerializeField] float pulseSpeed     = 0.09f;

    [Header("Infinite Illusion")]
    [SerializeField] float outerCullRadius = 14f;   // eyes beyond this fade out and respawn
    [SerializeField] float fadeSpeed       = 1.2f;

    // ─── per-eye data ───────────────────────────────────────────────────────
    class EyeData
    {
        public Transform t;
        public Renderer[] renderers;
        public Vector3    origin;       // home position (ring slot)
        public float      baseScale;
        public float      phasePos;     // unique drift phase
        public float      phaseScale;   // unique pulse phase
        // irrational frequency multipliers so no two eyes ever sync
        public float      freqX, freqY, freqZ, freqS;
        // fade state
        public float      alpha = 1f;
        public bool       fading;
        public float      respawnTimer;
    }

    readonly List<EyeData> eyeData = new();

    // ─── spawn ──────────────────────────────────────────────────────────────
    void Start()
    {
        if (centerEye)
            Register(SpawnAt(new Vector3(0, 0, wallDepth)));

        for (int ring = 1; ring <= ringCount; ring++)
        {
            float radius = ring * ringSpacing;
            int count = Mathf.Max(4, Mathf.RoundToInt(2f * Mathf.PI * radius / eyeSpacing));
            float angleOffset = (ring % 2 == 0) ? Mathf.PI / count : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count + angleOffset;
                Vector3 pos = new Vector3(
                    radius * Mathf.Cos(angle),
                    radius * Mathf.Sin(angle),
                    wallDepth + Random.Range(-depthVariation, depthVariation)
                );
                Register(SpawnAt(pos));
            }
        }
    }

    GameObject SpawnAt(Vector3 pos)
    {
        float scale = Random.Range(minScale, maxScale);
        var go = Instantiate(eyePrefab, pos, Quaternion.Euler(0, 180, 0), transform);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    void Register(GameObject go)
    {
        // Irrational prime-ratio multipliers — pattern never repeats between eyes
        static float Irrational() => 0.71f + Random.value * 1.43f;

        var d = new EyeData
        {
            t          = go.transform,
            renderers  = go.GetComponentsInChildren<Renderer>(),
            origin     = go.transform.position,
            baseScale  = go.transform.localScale.x,
            phasePos   = Random.Range(0f, 100f),
            phaseScale = Random.Range(0f, 100f),
            freqX      = Irrational(),
            freqY      = Irrational() * 1.31f,
            freqZ      = Irrational() * 0.79f,
            freqS      = Irrational() * 1.13f,
        };
        eyeData.Add(d);
    }

    // ─── update ─────────────────────────────────────────────────────────────
    void Update()
    {
        float t = Time.time;

        // Mouse world position on the wall plane
        Vector3 mouseWorld = Vector3.zero;
        bool hasMouseTarget = false;

        if (Mouse.current != null)
        {
            Vector2 mp = Mouse.current.position.ReadValue();
            if (!float.IsNaN(mp.x) && !float.IsNaN(mp.y) &&
                mp.x >= 0 && mp.y >= 0 &&
                mp.x <= Screen.width && mp.y <= Screen.height)
            {
                Ray ray = cam.ScreenPointToRay(mp);
                Plane plane = new Plane(-cam.transform.forward, new Vector3(0, 0, wallDepth));
                if (plane.Raycast(ray, out float dist))
                {
                    mouseWorld = ray.GetPoint(dist);
                    hasMouseTarget = true;
                }
            }
        }

        foreach (var d in eyeData)
        {
            if (d.t == null) continue;

            float tp = t + d.phasePos;
            float ts = t + d.phaseScale;

            // ── Organic position drift (three incommensurable axes) ──────────
            float dx = Mathf.Sin(tp * driftSpeed * d.freqX) * driftAmount;
            float dy = Mathf.Sin(tp * driftSpeed * d.freqY) * driftAmount * 0.6f;
            float dz = Mathf.Sin(tp * driftSpeed * d.freqZ) * driftAmount * 0.4f;
            Vector3 drifted = d.origin + new Vector3(dx, dy, dz);

            // ── Scale pulse (slow breath) ────────────────────────────────────
            float pulse = 1f + Mathf.Sin(ts * pulseSpeed * d.freqS) * pulseAmount;
            float targetScale = d.baseScale * pulse;

            if (!d.fading)
            {
                d.t.position   = drifted;
                d.t.localScale = Vector3.one * targetScale * d.alpha;

                // Begin fade if eye drifted or was placed too far from center
                float radial = new Vector2(d.origin.x, d.origin.y).magnitude;
                if (radial > outerCullRadius)
                    d.fading = true;
            }
            else
            {
                // ── Fade out → respawn closer (infinite feel) ───────────────
                d.alpha -= fadeSpeed * Time.deltaTime;
                d.t.localScale = Vector3.one * d.baseScale * Mathf.Max(0f, d.alpha);

                if (d.alpha <= 0f)
                {
                    // Respawn at a random inner ring slot
                    int ring = Random.Range(1, ringCount + 1);
                    float radius = ring * ringSpacing;
                    float angle  = Random.Range(0f, 2f * Mathf.PI);
                    d.origin = new Vector3(
                        radius * Mathf.Cos(angle),
                        radius * Mathf.Sin(angle),
                        wallDepth + Random.Range(-depthVariation, depthVariation)
                    );
                    d.baseScale  = Random.Range(minScale, maxScale);
                    d.phasePos   = Random.Range(0f, 100f);
                    d.phaseScale = Random.Range(0f, 100f);
                    d.alpha      = 0f;
                    d.fading     = false;
                }
            }

            // Fade in from 0 → 1 when not fading out
            if (!d.fading && d.alpha < 1f)
                d.alpha = Mathf.Min(1f, d.alpha + fadeSpeed * Time.deltaTime);

            // ── Mouse tracking ───────────────────────────────────────────────
            if (hasMouseTarget)
            {
                Vector3 dir = mouseWorld - d.t.position;
                if (dir.sqrMagnitude >= 0.001f)
                {
                    Quaternion target = Quaternion.LookRotation(dir);
                    d.t.rotation = Quaternion.Slerp(d.t.rotation, target, trackingSpeed * Time.deltaTime);
                }
            }
        }
    }
}
