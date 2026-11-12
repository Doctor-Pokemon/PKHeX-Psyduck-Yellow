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
    /// <summary>The Pallet starter gifts of the expanded game: Psyduck and Eevee have a random gender there (saves of the same build hold both a male and a female starter); the 0029 profile keeps its fixed genders.</summary>
    public static readonly EncounterStatic4[] StartersExpanded =
    [
        CreateStarter(54, true), CreateStarter(133, true), CreateStarter(175),
    ];
    /// <summary>ROM 0233 Kanto starter gifts (Bulbasaur in Cerulean, Charmander on Route 24, Squirtle in Vermilion): level 10, met in those towns; only the expanded profile uses them.</summary>
    public static readonly EncounterStatic4[] KantoGifts =
    [
        CreateKantoGift(1, 141), CreateKantoGift(4, 172), CreateKantoGift(7, 143),
        // Scripted gifts found by scanning the game's scripts (ROM 0256): the Dojo's Hitmonlee / Hitmonchan, a Tentacool, the level 5 Treecko and level 1 Torchic gifts, the level 1 Palkia / Giratina
        CreateKantoGift(106, 148, 30), CreateKantoGift(107, 148, 30), CreateKantoGift(72, 130, 15),
        CreateKantoGift(252, 148, 5), CreateKantoGift(255, 153, 1), CreateKantoGift(484, 231, 1), CreateKantoGift(487, 231, 1), CreateKantoGift(483, 231, 1),
        CreateKantoGift(152, 126, 5), CreateKantoGift(155, 126, 5), CreateKantoGift(158, 126, 5), // the Johto starter gifts
        CreateStaticBattle(143, 160, 50), // Snorlax, level 50
        CreateStaticBattle(144, 203, 50), CreateStaticBattle(145, 158, 50), CreateStaticBattle(146, 221, 50), CreateStaticBattle(150, 199, 70), // Articuno, Zapdos, Moltres (Victory Road) and Mewtwo at Yellow's levels
        CreateKantoGift(129, 151, 5), // the Magikarp salesman in the Mt. Moon Pokémon Center: level 5, bought (any Gyarados line Pokémon can come from it)
    ];
    private static EncounterStatic4 CreateStaticBattle(ushort species, byte location, byte level) => new(GameVersion.HG)
    {
        Species = species, Level = level, Location = location, IsPsyduckYellowLegendary = true,
        GroundTile = GroundTileAllowed.None | GroundTileAllowed.Sand | GroundTileAllowed.Grass | GroundTileAllowed.Rock | GroundTileAllowed.Cave | GroundTileAllowed.Water
                   | GroundTileAllowed.Snow | GroundTileAllowed.Ice | GroundTileAllowed.Building | GroundTileAllowed.Marsh | GroundTileAllowed.Max_Pt,
    };
    private static EncounterStatic4 CreateKantoGift(ushort species, byte location, byte level = 10) => new(GameVersion.HG)
    {
        Species = species, Level = level, Location = location, FixedBall = Ball.Poke,
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
            // static battles with the same scaled level (60 + 5 per Johto badge), found by scanning the game's scripts: Suicune, Moltres, the Rocket HQ Electrodes, Sudowoodo, Lapras, the Lake of Rage Gyarados
            ((ushort)Species.Suicune, 206), ((ushort)Species.Moltres, 219), ((ushort)Species.Electrode, 213), ((ushort)Species.Sudowoodo, 184),
            ((ushort)Species.Lapras, 210), ((ushort)Species.Gyarados, 135), ((ushort)Species.Lugia, 218), ((ushort)Species.HoOh, 205),
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
    private static EncounterStatic4 CreateStarter(ushort species, bool randomGender = false) => new(GameVersion.HG)
    {
        Species = species, Level = 5, Location = 138, FixedBall = Ball.Poke,
        Gender = randomGender ? FixedGenderUtil.GenderRandom : (byte)(species switch { 54 => 0, 133 => 1, _ => FixedGenderUtil.GenderRandom }),
        GroundTile = GroundTileAllowed.Building | GroundTileAllowed.Max_Pt, IsPsyduckYellowStarter = true, // gifted indoors; older builds wrote "Unspecific"
    };
}
