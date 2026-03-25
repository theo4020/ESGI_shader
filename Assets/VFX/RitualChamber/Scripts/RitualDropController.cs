using UnityEngine;

public class RitualDropController : MonoBehaviour
{
    [SerializeField] MagicCircleDriver circleDriver;
    [SerializeField] Transform         dropTransform;
    [SerializeField] Renderer          dropRenderer;

    [Header("Drop")]
    [SerializeField] float spawnHeight  =  4.0f;
    [SerializeField] float impactHeight = -0.5f;
    [SerializeField] float fallDuration =  1.35f;
    [SerializeField] float squashAmount =  0.38f;

    public event System.Action OnImpact;

    static readonly int FallProgressID = Shader.PropertyToID("_FallProgress");

    bool  falling;
    bool  impactFired;
    float fallTimer;

    void OnEnable()  { if (circleDriver) circleDriver.OnDropRelease += StartFall; }
    void OnDisable() { if (circleDriver) circleDriver.OnDropRelease -= StartFall; }

    void StartFall()
    {
        falling     = true;
        impactFired = false;
        fallTimer   = 0f;
        if (dropTransform != null)
        {
            dropTransform.gameObject.SetActive(true);
            dropTransform.position = new Vector3(0f, spawnHeight, 0f);
        }
    }

    void Update()
    {
        if (!falling || dropTransform == null) return;

        fallTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fallTimer / fallDuration);

        float eased = t * t;
        float y = Mathf.Lerp(spawnHeight, impactHeight, eased);
        dropTransform.position = new Vector3(0f, y, 0f);

        float squash = 1f - squashAmount * Mathf.Pow(t, 2.5f);
        float stretch = 1f / Mathf.Max(squash, 0.01f);
        dropTransform.localScale = new Vector3(0.28f * squash, 0.28f * stretch, 0.28f * squash);

        if (dropRenderer != null)
            dropRenderer.material.SetFloat(FallProgressID, t);

        // Fire impact slightly early so VFX is already spawning when drop hits the surface
        if (!impactFired && t >= 0.92f)
        {
            impactFired = true;
            OnImpact?.Invoke();
        }

        if (t >= 1f)
        {
            falling = false;
            dropTransform.gameObject.SetActive(false);
        }
    }
}
