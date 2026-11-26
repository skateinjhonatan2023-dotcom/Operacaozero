using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LineRenderer))]
public sealed class OZZoneVisual : MonoBehaviour
{
    const int Segments = 128;
    readonly Vector3[] points = new Vector3[Segments];
    LineRenderer line;
    Material material;

    public void Initialize(Material template)
    {
        line = GetComponent<LineRenderer>();
        line.loop = true;
        line.positionCount = Segments;
        line.widthMultiplier = .26f;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        if (template)
        {
            material = new Material(template);
            material.SetColor("_BaseColor", new Color(.19f, .74f, 1));
            line.sharedMaterial = material;
        }
    }

    public void SetRadius(float radius)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * 2 * Mathf.PI / Segments;
            var point = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            if (Physics.Raycast(point + Vector3.up * 120, Vector3.down, out var hit, 200,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                point.y = hit.point.y;
            points[i] = point + Vector3.up * .4f;
        }
        line.SetPositions(points);
    }

    void OnDestroy()
    {
        if (material) Destroy(material);
    }
}
