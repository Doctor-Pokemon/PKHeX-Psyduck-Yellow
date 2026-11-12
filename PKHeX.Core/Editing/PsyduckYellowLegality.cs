using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core;

/// <summary>
/// Legality of Psyduck Yellow Pokémon that the retail Generation 4 view cannot describe (species, moves, abilities or items newer than Generation 4).
/// Everything is judged against the game's own tables read from the ROM: encounters, evolutions, level-up / TM / tutor moves, personal data.
/// </summary>
public static class PsyduckYellowLegality
{
    /// <summary>Judge every Psyduck Yellow Pokémon with the game's own tables, even those the retail Generation 4 view could describe (used by the self-consistency tests).</summary>
    public static bool ForceOwnRules { get; set; }

    /// <summary>Eggs and nameless species stay unjudged for now.</summary>
    internal static bool CanAnalyze(PK4 pk) => !pk.IsEgg && pk.Species != 0;

    internal static void Analyze(LegalityAnalysis la, PK4 pk)
    {
        var info = la.Info;
        var species = pk.Species;
        if (species == 0 || species >= PsyduckYellowExpanded.SpeciesNames.Length || string.IsNullOrEmpty(PsyduckYellowExpanded.SpeciesNames[species]) || species >= PsyduckYellowExpanded.Personal.MaxSpeciesID + 1)
        {
            la.AddLine(Severity.Invalid, LegalityCheckResultCode.EncConditionBadSpecies, CheckIdentifier.Encounter);
            return;
        }

        // 1. Encounter: a place the game creates this Pokémon (or an earlier stage of it) at, with the met level and location it has.
        // Several encounters can match (different stages of the same line); keep the one under which the evolution and every move work.
        var best = FindBest(pk);
        var path = best.Path;
        var id = CheckIdentifier.Encounter;
        if (best.Encounter is null)
        {
            la.AddLine(Severity.Invalid, LegalityCheckResultCode.EncInvalid, id);
        }
        else
        {
            info.EncounterMatch = best.Encounter;
            la.AddLine(Severity.Valid, LegalityCheckResultCode.EncCondition, id);
            if (pk.CurrentLevel < best.MinLevel)
                la.AddLine(Severity.Invalid, LegalityCheckResultCode.EvoInvalid, CheckIdentifier.Evolution);
        }
        if (pk.CurrentLevel < pk.MetLevel || pk.CurrentLevel is 0 or > 100)
            la.AddLine(Severity.Invalid, LegalityCheckResultCode.LevelMetBelow, CheckIdentifier.Level);

        // 2. Moves
        var moveResults = GetMoveResults(pk, path);
        for (int i = 0; i < moveResults.Length; i++)
            info.Moves[i] = moveResults[i];
        for (int i = 0; i < info.Relearn.Length; i++)
            info.Relearn[i] = MoveResult.Empty;

        // 3. Ability: the slot the Pokémon's PID selects (or the hidden slot) must hold this species' ability
        var pi = PsyduckYellowExpanded.Personal[species];
        var expected = pk.AbilityNumber switch
        {
            4 => pi.EngineHiddenAbility,
            2 => pi.EngineAbility2,
            _ => pi.EngineAbility1,
        };
        if (pk.Ability != expected && !(pk.Ability == pi.EngineAbility1 || pk.Ability == pi.EngineAbility2 || pk.Ability == pi.EngineHiddenAbility))
            la.AddLine(Severity.Invalid, LegalityCheckResultCode.AbilityMismatch, CheckIdentifier.Ability);
        else if (pk.Ability != expected)
            la.AddLine(Severity.Invalid, LegalityCheckResultCode.AbilityMismatchPID, CheckIdentifier.Ability);

        // 4. Gender against the species' ratio
        var ratio = pi.Gender;
        if ((ratio == PersonalInfo.RatioMagicGenderless && pk.Gender != 2)
            || (ratio == PersonalInfo.RatioMagicFemale && pk.Gender != 1)
            || (ratio == PersonalInfo.RatioMagicMale && pk.Gender != 0)
            || (pi.IsDualGender && pk.Gender > 1))
            la.AddLine(Severity.Invalid, LegalityCheckResultCode.EncGiftPIDMismatch, CheckIdentifier.PID);

        // 5. Held item must exist in the game
        if (pk.HeldItem >= PsyduckYellowExpanded.ItemNames.Length)
            la.AddLine(Severity.Invalid, LegalityCheckResultCode.ItemUnreleased, CheckIdentifier.HeldItem);
    }

    private static (IEncounterable? Encounter, List<ushort> Path, int MinLevel) FindBest(PK4 pk)
    {
        var species = pk.Species;
        (IEncounterable? Encounter, List<ushort> Path, int MinLevel) best = (null, [species], 0);
        int bestScore = int.MaxValue;
        foreach (var enc in PsyduckYellowEncounterSearch.Search(species))
        {
            if (enc is not IEncounterable e || !IsMatch(enc, pk))
                continue;
            if (!PsyduckYellowExpanded.TryGetPath(enc.Species, species, out var path, out var min))
                continue;
            var score = (pk.CurrentLevel >= min ? 0 : 100) + GetMoveResults(pk, path).Count(r => !r.Valid);
            if (score >= bestScore)
                continue;
            best = (e, path, min);
            bestScore = score;
            if (score == 0)
                break;
        }
        return best;
    }

    private static bool IsMatch(IEncounterInfo enc, PK4 pk)
    {
        if (pk.IsEgg)
            return false;
        return enc switch
        {
            EncounterSlot4 slot => slot.Location == pk.MetLocation && pk.MetLevel >= slot.LevelMin && pk.MetLevel <= slot.LevelMax,
            EncounterStatic4 st => st.Location == pk.MetLocation && st.Level == pk.MetLevel,
            _ => false,
        };
    }

    private static MoveResult[] GetMoveResults(PK4 pk, List<ushort> path)
    {
        var results = new MoveResult[4];
        Span<ushort> moves = [pk.Move1, pk.Move2, pk.Move3, pk.Move4];
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            if (move == 0)
                results[i] = i == 0 ? MoveResult.EmptyInvalid : MoveResult.Empty;
            else if (moves[..i].Contains(move))
                results[i] = MoveResult.Duplicate;
            else
                results[i] = move < PsyduckYellowExpanded.MoveNames.Length ? FindSource(pk, path, move) : MoveResult.Unobtainable();
        }
        return results;
    }

    private static MoveResult FindSource(PK4 pk, List<ushort> path, ushort move)
    {
        foreach (var s in path)
        {
            if (s >= PsyduckYellowExpanded.Learnsets.Length)
                continue;
            var learnset = PsyduckYellowExpanded.Learnsets[s];
            if (System.MemoryExtensions.Contains(learnset.GetMoveRange(pk.CurrentLevel), move)) // a move can be listed at several levels: any of them at or below the level counts
            {
                var level = learnset.TryGetLevelLearnMove(move, out var first) ? first : (byte)1;
                return new MoveResult(new MoveLearnInfo(level <= 1 ? LearnMethod.Initial : LearnMethod.LevelUp, LearnEnvironment.HGSS, level), 0, EntityContext.Gen4);
            }
        }
        foreach (var s in path)
        {
            if (PsyduckYellowExpanded.CanLearnMachine(s, move))
                return new MoveResult(LearnMethod.TMHM, LearnEnvironment.HGSS);
            if (PsyduckYellowExpanded.CanLearnTutor(s, move))
                return new MoveResult(LearnMethod.Tutor, LearnEnvironment.HGSS);
        }
        return MoveResult.Unobtainable();
    }
}
