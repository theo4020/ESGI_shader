using UnityEngine;
using UnityEngine.VFX;

public class ImpactEventBridge : MonoBehaviour
{
    [SerializeField] RitualDropController dropController;
    [SerializeField] VisualEffect         impactBurstVFX;
    [SerializeField] VisualEffect         waterDropsVFX;
    [SerializeField] Renderer             poolRenderer;
    [SerializeField] Light                holeRimLight;
    [SerializeField] GameObject sparksInstance;

    [Header("Rim Light Flash")]
    [SerializeField] float flashIntensity = 6f;
    [SerializeField] float flashDecay     = 4f;

    [Header("Wave Amplitude Pulse")]
    [SerializeField] float amplitudeMultiplier = 3.0f;  // spike = base * this
    [SerializeField] float amplitudeDecay      = 0.6f;

    static readonly int AmplitudeID = Shader.PropertyToID("_Amplitude");

    float rimBaseIntensity;
    float rimFlashCurrent;
    float amplitudeBase;
    float amplitudeCurrent;
    Material m_PoolMat;

    void OnEnable()  { if (dropController) dropController.OnImpact += HandleImpact; }
    void OnDisable() { if (dropController) dropController.OnImpact -= HandleImpact; }

    void Start()
    {
        if (holeRimLight != null) rimBaseIntensity = holeRimLight.intensity;

        // Read the actual amplitude from the shared material — never override the artist's value
        if (poolRenderer != null)
        {
            amplitudeBase    = poolRenderer.sharedMaterial.GetFloat(AmplitudeID);
            amplitudeCurrent = amplitudeBase;
            m_PoolMat = poolRenderer.material;
        }
    }

    void Update()
    {
        if (holeRimLight != null && rimFlashCurrent > 0f)
        {
            rimFlashCurrent = Mathf.Max(0f, rimFlashCurrent - Time.deltaTime * flashDecay);
            holeRimLight.intensity = rimBaseIntensity + rimFlashCurrent;
        }

        if (poolRenderer != null && amplitudeCurrent > amplitudeBase)
        {
            amplitudeCurrent = Mathf.Lerp(amplitudeCurrent, amplitudeBase, Time.deltaTime * amplitudeDecay);
            if (amplitudeCurrent - amplitudeBase < 0.001f) amplitudeCurrent = amplitudeBase;
            m_PoolMat.SetFloat(AmplitudeID, amplitudeCurrent);
        }
    }

    void OnDestroy()
    {
        if (m_PoolMat != null) Object.Destroy(m_PoolMat);
    }

    void HandleImpact()
    {
        if (impactBurstVFX != null) { impactBurstVFX.transform.position = Vector3.zero; impactBurstVFX.Play(); }
        if (waterDropsVFX  != null) { waterDropsVFX.transform.position  = Vector3.zero; waterDropsVFX.Play(); }

        amplitudeCurrent = amplitudeBase * amplitudeMultiplier;
        rimFlashCurrent  = flashIntensity;
        if (sparksInstance != null)
        {
            sparksInstance.SetActive(false);
            sparksInstance.SetActive(true);
        }
    }
}
