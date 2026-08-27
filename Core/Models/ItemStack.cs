using System;
using System.Collections.ObjectModel;
using System.Linq;
using BedrockInventoryEditor.Core.Nbt;
using BedrockInventoryEditor.Core.Registry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.Core.Models;

public enum SlotLocation
{
    Hotbar,
    MainBag,
    ArmorHelmet,
    ArmorChestplate,
    ArmorLeggings,
    ArmorBoots,
    Offhand,
    EnderChest
}

public partial class ItemStack : ObservableObject
{
    [ObservableProperty]
    private byte _slot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(ItemCategory))]
    private string _id = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private byte _count = 0;

    [ObservableProperty]
    private short _damage = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _customName = string.Empty;

    [ObservableProperty]
    private SlotLocation _location;

    public ObservableCollection<string> Lore { get; } = [];
    public ObservableCollection<EnchantmentEntry> Enchantments { get; } = [];

    public NbtCompound? ExtraNbt { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Id) || Id == "minecraft:air" || Count == 0;

    public string DisplayName => !string.IsNullOrWhiteSpace(CustomName)
        ? CustomName
        : BedrockItemRegistry.GetDisplayName(Id);

    public string ItemCategory => IsEmpty ? "" : (BedrockItemRegistry.Items.FirstOrDefault(i => i.Id.Equals(Id, StringComparison.OrdinalIgnoreCase))?.Category ?? "Custom");

    public string WatermarkIcon => Location switch
    {
        SlotLocation.ArmorHelmet => "🪖",
        SlotLocation.ArmorChestplate => "🦺",
        SlotLocation.ArmorLeggings => "👖",
        SlotLocation.ArmorBoots => "👢",
        SlotLocation.Offhand => "🛡️",
        _ => ""
    };

    public ItemStack() { }

    public ItemStack(byte slot, SlotLocation location)
    {
        Slot = slot;
        Location = location;
    }

    public void Clear()
    {
        Id = string.Empty;
        Count = 0;
        Damage = 0;
        CustomName = string.Empty;
        Lore.Clear();
        Enchantments.Clear();
        ExtraNbt = null;
    }

    public ItemStack Clone()
    {
        var copy = new ItemStack(Slot, Location)
        {
            Id = Id,
            Count = Count,
            Damage = Damage,
            CustomName = CustomName,
            ExtraNbt = ExtraNbt?.Clone() as NbtCompound
        };
        foreach (var l in Lore) copy.Lore.Add(l);
        foreach (var e in Enchantments) copy.Enchantments.Add(new EnchantmentEntry(e.Id, e.Name, e.Level));
        return copy;
    }

    public static ItemStack FromNbt(NbtCompound tag, SlotLocation location, byte defaultSlot = 0)
    {
        var item = new ItemStack(defaultSlot, location);

        if (tag.ContainsKey("Slot"))
            item.Slot = tag.GetByte("Slot");

        item.Id = tag.GetString("Name");
        if (string.IsNullOrEmpty(item.Id) && tag.ContainsKey("id"))
        {
            item.Id = tag.GetString("id");
        }

        item.Count = tag.GetByte("Count", 1);

        // Read Damage from root tag or inner tag (Bedrock & Java compatibility)
        int dmg = -1;
        if (tag.ContainsKey("Damage")) dmg = tag.GetNumeric("Damage", -1);
        if (dmg < 0 && tag.ContainsKey("damage")) dmg = tag.GetNumeric("damage", -1);
        if (dmg < 0 && tag.ContainsKey("Aux")) dmg = tag.GetNumeric("Aux", -1);

        var innerTag = tag.GetCompound("tag");
        if (dmg <= 0 && innerTag != null)
        {
            if (innerTag.ContainsKey("Damage")) dmg = innerTag.GetNumeric("Damage", 0);
            else if (innerTag.ContainsKey("damage")) dmg = innerTag.GetNumeric("damage", 0);
        }

        item.Damage = (short)Math.Max(0, dmg);

        if (innerTag != null)
        {
            item.ExtraNbt = innerTag.Clone() as NbtCompound;

            // Enchantments
            var enchList = innerTag.GetList("ench");
            if (enchList != null)
            {
                foreach (var enchTag in enchList.OfType<NbtCompound>())
                {
                    var id = enchTag.GetShort("id");
                    var lvl = enchTag.GetShort("lvl");
                    var name = BedrockEnchantments.GetName(id);
                    item.Enchantments.Add(new EnchantmentEntry(id, name, lvl));
                }
            }

            // Display Name & Lore
            var displayTag = innerTag.GetCompound("display");
            if (displayTag != null)
            {
                var customName = displayTag.GetString("Name");
                if (!string.IsNullOrWhiteSpace(customName))
                {
                    item.CustomName = customName;
                }

                var loreList = displayTag.GetList("Lore");
                if (loreList != null)
                {
                    foreach (var loreItem in loreList.OfType<NbtString>())
                    {
                        item.Lore.Add(loreItem.Value);
                    }
                }
            }
        }

        return item;
    }

    public NbtCompound ToNbt()
    {
        var tag = new NbtCompound();
        tag.SetByte("Slot", Slot);
        tag.SetString("Name", Id);
        tag.SetByte("Count", Count);
        tag.SetShort("Damage", Damage);
        tag.SetByte("WasPickedUp", 0);

        // Build or reuse inner tag
        NbtCompound innerTag = ExtraNbt?.Clone() as NbtCompound ?? new NbtCompound("tag");

        // Synchronize damage with inner tag so Bedrock engine never reads old damage
        if (Damage == 0)
        {
            innerTag.Remove("Damage");
            innerTag.Remove("damage");
            innerTag.Remove("Aux");
            innerTag.Remove("aux");
        }
        else
        {
            if (innerTag.ContainsKey("Damage")) innerTag.SetShort("Damage", Damage);
            if (innerTag.ContainsKey("damage")) innerTag.SetShort("damage", Damage);
        }

        // Update Enchantments
        if (Enchantments.Count > 0)
        {
            var enchList = new NbtList("ench", NbtTagType.Compound);
            foreach (var ench in Enchantments)
            {
                var entry = new NbtCompound();
                entry.SetShort("id", ench.Id);
                entry.SetShort("lvl", ench.Level);
                enchList.Add(entry);
            }
            innerTag.Set(enchList);
        }
        else
        {
            innerTag.Remove("ench");
        }

        // Update Display (Custom Name & Lore)
        // If CustomName is empty, do NOT write any empty display.Name tag!
        if (!string.IsNullOrWhiteSpace(CustomName) || Lore.Count > 0)
        {
            var displayTag = innerTag.GetCompound("display") ?? new NbtCompound("display");
            if (!string.IsNullOrWhiteSpace(CustomName))
            {
                displayTag.SetString("Name", CustomName.Trim());
            }
            else
            {
                displayTag.Remove("Name");
            }

            if (Lore.Count > 0)
            {
                var loreList = new NbtList("Lore", NbtTagType.String);
                foreach (var line in Lore)
                {
                    loreList.Add(new NbtString(string.Empty, line));
                }
                displayTag.Set(loreList);
            }
            else
            {
                displayTag.Remove("Lore");
            }

            if (displayTag.Count > 0)
            {
                innerTag.Set(displayTag);
            }
            else
            {
                innerTag.Remove("display");
            }
        }
        else
        {
            innerTag.Remove("display");
        }

        if (innerTag.Count > 0)
        {
            tag.Set("tag", innerTag);
        }
        else
        {
            tag.Remove("tag");
        }

        return tag;
    }
}
