using System.Collections.Generic;
using UnityEngine;

public class RitualEyeManager : MonoBehaviour
{
    [SerializeField] GameObject eyePrefab;
    [SerializeField] Transform  dropTransform;

    [SerializeField] float trackingSpeed   = 1.4f;
    [SerializeField] float scatterStrength = 90f;
    [SerializeField] float scatterDecay    = 2.2f;

    static readonly Vector3[] Positions =
    {
        // Pillar clusters
        new( 6.9f,  2.1f,  0.35f), new( 7.3f,  3.6f, -0.2f),  new( 6.6f,  1.1f,  0.5f),
        new( 3.2f,  1.9f,  5.95f), new( 3.7f,  3.3f,  6.25f), new( 3.0f,  0.8f,  5.7f),
        new(-3.5f,  2.3f,  6.05f), new(-3.1f,  3.7f,  5.85f),
        new(-7.1f,  2.0f,  0.15f), new(-6.8f,  3.4f, -0.35f), new(-7.3f,  1.1f, -0.2f),
        new(-3.4f,  2.1f, -6.0f),  new(-3.0f,  3.6f, -5.8f),
        new( 3.5f,  2.4f, -6.05f), new( 3.8f,  3.8f, -5.75f), new( 3.2f,  1.0f, -5.9f),
        // Ground creepers
        new( 5.6f,  0.25f,  3.1f), new(-5.1f,  0.3f, -4.1f),
        new( 0.4f,  0.2f,   6.6f), new(-0.4f,  0.25f,-6.6f),
        new( 8.0f,  0.2f,  -1.0f), new(-8.1f,  0.2f,  1.2f),
        // Upper darkness
        new( 1.1f,  5.2f,  1.1f),  new(-1.6f,  5.0f, -0.6f),
        new( 2.1f,  5.7f, -2.1f),  new(-2.6f,  5.4f,  2.1f),
        new( 0.2f,  6.2f,  0.3f),  new(-0.5f,  5.9f, -0.2f),
    };

    readonly List<Transform> eyes         = new();
    readonly List<float>     scatterAngles = new();
    Vector3 targetPoint;

    void Start()
    {
        targetPoint = Vector3.zero;
        foreach (var pos in Positions)
        {
            float scale = Random.Range(0.55f, 1.35f);
            var go = Instantiate(eyePrefab, pos, Quaternion.Euler(0f, 180f, 0f), transform);
            go.transform.localScale = Vector3.one * scale;
            eyes.Add(go.transform);
            scatterAngles.Add(0f);
        }
    }

    void Update()
    {
        targetPoint = (dropTransform != null && dropTransform.gameObject.activeSelf)
            ? dropTransform.position
            : new Vector3(0f, -0.04f, 0f);

        for (int i = 0; i < eyes.Count; i++)
        {
            if (eyes[i] == null) continue;

            Vector3 dir = targetPoint - eyes[i].position;
            if (dir.sqrMagnitude < 0.001f) continue;

            Quaternion target = Quaternion.LookRotation(dir);

            if (scatterAngles[i] > 0.5f)
            {
                scatterAngles[i] = Mathf.Max(0f, scatterAngles[i] - scatterDecay * Time.deltaTime);
                float noise = scatterAngles[i];
                target *= Quaternion.Euler(
                    Random.Range(-noise, noise),
                    Random.Range(-noise, noise),
                    0f);
            }

            eyes[i].rotation = Quaternion.Slerp(eyes[i].rotation, target, trackingSpeed * Time.deltaTime);
        }
    }

    public void Scatter()
    {
        for (int i = 0; i < scatterAngles.Count; i++)
            scatterAngles[i] = scatterStrength;
    }
}
