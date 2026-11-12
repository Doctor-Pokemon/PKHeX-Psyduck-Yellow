using System;
using System.Buffers.Binary;
namespace PKHeX.Core;

public static partial class PsyduckYellowExpanded
{
    private static readonly Lazy<ushort[][]> EggMoveData = new(LoadEggMoves);

    /// <summary>The egg moves of a species, from the game's own table (only base species have any).</summary>
    public static ReadOnlySpan<ushort> GetEggMoves(ushort species) => species < EggMoveData.Value.Length ? EggMoveData.Value[species] : [];

    private static ushort[][] LoadEggMoves()
    {
        var data = Util.GetBinaryResource("egg_psyduck_expanded");
        var count = BinaryPrimitives.ReadUInt16LittleEndian(data);
        var result = new ushort[count][];
        int pos = 2;
        for (int i = 0; i < count; i++)
        {
            var moves = new ushort[data[pos++]];
            for (int m = 0; m < moves.Length; m++, pos += 2)
                moves[m] = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(pos));
            result[i] = moves;
        }
        return result;
    }
}
