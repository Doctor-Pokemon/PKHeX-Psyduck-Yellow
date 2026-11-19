using System;
using System.Collections.Generic;
namespace PKHeX.Core;
public static partial class PsyduckYellowExpanded
{
    private static readonly Lazy<PersonalTable4> PersonalData = new(CreatePersonalTable);
    public static PersonalTable4 Personal => PersonalData.Value;
    public static ushort MaxSpecies => (ushort)(SpeciesNames.Length - 1);
    public static ushort MaxMove => (ushort)(MoveNames.Length - 1);
    public static ushort MaxAbility => (ushort)(AbilityNames.Length - 1);
    public static ushort MaxItem => (ushort)(ItemNames.Length - 1);
    /// <summary>Every item of the game's name table that no pocket lists. Their ROM item records are empty (items of later generations, the game's special held items, key-item-like objects ...), so the game's own pockets do not know them, but the owner wants EVERY item of the ROM to be injectable into a save, usable or not (2026-11-19). Names that are empty, "-----" or "???" are not items and stay out.</summary>
    public static ReadOnlySpan<ushort> ExtraItems => ExtraItemData.Value;
    private static readonly Lazy<ushort[]> ExtraItemData = new(GetExtraItems);
    private static ushort[] GetExtraItems()
    {
        var listed = new HashSet<ushort>();
        for (int i = 0; i < ItemPockets.Length; i++) foreach (var id in ItemPockets[i]) listed.Add(id);
        var result = new List<ushort>();
        for (int id = 1; id < ItemNames.Length; id++)
        {
            var name = ItemNames[id];
            if (name.Length == 0 || name == "-----" || name == "???") continue;
            if (!listed.Contains((ushort)id)) result.Add((ushort)id);
        }
        return [.. result];
    }
    private static readonly Lazy<ushort[]> HeldItemData = new(GetHeldItems);
    public static ReadOnlySpan<ushort> HeldItems => HeldItemData.Value;
    private static ushort[] GetHeldItems()
    {
        var result = new SortedSet<ushort>();
        for (int i = 0; i < 7; i++) foreach (var id in ItemPockets[i]) result.Add(id);
        // Some of the game's held items have an empty record in the ROM's item data (the engine recognises them by id), so they are in no pocket: add them by name. Held ids 776-806, 835-836, 921-926, 947 and the bag copies 807-834, 927-932 (the engine swaps the two on give/take, so either id may be seen on a Pokemon).
        for (int id = 0; id < ItemNames.Length; id++)
        {
            bool z = id is (>= 776 and <= 836) or (>= 921 and <= 932) or 947;
            if (z && ItemNames[id].EndsWith(" Z", StringComparison.Ordinal))
                result.Add((ushort)id);
        }
        foreach (var id in ExtraItems) result.Add(id); // every named item of the ROM may be held (injection, usable or not)
        return [.. result];
    }
    private static PersonalTable4 CreatePersonalTable()
    {
        var data = Util.GetBinaryResource("personal_psyduck_expanded");
        var table = new PersonalTable4(data, (ushort)(data.Length / PersonalInfo4.SIZE - 1), FormMap);
        for (int i = 0; i < table.Count; i++)
        {
            var p = table[i]; p.EngineAbility1 = Ability1[i]; p.EngineAbility2 = Ability2[i]; p.EngineHiddenAbility = HiddenAbility[i];
        }
        return table;
    }
    public static int GetFormCount(ushort species)
    {
        int count = 1;
        for (int f = 1; f <= 31; f++) { int at = species * 32 + f - 1; if ((uint)at < FormMap.Length && FormMap[at] is > 0 and var index && index < SpeciesNames.Length) count = f + 1; }
        return count;
    }
    public static string[] GetFormNames(ushort species)
    {
        var names = new string[GetFormCount(species)]; names[0] = "Base";
        for (int f = 1; f < names.Length; f++) { var index = FormMap[species * 32 + f - 1]; names[f] = index > 0 && index < SpeciesNames.Length ? $"{f}: {SpeciesNames[index]}" : $"Form {f}"; }
        return names;
    }
    /// <summary>Species of the project without a national sprite: their pictures are b_py{index} / a_py{index} (see CustomSpriteBase).</summary>
    public const ushort CustomSpriteBase = 20000;
    public static readonly string[] CustomSpriteNames = ["Brawleon", "Venomeon", "Duneon", "Zephyreon", "Chryseon", "Strateon", "Wraitheon", "Wyrmeon", "Ferreon", "Stellareon", "Raptozolt", "Dracosaur", "Dunklevish", "Arctosaur"];

    public static ushort GetDisplaySpecies(ushort species)
    {
        if (species <= 493) return species;
        if (species >= SpeciesNames.Length) return 0;
        var custom = Array.FindIndex(CustomSpriteNames, n => string.Equals(n, SpeciesNames[species], StringComparison.OrdinalIgnoreCase));
        if (custom >= 0) return (ushort)(CustomSpriteBase + custom);
        var national = GameInfo.GetStrings("en").specieslist;
        for (ushort i = 1; i < national.Length; i++)
            if (string.Equals(national[i], SpeciesNames[species], StringComparison.OrdinalIgnoreCase)) return i;
        return 0; // Unknown engine forms use the neutral sprite rather than an unrelated Pokemon.
    }
}

