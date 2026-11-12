using System.Collections.Generic;
using System;
namespace PKHeX.Core;

/// <summary>Opt-in, non-serialized rules for the released Psyduck Yellow 0029 ROM.</summary>
public static partial class PsyduckYellow0029
{
    public const string Name = "Psyduck Yellow prototype 0029";
    public const GameVersion ProfileVersion = GameVersion.PY;
    public static readonly PersonalTable4 Personal = CreatePersonalTable();
    private static PersonalTable4 CreatePersonalTable()
    {
        var original = PersonalTable.HGSS;
        var bytes = new byte[original.Count * PersonalInfo4.SIZE];
        for (int i = 0; i < original.Count; i++)
            original[i].Write().CopyTo(bytes, i * PersonalInfo4.SIZE);
        var table = new PersonalTable4(bytes);
        table[55].Type2 = (byte)MoveType.Psychic; // Verified 0029 Golduck: Water/Psychic.
        return table;
    }
    public static int GetHiddenAbility(ushort species) => species switch
    {
        54 or 55 => 33, 133 => 107, 134 => 93, 135 => 95, 136 => 62,
        197 => 39, 470 => 34, 471 => 115, 175 or 176 or 468 => 105, _ => 0,
    };
    public static bool IsEnabled(PKM pk) => pk is PK4 { IsPsyduckYellow0029: true, IsExpandedEngine: false };
    public static string[] GetItemNames(string[] original)
    {
        var names = (string[])original.Clone();
        names[114] = "Shiny Charm";
        names[121] = "Friendship Charm";
        names[115] = "TM34 Bide";
        names[116] = "TM12 Water Gun";
        names[117] = "TM01 Mega Punch";
        names[118] = "TM04 Whirlwind";
        names[119] = "TM11 BubbleBeam";
        names[120] = "TM45 Thunder Wave";
        names[123] = "TM19 Seismic Toss";
        return names;
    }
    public static readonly EncounterStatic4[] Starters =
    [
        CreateStarter(54), CreateStarter(133), CreateStarter(175),
    ];
    /// <summary>ROM 0233 Kanto starter gifts (Bulbasaur in Cerulean, Charmander on Route 24, Squirtle in Vermilion): level 10, met in those towns; only the expanded profile uses them.</summary>
    public static readonly EncounterStatic4[] KantoGifts =
    [
        CreateKantoGift(1, 141), CreateKantoGift(4, 172), CreateKantoGift(7, 143),
    ];
    private static EncounterStatic4 CreateKantoGift(ushort species, byte location) => new(GameVersion.HG)
    {
        Species = species, Level = 10, Location = location, FixedBall = Ball.Poke,
        GroundTile = GroundTileAllowed.Building | GroundTileAllowed.Max_Pt, IsPsyduckYellowStarter = true,
    };
    /// <summary>ROM 0245+ legendary encounters (expanded profile only). Themed spots: species and met location (the map's location id); the daily Embedded Tower pick and the three orb chambers: location 232.
    /// Every one is a wild battle at level 60 + 5 x Johto badges (60..100 in steps of 5), caught in any ball, no fateful flag.</summary>
    public static readonly EncounterStatic4[] Legendaries = CreateLegendaries();
    private static EncounterStatic4[] CreateLegendaries()
    {
        (ushort Species, byte Location)[] spots =
        [
            ((ushort)Species.Celebi, 214), ((ushort)Species.Regirock, 210), ((ushort)Species.Regice, 217), ((ushort)Species.Registeel, 216),
            ((ushort)Species.Uxie, 209), ((ushort)Species.Mesprit, 135), ((ushort)Species.Azelf, 210),
        ];
        ushort[] tower =
        [
            (ushort)Species.Kyogre, (ushort)Species.Groudon, (ushort)Species.Rayquaza, // the orb chambers
            (ushort)Species.Mew, (ushort)Species.Jirachi, (ushort)Species.Deoxys, (ushort)Species.Heatran, (ushort)Species.Regigigas, (ushort)Species.Cresselia,
            (ushort)Species.Phione, (ushort)Species.Manaphy, (ushort)Species.Darkrai, (ushort)Species.Shaymin, (ushort)Species.Arceus, // daily pick (Gen 4 part of the pool)
        ];
        var list = new List<EncounterStatic4>();
        foreach (var (species, location) in spots)
            AddLevels(list, species, location);
        foreach (var species in tower)
            AddLevels(list, species, 232);
        return [.. list];
    }
    private static void AddLevels(List<EncounterStatic4> list, ushort species, byte location)
    {
        for (byte level = 60; level <= 100; level += 5)
        {
            list.Add(new EncounterStatic4(GameVersion.HG)
            {
                Species = species, Level = level, Location = location, IsPsyduckYellowLegendary = true,
                GroundTile = GroundTileAllowed.None | GroundTileAllowed.Sand | GroundTileAllowed.Grass | GroundTileAllowed.Rock | GroundTileAllowed.Cave | GroundTileAllowed.Water
                           | GroundTileAllowed.Snow | GroundTileAllowed.Ice | GroundTileAllowed.Building | GroundTileAllowed.Marsh | GroundTileAllowed.Max_Pt,
            });
        }
    }
    private static EncounterStatic4 CreateStarter(ushort species) => new(GameVersion.HG)
    {
        Species = species, Level = 5, Location = 138, FixedBall = Ball.Poke,
        Gender = species switch { 54 => 0, 133 => 1, _ => FixedGenderUtil.GenderRandom },
        GroundTile = GroundTileAllowed.Building | GroundTileAllowed.Max_Pt, IsPsyduckYellowStarter = true, // gifted indoors; older builds wrote "Unspecific"
    };
}
