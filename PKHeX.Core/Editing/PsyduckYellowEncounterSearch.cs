using System.Collections.Generic;

namespace PKHeX.Core;

/// <summary>
/// Encounter search for Psyduck Yellow saves: the game's own wild encounters (Kanto, and the post-Hall-of-Fame tables), the Pallet and Kanto starter gifts and the legendary encounters (themed spots, Embedded Tower),
/// including encounters of a species' pre-evolutions (a Scizor search finds Scyther).
/// </summary>
public static class PsyduckYellowEncounterSearch
{
    public static IEnumerable<IEncounterInfo> Search(ushort species)
    {
        var baseSpecies = PsyduckYellowExpanded.GetRetailSpecies(species);
        var tree = EvolutionTree.PsyduckYellow0029;
        // The species itself and its pre-evolutions only (an evolved Pokémon can't turn back).
        var allowed = new HashSet<ushort> { baseSpecies };
        foreach (var (pre, _) in tree.Reverse.GetPreEvolutions(baseSpecies, 0))
            allowed.Add(pre);
        bool Matches(ushort s) => allowed.Contains(s);

        foreach (var starter in PsyduckYellow0029.StartersExpanded)
        {
            if (Matches(starter.Species))
                yield return starter;
        }

        foreach (var gift in PsyduckYellow0029.KantoGifts)
        {
            if (Matches(gift.Species))
                yield return gift;
        }
        foreach (var legend in PsyduckYellow0029.Legendaries)
        {
            if (Matches(legend.Species))
                yield return legend;
        }

        // Wild areas repeat each slot for every time of day and slot number; list each distinct place/level once.
        var seen = new HashSet<(byte Location, SlotType4 Type, ushort Species, byte Min, byte Max)>();
        foreach (var area in Encounters4HGSS.SlotsPY)
        {
            foreach (var slot in area.Slots)
            {
                if (Matches(slot.Species) && seen.Add((area.Location, area.Type, slot.Species, slot.LevelMin, slot.LevelMax)))
                    yield return slot;
            }
        }
    }

    /// <summary>Every encounter of the game (each listed once), for a search with no species chosen.</summary>
    public static IEnumerable<IEncounterInfo> SearchAll()
    {
        var seen = new HashSet<IEncounterInfo>(ReferenceEqualityComparer.Instance);
        var lowest = new HashSet<ushort>();
        for (ushort species = 1; species < PsyduckYellowExpanded.SpeciesNames.Length; species++)
        {
            foreach (var enc in Search(species))
            {
                if (enc.Species == PsyduckYellowExpanded.GetRetailSpecies(species) && seen.Add(enc))
                    yield return enc;
            }
        }
    }

    /// <summary>Turns a Pokémon generated from an encounter into a Psyduck Yellow one: game flags, the starter's own species, the game's name casing.</summary>
    public static void AdaptToGame(PK4 pk, IEncounterInfo source)
    {
        pk.IsExpandedEngine = true;
        pk.IsPsyduckYellow0029 = true;
        if (source is EncounterStatic4 { IsPsyduckYellowStarter: true })
        {
            if (pk.Species == (ushort)Species.Psyduck)
                pk.Species = PsyduckYellowExpanded.StarterPsyduck;
            else if (pk.Species == (ushort)Species.Eevee)
                pk.Species = PsyduckYellowExpanded.StarterEevee;
        }
        if (source is EncounterStatic4 { IsPsyduckYellowLegendary: true } && pk.Species < PsyduckYellowExpanded.Learnsets.Length)
        {
            // The encounter generator knows retail HeartGold's level-up moves; the game's own table differs for these Pokémon.
            var moves = new List<ushort>();
            foreach (var move in PsyduckYellowExpanded.Learnsets[pk.Species].GetMoveRange(pk.CurrentLevel))
            {
                if (move <= Legal.MaxMoveID_4 && !moves.Contains(move))
                    moves.Add(move);
            }
            var last = moves.Count > 4 ? moves.GetRange(moves.Count - 4, 4) : moves;
            pk.SetMoves(last.Count == 4 ? [last[0], last[1], last[2], last[3]] : [.. last, .. new ushort[4 - last.Count]]);
            pk.HealPP();
        }
        if (!pk.IsNicknamed && pk.Species < PsyduckYellowExpanded.SpeciesNames.Length)
            pk.Nickname = PsyduckYellowExpanded.SpeciesNames[pk.Species];
    }
}
