using UnityEngine;
using UnityEngine.Rendering;

public sealed class OZShotEffects : MonoBehaviour
{
    const int Capacity = 4;
    readonly LineRenderer[] lines = new LineRenderer[Capacity];
    readonly float[] expiry = new float[Capacity];
    Material material;
    int next;

    public void Initialize(Material template)
    {
        if (!template) return;
        material = new Material(template);
        material.SetColor("_BaseColor", new Color(1, .69f, .25f));
        for (int i = 0; i < Capacity; i++)
        {
            var root = new GameObject("Traçante");
            root.transform.SetParent(transform, false);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.positionCount = 2;
            line.startWidth = .033f;
            line.endWidth = .014f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            lines[i] = line;
        }
    }

    public void Show(Vector3 from, Vector3 to)
    {
        if (!material) return;
        var line = lines[next];
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.enabled = true;
        expiry[next] = Time.time + .055f;
        next = (next + 1) % Capacity;
    }

    void LateUpdate()
    {
        if (!material) return;
        for (int i = 0; i < Capacity; i++)
            if (lines[i].enabled && Time.time >= expiry[i])
                lines[i].enabled = false;
    }

    void OnDestroy()
    {
        if (material) Destroy(material);
    }
}
