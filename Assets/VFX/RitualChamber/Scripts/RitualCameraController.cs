using System.Collections;
using UnityEngine;

public class RitualCameraController : MonoBehaviour
{
    [SerializeField] RitualDropController dropController;
    [SerializeField] Vector3 lookTarget    = new Vector3(0f, 0.35f, 0f);

    [Header("Orbit")]
    [SerializeField] float orbitSpeed     = 5.5f;
    [SerializeField] float orbitRadius    = 5.2f;
    [SerializeField] float orbitHeight    = 2.55f;

    [Header("Breathing")]
    [SerializeField] float breathAmplitude = 0.06f;
    [SerializeField] float breathFrequency = 0.23f;

    [Header("Impact")]
    [SerializeField] float fovPunchAmount  = 9f;
    [SerializeField] float fovPunchIn      = 0.12f;
    [SerializeField] float fovPunchOut     = 0.45f;

    float angle;
    float baseFOV;

    void OnEnable()  { if (dropController) dropController.OnImpact += OnImpact; }
    void OnDisable() { if (dropController) dropController.OnImpact -= OnImpact; }

    void Start()
    {
        baseFOV = GetComponent<Camera>().fieldOfView;
        // Start at a dramatic angle
        angle = -25f * Mathf.Deg2Rad;
    }

    void Update()
    {
        angle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

        float breath = Mathf.Sin(Time.time * breathFrequency * 2f * Mathf.PI) * breathAmplitude;
        float r = orbitRadius + breath;
        float h = orbitHeight + Mathf.Sin(Time.time * breathFrequency * 0.7f * 2f * Mathf.PI) * breathAmplitude * 0.5f;

        transform.position = new Vector3(r * Mathf.Cos(angle), h, r * Mathf.Sin(angle));
        transform.LookAt(lookTarget);
    }

    void OnImpact() => StartCoroutine(FovPunch());

    IEnumerator FovPunch()
    {
        var cam = GetComponent<Camera>();
        float t = 0f;

        while (t < fovPunchIn)
        {
            t += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(baseFOV, baseFOV - fovPunchAmount, t / fovPunchIn);
            yield return null;
        }

        t = 0f;
        while (t < fovPunchOut)
        {
            t += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(baseFOV - fovPunchAmount, baseFOV, t / fovPunchOut);
            yield return null;
        }

        cam.fieldOfView = baseFOV;
    }
}
