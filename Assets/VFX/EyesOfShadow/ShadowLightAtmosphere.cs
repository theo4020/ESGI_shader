using UnityEngine;

/// Atmospheric light behaviour for Eyes of Shadow.
/// Attach to the Directional Light.
public class ShadowLightAtmosphere : MonoBehaviour
{
    [Header("Intensity")]
    [SerializeField] float baseIntensity      = 0.4f;
    [SerializeField] float intensityVariation = 0.15f;

    [Header("Color")]
    [SerializeField] Color coldBlue  = new Color(0.35f, 0.05f, 0.04f);
    [SerializeField] Color voidBlack = new Color(0.06f, 0.01f, 0.01f);
    [SerializeField] float colorSpeed = 0.08f;

    [Header("Rotation Drift")]
    [SerializeField] float driftAmount = 4f;    // degrees
    [SerializeField] float driftSpeed  = 0.07f;

    Light  directionalLight;
    float  phase;
    Vector3 baseRotation;

    void Start()
    {
        directionalLight = GetComponent<Light>();
        phase            = Random.Range(0f, 100f);
        baseRotation     = transform.eulerAngles;
    }

    void Update()
    {
        float t = Time.time + phase;

        // Intensity — three overlapping incommensurable frequencies
        // produces a pattern that never fully repeats: broken cycle, like Shadow
        float wave = Mathf.Sin(t * 0.29f) * 0.50f
                   + Mathf.Sin(t * 0.67f) * 0.31f
                   + Mathf.Sin(t * 1.31f) * 0.19f;
        wave = (wave + 1f) * 0.5f;   // remap to 0-1
        directionalLight.intensity = baseIntensity + wave * intensityVariation;

        // Color — drifts between cold blue and near-void black
        float colorT = (Mathf.Sin(t * colorSpeed) + 1f) * 0.5f;
        directionalLight.color = Color.Lerp(voidBlack, coldBlue, colorT);

        // Subtle rotation drift — light angle shifts almost imperceptibly
        float dx = Mathf.Sin(t * driftSpeed * 0.83f) * driftAmount;
        float dy = Mathf.Sin(t * driftSpeed * 1.27f) * driftAmount * 0.5f;
        transform.eulerAngles = baseRotation + new Vector3(dx, dy, 0f);
    }
}
