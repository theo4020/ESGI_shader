using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class RitualCameraDirector : MonoBehaviour
{
    [SerializeField] CinemachineCamera vcamCircle;
    [SerializeField] CinemachineCamera vcamPool;
    [SerializeField] MagicCircleDriver circleDriver;
    [SerializeField] float             returnDelay = 2.5f;

    const int PriorityHigh = 15;
    const int PriorityLow  = 10;

    Coroutine m_ReturnCoroutine;

    void OnEnable()
    {
        if (circleDriver == null) return;
        circleDriver.OnDropRelease   += HandleDropRelease;
        circleDriver.OnChargingStart += HandleChargingStart;
    }

    void OnDisable()
    {
        if (circleDriver == null) return;
        circleDriver.OnDropRelease   -= HandleDropRelease;
        circleDriver.OnChargingStart -= HandleChargingStart;
    }

    void Start()
    {
        if (vcamCircle != null) vcamCircle.Priority = PriorityHigh;
        if (vcamPool   != null) vcamPool.Priority   = PriorityLow;
    }

    void HandleDropRelease()
    {
        if (m_ReturnCoroutine != null) { StopCoroutine(m_ReturnCoroutine); m_ReturnCoroutine = null; }
        if (vcamPool   != null) vcamPool.Priority   = PriorityHigh;
        if (vcamCircle != null) vcamCircle.Priority = PriorityLow;
    }

    void HandleChargingStart()
    {
        if (m_ReturnCoroutine != null) StopCoroutine(m_ReturnCoroutine);
        m_ReturnCoroutine = StartCoroutine(DelayedReturnToCircle());
    }

    IEnumerator DelayedReturnToCircle()
    {
        yield return new WaitForSeconds(returnDelay);
        if (vcamCircle != null) vcamCircle.Priority = PriorityHigh;
        if (vcamPool   != null) vcamPool.Priority   = PriorityLow;
        m_ReturnCoroutine = null;
    }
}
