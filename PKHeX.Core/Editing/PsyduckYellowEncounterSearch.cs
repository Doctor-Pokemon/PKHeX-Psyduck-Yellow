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
        if (!pk.IsNicknamed && pk.Species < PsyduckYellowExpanded.SpeciesNames.Length)
            pk.Nickname = PsyduckYellowExpanded.SpeciesNames[pk.Species];
    }
}
