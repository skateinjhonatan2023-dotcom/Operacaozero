using UnityEngine;

public sealed class OZLoot : MonoBehaviour
{
    public enum Kind { Ammo, Medkit, Armor, Helmet }
    public Kind Item { get; private set; }
    public string Label => labels[(int)Item];

    static readonly string[] labels = { "MUNIÇÃO", "KIT MÉDICO", "COLETE", "CAPACETE" };

    static readonly Color[] colors = {
        new Color(.95f, .69f, .26f), new Color(.82f, .30f, .34f),
        new Color(.27f, .66f, .85f), new Color(.42f, .85f, .51f)
    };

    public static OZLoot Create(Kind kind, Vector3 position, Material material)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = labels[(int)kind];
        marker.transform.position = position + Vector3.up * .09f;
        marker.transform.localScale = new Vector3(.36f, .08f, .36f);
        marker.GetComponent<Collider>().enabled = false;
        if (material)
        {
            var renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", colors[(int)kind]);
            renderer.SetPropertyBlock(block);
        }
        var loot = marker.AddComponent<OZLoot>();
        loot.Item = kind;
        return loot;
    }
}
