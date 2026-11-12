using System;
using System.Collections.Generic;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core;

/// <summary>The expanded game's own evolution table (hg-engine data read from the ROM), for legality and the encounter search.</summary>
public static partial class PsyduckYellowExpanded
{
    public readonly record struct EvolutionEdge(byte Method, ushort Parameter, ushort Target, byte Form)
    {
        /// <summary>The level the evolution needs, 0 if it needs none (items, trades, happiness) or the level is not part of the rule.</summary>
        public int MinLevel => Method is 4 or (>= 8 and <= 14) or (>= 22 and <= 30) or 32 or 34 or 35 ? Parameter : 0;
    }

    private static readonly Lazy<EvolutionEdge[][]> EvolutionData = new(LoadEvolutions);
    private static readonly Lazy<ushort[][]> PreEvolutionData = new(() => BuildReverse(EvolutionData.Value));

    /// <summary>Evolutions of an internal species (forms are part of the target).</summary>
    public static EvolutionEdge[] GetEvolutions(ushort species) => species < EvolutionData.Value.Length ? EvolutionData.Value[species] : [];

    /// <summary>The species that evolve directly into the given one.</summary>
    public static ushort[] GetDirectPreEvolutions(ushort species) => species < PreEvolutionData.Value.Length ? PreEvolutionData.Value[species] : [];

    private static EvolutionEdge[][] LoadEvolutions()
    {
        // u16 species count; per species: u8 n, n * (u8 method, u16 parameter, u16 target); the target holds the species in its low 11 bits and the form above
        var data = Util.GetBinaryResource("evolution_psyduck_expanded");
        var count = ReadUInt16LittleEndian(data);
        var result = new EvolutionEdge[count][];
        int pos = 2;
        for (int i = 0; i < count; i++)
        {
            var edges = new EvolutionEdge[data[pos++]];
            for (int e = 0; e < edges.Length; e++, pos += 5)
            {
                var target = ReadUInt16LittleEndian(data.AsSpan(pos + 3));
                var species = (ushort)(target & 0x7FF);
                var form = (byte)(target >> 11);
                if (form != 0)
                {
                    // the Pokémon of a form is stored under its own internal species id
                    var at = species * 32 + form - 1;
                    if ((uint)at < FormMap.Length && FormMap[at] is > 0 and var internalSpecies && internalSpecies < count)
                        species = internalSpecies;
                }
                edges[e] = new EvolutionEdge(data[pos], ReadUInt16LittleEndian(data.AsSpan(pos + 1)), species, form);
            }
            result[i] = edges;
        }
        AddFormEvolutions(result);
        return result;
    }

    /// <summary>A form kept under its own internal species id (e.g. a regional form) that nothing evolves into inherits the ways its base species is reached, so its pre-evolutions count for it too.</summary>
    private static void AddFormEvolutions(EvolutionEdge[][] table)
    {
        var hasPre = new bool[table.Length];
        foreach (var edges in table)
            foreach (var edge in edges)
                if (edge.Target < hasPre.Length)
                    hasPre[edge.Target] = true;
        var extra = new List<(ushort From, EvolutionEdge Edge)>();
        for (int b = 1; b < table.Length; b++)
        {
            for (int f = 1; f < 32; f++)
            {
                var at = b * 32 + f - 1;
                if ((uint)at >= FormMap.Length || FormMap[at] == 0)
                    continue;
                var form = FormMap[at];
                if (form >= table.Length || form == b || hasPre[form])
                    continue;
                for (ushort s = 1; s < table.Length; s++)
                    foreach (var edge in table[s])
                        if (edge.Target == b)
                            extra.Add((s, edge with { Target = form, Form = (byte)f }));
            }
        }
        foreach (var group in extra.GroupBy(x => x.From))
            table[group.Key] = [.. table[group.Key], .. group.Select(x => x.Edge)];
    }

    private static ushort[][] BuildReverse(EvolutionEdge[][] forward)
    {
        var lists = new List<ushort>[forward.Length];
        for (ushort s = 0; s < forward.Length; s++)
        {
            foreach (var edge in forward[s])
            {
                if (edge.Target >= lists.Length || edge.Target == s)
                    continue;
                (lists[edge.Target] ??= []).Add(s);
            }
        }
        return Array.ConvertAll(lists, l => l?.ToArray() ?? []);
    }

    /// <summary>Every species from which <paramref name="species"/> can come (itself first, then pre-evolutions).</summary>
    public static List<ushort> GetLine(ushort species)
    {
        var result = new List<ushort> { species };
        for (int i = 0; i < result.Count; i++)
        {
            foreach (var pre in GetDirectPreEvolutions(result[i]))
            {
                if (!result.Contains(pre))
                    result.Add(pre);
            }
        }
        return result;
    }

    /// <summary>Finds an evolution path from <paramref name="from"/> to <paramref name="to"/>. Returns the species on the way (both ends included) and the highest level any step needs.</summary>
    public static bool TryGetPath(ushort from, ushort to, out List<ushort> path, out int minLevel)
    {
        path = [from];
        minLevel = 0;
        if (from == to)
            return true;
        // breadth first over the (small) evolution graph
        var previous = new Dictionary<ushort, (ushort Species, int Level)> { [from] = (from, 0) };
        var queue = new Queue<ushort>();
        queue.Enqueue(from);
        while (queue.Count != 0)
        {
            var s = queue.Dequeue();
            foreach (var edge in GetEvolutions(s))
            {
                if (previous.ContainsKey(edge.Target))
                    continue;
                previous[edge.Target] = (s, edge.MinLevel);
                if (edge.Target == to)
                {
                    path = [];
                    var at = to;
                    while (at != from)
                    {
                        path.Insert(0, at);
                        minLevel = Math.Max(minLevel, previous[at].Level);
                        at = previous[at].Species;
                    }
                    path.Insert(0, from);
                    return true;
                }
                queue.Enqueue(edge.Target);
            }
        }
        return false;
    }
}
