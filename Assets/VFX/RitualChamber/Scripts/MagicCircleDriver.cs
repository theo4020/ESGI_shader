using UnityEngine;
using UnityEngine.VFX;

public class MagicCircleDriver : MonoBehaviour
{
    [SerializeField] Renderer      circleRenderer;
    [SerializeField] Light         circleLight;
    [SerializeField] VisualEffect  energyFeedVFX;

    [Header("Timing")]
    [SerializeField] float chargeTime   = 3.2f;
    [SerializeField] float holdTime     = 1.1f;
    [SerializeField] float releaseTime  = 0.35f;
    [SerializeField] float cooldownTime = 2.4f;

    [Header("Light")]
    [SerializeField] float maxLightIntensity = 7f;

    public event System.Action OnDropRelease;
    public event System.Action OnCooldownStart;
    public event System.Action OnChargingStart;

    enum State { Charging, Holding, Releasing, Cooldown }
    State state = State.Charging;
    float timer;
    float chargeAmount;

    static readonly int ChargeID = Shader.PropertyToID("_ChargeAmount");

    void Update()
    {
        timer += Time.deltaTime;

        switch (state)
        {
            case State.Charging:
                chargeAmount = Mathf.SmoothStep(0f, 1f, timer / chargeTime);
                if (energyFeedVFX != null) energyFeedVFX.enabled = true;
                if (timer >= chargeTime) Transition(State.Holding);
                break;

            case State.Holding:
                chargeAmount = 1f;
                if (timer >= holdTime)
                {
                    OnDropRelease?.Invoke();
                    Transition(State.Releasing);
                }
                break;

            case State.Releasing:
                chargeAmount = Mathf.SmoothStep(1f, 0f, timer / releaseTime);
                if (energyFeedVFX != null) energyFeedVFX.enabled = false;
                if (timer >= releaseTime) Transition(State.Cooldown);
                break;

            case State.Cooldown:
                chargeAmount = 0f;
                if (timer >= cooldownTime) Transition(State.Charging);
                break;
        }

        if (circleRenderer != null)
            circleRenderer.material.SetFloat(ChargeID, chargeAmount);

        if (circleLight != null)
            circleLight.intensity = chargeAmount * maxLightIntensity;
    }

    void Transition(State next)
    {
        if (next == State.Cooldown)  OnCooldownStart?.Invoke();
        if (next == State.Charging) OnChargingStart?.Invoke();
        state = next;
        timer = 0f;
    }
}
