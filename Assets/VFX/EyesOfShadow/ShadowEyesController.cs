using UnityEngine;
using UnityEngine.VFX;

public class ShadowEyesController : MonoBehaviour
{
    [SerializeField] VisualEffect vfx;
    [SerializeField] Transform player;
    [SerializeField] float proximityRadius = 8f;

    void Update()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        float proximity = Mathf.InverseLerp(proximityRadius, 1f, dist);
        vfx.SetFloat("TendrilDensity", Mathf.Lerp(20f, 80f, proximity));
    }
}
