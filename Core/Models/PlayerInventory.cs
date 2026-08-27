using System.Collections.ObjectModel;
using System.Linq;
using BedrockInventoryEditor.Core.Nbt;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.Core.Models;

public partial class PlayerInventory : ObservableObject
{
    public ObservableCollection<ItemStack> Armor { get; } = [];
    public ObservableCollection<ItemStack> Offhand { get; } = [];
    public ObservableCollection<ItemStack> Hotbar { get; } = [];
    public ObservableCollection<ItemStack> MainBag { get; } = [];
    public ObservableCollection<ItemStack> EnderChest { get; } = [];

    public PlayerInventory()
    {
        InitializeSlotsOnce();
    }

    private void InitializeSlotsOnce()
    {
        Armor.Clear();
        Armor.Add(new ItemStack(0, SlotLocation.ArmorHelmet));
        Armor.Add(new ItemStack(1, SlotLocation.ArmorChestplate));
        Armor.Add(new ItemStack(2, SlotLocation.ArmorLeggings));
        Armor.Add(new ItemStack(3, SlotLocation.ArmorBoots));

        Offhand.Clear();
        Offhand.Add(new ItemStack(0, SlotLocation.Offhand));

        Hotbar.Clear();
        for (byte i = 0; i < 9; i++)
        {
            Hotbar.Add(new ItemStack(i, SlotLocation.Hotbar));
        }

        MainBag.Clear();
        for (byte i = 9; i <= 35; i++)
        {
            MainBag.Add(new ItemStack(i, SlotLocation.MainBag));
        }

        EnderChest.Clear();
        for (byte i = 0; i < 27; i++)
        {
            EnderChest.Add(new ItemStack(i, SlotLocation.EnderChest));
        }
    }

    public void ResetToEmpty()
    {
        foreach (var item in Armor) item.Clear();
        foreach (var item in Offhand) item.Clear();
        foreach (var item in Hotbar) item.Clear();
        foreach (var item in MainBag) item.Clear();
        foreach (var item in EnderChest) item.Clear();
    }

    public void LoadFromPlayerNbt(NbtCompound playerCompound)
    {
        // Reset existing instances without destroying object references
        ResetToEmpty();

        // 1. Load Inventory & Hotbar (stored together in 'Inventory' tag)
        var invList = playerCompound.GetList("Inventory");
        if (invList != null)
        {
            foreach (var itemComp in invList.OfType<NbtCompound>())
            {
                var slot = itemComp.GetByte("Slot");
                if (slot < 9)
                {
                    var hotbarItem = Hotbar.FirstOrDefault(h => h.Slot == slot);
                    if (hotbarItem != null)
                    {
                        var loaded = ItemStack.FromNbt(itemComp, SlotLocation.Hotbar, slot);
                        CopyItemProperties(loaded, hotbarItem);
                    }
                }
                else if (slot <= 35)
                {
                    var bagItem = MainBag.FirstOrDefault(b => b.Slot == slot);
                    if (bagItem != null)
                    {
                        var loaded = ItemStack.FromNbt(itemComp, SlotLocation.MainBag, slot);
                        CopyItemProperties(loaded, bagItem);
                    }
                }
            }
        }

        // 2. Load Armor (stored in 'Armor' tag, slots 0 to 3)
        // In Bedrock: Armor list has 4 elements: 0 = Helmet, 1 = Chestplate, 2 = Leggings, 3 = Boots
        var armorList = playerCompound.GetList("Armor");
        if (armorList != null)
        {
            byte armorIndex = 0;
            foreach (var itemComp in armorList.OfType<NbtCompound>())
            {
                var slot = itemComp.ContainsKey("Slot") ? itemComp.GetByte("Slot") : armorIndex;
                if (slot < Armor.Count)
                {
                    var loaded = ItemStack.FromNbt(itemComp, Armor[slot].Location, slot);
                    CopyItemProperties(loaded, Armor[slot]);
                }
                armorIndex++;
            }
        }

        // 3. Load Offhand (stored in 'Offhand' tag)
        var offhandList = playerCompound.GetList("Offhand");
        if (offhandList != null && offhandList.Count > 0 && offhandList[0] is NbtCompound offComp)
        {
            var loaded = ItemStack.FromNbt(offComp, SlotLocation.Offhand, 0);
            CopyItemProperties(loaded, Offhand[0]);
        }

        // 4. Load Ender Chest (stored in 'EnderChestInventory' tag)
        var enderList = playerCompound.GetList("EnderChestInventory");
        if (enderList != null)
        {
            foreach (var itemComp in enderList.OfType<NbtCompound>())
            {
                var slot = itemComp.GetByte("Slot");
                if (slot < EnderChest.Count)
                {
                    var target = EnderChest.FirstOrDefault(e => e.Slot == slot);
                    if (target != null)
                    {
                        var loaded = ItemStack.FromNbt(itemComp, SlotLocation.EnderChest, slot);
                        CopyItemProperties(loaded, target);
                    }
                }
            }
        }
    }

    public void SaveToPlayerNbt(NbtCompound playerCompound)
    {
        // 1. Save Inventory (Hotbar + MainBag)
        var invList = new NbtList("Inventory", NbtTagType.Compound);
        foreach (var item in Hotbar.Where(i => !i.IsEmpty))
        {
            invList.Add(item.ToNbt());
        }
        foreach (var item in MainBag.Where(i => !i.IsEmpty))
        {
            invList.Add(item.ToNbt());
        }
        playerCompound.Set(invList);

        // 2. Save Armor (4 items, even if empty)
        var armorList = new NbtList("Armor", NbtTagType.Compound);
        foreach (var armorItem in Armor)
        {
            if (!armorItem.IsEmpty)
            {
                armorList.Add(armorItem.ToNbt());
            }
            else
            {
                var empty = new NbtCompound();
                empty.SetByte("Slot", armorItem.Slot);
                empty.SetString("Name", "");
                empty.SetByte("Count", 0);
                empty.SetShort("Damage", 0);
                armorList.Add(empty);
            }
        }
        playerCompound.Set(armorList);

        // 3. Save Offhand
        var offhandList = new NbtList("Offhand", NbtTagType.Compound);
        if (!Offhand[0].IsEmpty)
        {
            offhandList.Add(Offhand[0].ToNbt());
        }
        else
        {
            var empty = new NbtCompound();
            empty.SetByte("Slot", 0);
            empty.SetString("Name", "");
            empty.SetByte("Count", 0);
            empty.SetShort("Damage", 0);
            offhandList.Add(empty);
        }
        playerCompound.Set(offhandList);

        // 4. Save Ender Chest
        var enderList = new NbtList("EnderChestInventory", NbtTagType.Compound);
        foreach (var item in EnderChest.Where(i => !i.IsEmpty))
        {
            enderList.Add(item.ToNbt());
        }
        playerCompound.Set(enderList);
    }

    private static void CopyItemProperties(ItemStack source, ItemStack dest)
    {
        dest.Id = source.Id;
        dest.Count = source.Count;
        dest.Damage = source.Damage;
        dest.CustomName = source.CustomName;
        dest.ExtraNbt = source.ExtraNbt;

        dest.Lore.Clear();
        foreach (var l in source.Lore) dest.Lore.Add(l);

        dest.Enchantments.Clear();
        foreach (var e in source.Enchantments) dest.Enchantments.Add(new EnchantmentEntry(e.Id, e.Name, e.Level));
    }
}
