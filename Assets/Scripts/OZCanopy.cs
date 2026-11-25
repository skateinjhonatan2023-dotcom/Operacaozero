using UnityEngine;
using UnityEngine.Rendering;

public sealed class OZCanopy : MonoBehaviour
{
    Material material;
    Mesh mesh;

    public static GameObject Create(Transform owner)
    {
        var root = new GameObject("Paraquedas");
        root.transform.SetParent(owner, false);
        root.AddComponent<OZCanopy>();
        return root;
    }

    void Awake()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) return;
        material = new Material(shader);
        material.SetColor("_BaseColor", new Color(.19f, .32f, .25f));
        material.SetFloat("_Cull", 0);
        material.SetFloat("_Smoothness", .1f);

        const int panels = 12;
        var vertices = new Vector3[panels + 1];
        var triangles = new int[panels * 3];
        vertices[0] = Vector3.up * 4.6f;
        for (int i = 0; i < panels; i++)
        {
            float angle = i * 2 * Mathf.PI / panels;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 2.2f, 3.5f, Mathf.Sin(angle) * 1.65f);
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = (i + 1) % panels + 1;
            triangles[i * 3 + 2] = i + 1;
        }
        mesh = new Mesh { name = "Dossel de salto" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;

        for (int i = 1; i < vertices.Length; i += 2)
        {
            var rope = new GameObject("Corda").AddComponent<LineRenderer>();
            rope.transform.SetParent(transform, false);
            rope.useWorldSpace = false;
            rope.positionCount = 2;
            rope.SetPosition(0, vertices[i]);
            rope.SetPosition(1, new Vector3(Mathf.Sign(vertices[i].x) * .27f, 1.35f, 0));
            rope.widthMultiplier = .022f;
            rope.shadowCastingMode = ShadowCastingMode.Off;
            rope.sharedMaterial = material;
        }
    }

    void OnDestroy()
    {
        if (mesh) Destroy(mesh);
        if (material) Destroy(material);
    }
}
