using System;
using UnityEngine;

internal sealed class OZLobbyInventory
{
    internal enum Category { Outfit, Rifle, Kit }

    internal readonly struct Item
    {
        internal readonly string Name;
        internal readonly string Description;
        internal readonly Category Category;
        internal readonly int Price;
        internal readonly int Outfit;
        internal readonly int Rifle;

        internal Item(string name, string description, Category category, int price, int outfit, int rifle)
        {
            Name = name;
            Description = description;
            Category = category;
            Price = price;
            Outfit = outfit;
            Rifle = rifle;
        }
    }

    internal static readonly Item[] Items =
    {
        new("SENTINELA", "Verde-oliva e detalhes em latão.", Category.Outfit, 60, 1, 0),
        new("ANOITECER", "Cinza-azulado e detalhes em ardósia.", Category.Outfit, 90, 2, 0),
        new("AÇO DOURADO", "Acabamento bronze para o rifle padrão.", Category.Rifle, 55, 0, 1),
        new("MARÉ ALTA", "Acabamento azul-petróleo para o rifle padrão.", Category.Rifle, 75, 0, 2),
        new("KIT COSTA DE FERRO", "Uniforme Sentinela e acabamento Aço Dourado.", Category.Kit, 100, 1, 1),
        new("KIT MAR ABERTO", "Uniforme Anoitecer e acabamento Maré Alta.", Category.Kit, 140, 2, 2)
    };

    static readonly Color[] Outfits =
    {
        Color.white, new(.82f, .90f, .62f), new(.59f, .72f, 1f)
    };
    static readonly Color[] Rifles =
    {
        Color.white, new(1f, .70f, .32f), new(.52f, .83f, .93f)
    };
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    const string SaveKey = "OZ_LOBBY_COLLECTION_V1";

    [Serializable]
    sealed class Data
    {
        public int spent;
        public int outfits = 1;
        public int rifles = 1;
        public int selectedOutfit;
        public int selectedRifle;
    }

    readonly Data data;

    internal OZLobbyInventory()
    {
        try
        {
            data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(SaveKey, "")) ?? new Data();
        }
        catch (ArgumentException)
        {
            data = new Data();
        }

        data.spent = Mathf.Max(0, data.spent);
        data.outfits |= 1;
        data.rifles |= 1;
        data.selectedOutfit = ValidateSelection(data.selectedOutfit, data.outfits, Outfits.Length);
        data.selectedRifle = ValidateSelection(data.selectedRifle, data.rifles, Rifles.Length);
    }

    static int ValidateSelection(int value, int owned, int count) =>
        value >= 0 && value < count && Has(owned, value) ? value : 0;

    static bool Has(int set, int index) => (set & (1 << index)) != 0;

    internal int Balance(int xp) => Mathf.Max(0, xp / 5 - data.spent);
    internal int Outfit => data.selectedOutfit;
    internal int Rifle => data.selectedRifle;
    internal bool Owns(Item item) => Has(data.outfits, item.Outfit) && Has(data.rifles, item.Rifle);
    internal bool IsEquipped(Item item) =>
        (item.Outfit == 0 || Outfit == item.Outfit) && (item.Rifle == 0 || Rifle == item.Rifle);

    internal int Cost(Item item)
    {
        if (Owns(item)) return 0;
        if (item.Category != Category.Kit) return item.Price;
        if (Has(data.outfits, item.Outfit))
            return Array.Find(Items, single => single.Category == Category.Rifle && single.Rifle == item.Rifle).Price;
        if (Has(data.rifles, item.Rifle))
            return Array.Find(Items, single => single.Category == Category.Outfit && single.Outfit == item.Outfit).Price;
        return item.Price;
    }

    internal bool Equip(Item item, int xp)
    {
        int cost = Cost(item);
        if (cost > Balance(xp)) return false;

        data.spent += cost;
        data.outfits |= 1 << item.Outfit;
        data.rifles |= 1 << item.Rifle;
        if (item.Outfit != 0) data.selectedOutfit = item.Outfit;
        if (item.Rifle != 0) data.selectedRifle = item.Rifle;
        Save();
        return true;
    }

    internal void UseStandard(Category category)
    {
        if (category == Category.Outfit) data.selectedOutfit = 0;
        if (category == Category.Rifle) data.selectedRifle = 0;
        Save();
    }

    void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    internal void Apply(GameObject root, Transform rifle, bool lobbyModel)
    {
        var block = new MaterialPropertyBlock();
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            bool weapon = rifle && renderer.transform.IsChildOf(rifle);
            if (!weapon && renderer is not SkinnedMeshRenderer) continue;
            var materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                var material = materials[index];
                if (!material || !material.HasProperty(BaseColor)) continue;
                bool uniform = lobbyModel
                    ? IsUniform(material.name)
                    : material.name.StartsWith("Operative", StringComparison.Ordinal);
                if (!weapon && !uniform) continue;

                renderer.GetPropertyBlock(block, index);
                block.SetColor(BaseColor, material.GetColor(BaseColor) * (weapon ? Rifles[Rifle] : Outfits[Outfit]));
                renderer.SetPropertyBlock(block, index);
                block.Clear();
            }
        }
    }

    static bool IsUniform(string name) =>
        name.IndexOf("cloth", StringComparison.OrdinalIgnoreCase) >= 0 ||
        name.IndexOf("fabric", StringComparison.OrdinalIgnoreCase) >= 0 ||
        name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0;
}
