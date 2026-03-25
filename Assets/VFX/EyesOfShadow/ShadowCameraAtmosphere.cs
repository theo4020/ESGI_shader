using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Atmospheric camera behaviour for Eyes of Shadow.
/// Attach to the Main Camera. Assign the Global Volume in the Inspector.
public class ShadowCameraAtmosphere : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Volume postProcessVolume;

    [Header("Breathing")]
    [SerializeField] float breathSpeed  = 0.25f;
    [SerializeField] float breathAmount = 0.6f;   // FOV units

    [Header("Micro Sway")]
    [SerializeField] float swaySpeed  = 0.18f;
    [SerializeField] float swayAmount = 0.12f;    // degrees

    Camera          cam;
    float           baseFOV;
    float           phase;

    Vignette            vignette;
    ChromaticAberration chromatic;

    void Start()
    {
        cam     = GetComponent<Camera>();
        baseFOV = cam.fieldOfView;
        phase   = Random.Range(0f, 100f);

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out chromatic);
        }
    }

    void Update()
    {
        float t = Time.time + phase;

        // Breathing — two irrational frequencies so it never repeats cleanly
        float breath = Mathf.Sin(t * breathSpeed)        * 0.65f
                     + Mathf.Sin(t * breathSpeed * 1.73f) * 0.35f;
        cam.fieldOfView = baseFOV + breath * breathAmount;

        // Micro sway — slight roll makes it feel alive, not mechanical
        float rx = Mathf.Sin(t * swaySpeed * 0.93f) * swayAmount * 0.5f;
        float ry = Mathf.Sin(t * swaySpeed * 1.17f) * swayAmount;
        float rz = Mathf.Sin(t * swaySpeed * 0.61f) * swayAmount * 0.25f;
        transform.localRotation = Quaternion.Euler(rx, ry, rz);

        // Vignette — slowly tightens and releases, no fixed rhythm
        if (vignette != null)
        {
            float v = 0.55f + Mathf.Sin(t * 0.13f) * 0.07f
                            + Mathf.Sin(t * 0.31f) * 0.03f;
            vignette.intensity.Override(v);
        }

        // Chromatic aberration — spikes briefly at irregular intervals
        if (chromatic != null)
        {
            float c = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 0.37f)), 6f) * 0.5f;
            chromatic.intensity.Override(c);
        }
    }
}
