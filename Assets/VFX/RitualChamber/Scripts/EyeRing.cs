using UnityEngine;

public class EyeRing : MonoBehaviour
{
    [SerializeField] int                  eyeCount       = 8;
    [SerializeField] float                ringRadius     = 2.0f;
    [SerializeField] GameObject           eyePrefab;
    [SerializeField] MagicCircleDriver    circleDriver;
    [SerializeField] RitualDropController dropController;
    [SerializeField] Transform            dropTransform;

    [Header("Tracking")]
    [SerializeField] float trackingSpeed = 3.5f;
    [SerializeField] float idleSpeed     = 1.2f;

    [Header("Impact Scatter")]
    [SerializeField] float scatterAngle = 25f;
    [SerializeField] float scatterDecay = 2.2f;

    static readonly Vector3 IdleTarget = new(0f, -0.5f, 0f);

    Transform[]  eyes;
    Quaternion[] scatterOffsets;
    bool         tracking;

    void Awake()
    {
        if (eyePrefab == null) return;

        eyes           = new Transform[eyeCount];
        scatterOffsets = new Quaternion[eyeCount];

        for (int i = 0; i < eyeCount; i++)
        {
            float angle = 2f * Mathf.PI * i / eyeCount;
            var pos = new Vector3(
                ringRadius * Mathf.Cos(angle),
                0.05f,
                ringRadius * Mathf.Sin(angle));

            var go = Instantiate(eyePrefab, transform);
            go.name = $"RingEye_{i}";
            go.transform.localPosition = pos;

            eyes[i]           = go.transform;
            scatterOffsets[i] = Quaternion.identity;
        }
    }

    void OnEnable()
    {
        if (circleDriver)   circleDriver.OnDropRelease  += OnDropRelease;
        if (dropController) dropController.OnImpact     += OnImpact;
    }

    void OnDisable()
    {
        if (circleDriver)   circleDriver.OnDropRelease  -= OnDropRelease;
        if (dropController) dropController.OnImpact     -= OnImpact;
    }

    void OnDropRelease() { tracking = true; }

    void OnImpact()
    {
        tracking = false;
        if (eyes == null) return;
        for (int i = 0; i < eyes.Length; i++)
            scatterOffsets[i] = Quaternion.Euler(
                Random.Range(-scatterAngle, scatterAngle),
                Random.Range(-scatterAngle, scatterAngle),
                0f);
    }

    void Update()
    {
        if (eyes == null) return;

        float dt = Time.deltaTime;

        for (int i = 0; i < eyes.Length; i++)
        {
            scatterOffsets[i] = Quaternion.Slerp(scatterOffsets[i], Quaternion.identity, dt * scatterDecay);

            Vector3 target  = tracking && dropTransform != null ? dropTransform.position : IdleTarget;
            float   speed   = tracking ? trackingSpeed : idleSpeed;
            var     lookRot = Quaternion.LookRotation(target - eyes[i].position);
            var     smooth  = Quaternion.Slerp(eyes[i].rotation, lookRot, dt * speed);

            eyes[i].rotation = smooth * scatterOffsets[i];
        }
    }
}
