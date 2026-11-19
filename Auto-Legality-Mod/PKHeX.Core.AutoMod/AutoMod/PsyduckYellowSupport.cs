using System;
using System.Linq;
namespace PKHeX.Core.AutoMod;

/// <summary>
/// Psyduck Yellow generation: Pokémon are built from the game's own encounters (the Pallet and Kanto starter gifts, the wild areas including the post-Hall-of-Fame tables, and the legendary spots / Embedded Tower encounters).
/// The starter Psyduck/Eevee line is recognised by its own species IDs, never by guessing from a plain Psyduck or Eevee.
/// </summary>
public static class PsyduckYellowSupport
{
    private const int MaxAttempts = 50;
    private const int MaxMove4 = 467; // Generation 4 move and item id ranges (retail ids still match the game below these)
    private const int MaxItem4 = 536;

    /// <summary>Kept for the shared ALM call sites; Psyduck Yellow saves need no profile flag on the template.</summary>
    public static void AttachProfile(ITrainerInfo trainer, PKM template)
    {
        if (trainer is SAV4PY && template is PK4 pk)
        {
            pk.IsPsyduckYellow0029 = true;
            pk.IsExpandedEngine = true;
        }
    }

    public static bool TryGenerate(ITrainerInfo trainer, PKM template, IBattleTemplate set,
        out PKM result, out LegalizationResult status)
    {
        result = template;
        status = LegalizationResult.Failed;
        if (trainer is not SAV4PY || template is not PK4)
            return false;

        // A request this generator can't honour fails explicitly instead of being silently dropped.
        var species = set.Species;
        // The ALM's tables only know the ordinary species: work on the ordinary-species view of the request, then put the game's own species back.
        if (PsyduckYellowExpanded.GetRetailSpecies(species) != species)
        {
            if (template.Species != species)
                return true; // the request names a species that only exists in this game: it must come from the Pokémon itself
            var view = template.Clone();
            view.Species = PsyduckYellowExpanded.GetRetailSpecies(species);
            set = new RegenTemplate(new ShowdownSet(view), 4);
        }
        var regen = (set as RegenTemplate)?.Regen;
        if (set.Form != 0 || set.Level < 1 || set.Level > 100 || set.CanGigantamax ||
            regen?.Extra is { Alpha: true })
            return true;

        if (regen?.Extra is { Egg: true })
        {
            // "Egg: Yes" = a Pokémon hatched from an egg (the game's shared Day Care breeds). The starter line never comes from an egg.
            if (PsyduckYellowExpanded.IsStarterSpecies(species))
                return true;
            var hatchCriteria = EncounterCriteria.GetCriteria(set, template.PersonalInfo, EncounterMutationUtil.GetSuggested(trainer.Context, set.Level));
            if (TryGenerateHatched(trainer, set, species, hatchCriteria, out var hatched))
            {
                result = hatched;
                status = LegalizationResult.Regenerated;
            }
            return true;
        }

        var isStarterLine = PsyduckYellowExpanded.IsStarterSpecies(species);
        var criteria = EncounterCriteria.GetCriteria(set, template.PersonalInfo, EncounterMutationUtil.GetSuggested(trainer.Context, set.Level));
        foreach (var enc in PsyduckYellowEncounterSearch.Search(species))
        {
            // Wild Psyduck/Eevee are ordinary species; the starter line only ever comes from the Pallet gift.
            var isStarterGift = enc is EncounterStatic4 { IsPsyduckYellowStarter: true };
            var gift = isStarterGift && enc.Species is (ushort)Species.Psyduck or (ushort)Species.Eevee; // only the Pallet Psyduck/Eevee gifts are the starter line (Togepi and the Kanto starters are ordinary species)
            if (gift != isStarterLine && (gift || isStarterLine))
                continue;
            if (enc.LevelMin > set.Level)
                continue;
            for (int i = 0; i < MaxAttempts; i++)
            {
                try
                {
                if (enc.ConvertToPKM(trainer, criteria) is not PK4 pk)
                    break;
                pk.Species = set.Species; // ordinary species while the ALM edits it
                pk.CurrentLevel = (byte)Math.Max(set.Level, pk.MetLevel);
                APILegality.ApplySetDetails(pk, set, trainer, enc, regen ?? RegenSet.Default, criteria);
                PsyduckYellowEncounterSearch.AdaptToGame(pk, enc);
                pk.Species = species;
                pk.Nickname = PsyduckYellowExpanded.SpeciesNames[species];
                pk.HealPP();
                pk.ResetPartyStats();
                pk.RefreshChecksum();
                var la = new LegalityAnalysis(pk);
                if (!la.Valid && TryRepair(pk, la))
                    la = new LegalityAnalysis(pk);
                if (!la.Valid)
                    continue;
                if (set.Shiny != pk.IsShiny && i < MaxAttempts - 1)
                    continue;
                result = pk;
                status = LegalizationResult.Regenerated;
                return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    break; // the ALM's retail name tables cannot name this species (the game's species ids are shifted); the builder below names it from the game's own list
                }
            }
        }
        // No encounter produced it: a species of an egg line comes from an egg, and a species with no source in the game's data at all is still accepted by the analyzer.
        if (!isStarterLine && TryGenerateHatched(trainer, set, species, criteria, out var hatchedFallback))
        {
            result = hatchedFallback;
            status = LegalizationResult.Regenerated;
        }
        else if (!isStarterLine && !PsyduckYellowEncounterSearch.Search(species).Any() && TryGenerateHatched(trainer, set, species, criteria, out var plain, egg: false))
        {
            result = plain;
            status = LegalizationResult.Regenerated;
        }
        return true;
    }

    /// <summary>
    /// A Pokémon hatched from an egg: it started as the base species of its line (the egg), hatched at the Day Care's hatch place, and is now at the requested level.
    /// The legality rule (PsyduckYellowLegality) only asks for a base species line, an egg location and moves the line can know (egg moves included).
    /// </summary>
    private static bool TryGenerateHatched(ITrainerInfo trainer, IBattleTemplate set, ushort species, EncounterCriteria criteria, out PKM result, bool egg = true)
    {
        result = null!;
        ushort root = 0;
        if (!egg)
            root = species; // a species with no source in the game's data: the analyzer accepts it as it is, nothing to hatch
        else foreach (var candidate in PsyduckYellowExpanded.GetLine(species))
        {
            if (PsyduckYellowExpanded.CanHatch(candidate) && PsyduckYellowExpanded.TryGetPath(candidate, species, out _, out var minLevel) && minLevel <= set.Level)
            {
                root = candidate;
                break;
            }
        }
        if (root == 0 || species >= PsyduckYellowExpanded.SpeciesNames.Length)
            return false;

        var pi = PsyduckYellowExpanded.Personal[species];
        var rootInfo = PsyduckYellowExpanded.Personal[root];
        var id32 = trainer.ID32;
        var date = EncounterDate.GetDateNDS();
        for (int attempt = 0; attempt < 2000; attempt++)
        {
            var pid = Util.Rand32();
            var gender = EntityGender.GetFromPIDAndRatio(pid, pi.Gender);
            if (criteria.IsSpecifiedGender() && !criteria.IsSatisfiedGender(gender))
                continue;
            if (criteria.IsSpecifiedNature() && !criteria.IsSatisfiedNature(pid))
                continue;
            if (criteria.IsSpecifiedAbility() && !criteria.IsSatisfiedAbility((byte)(pid & 1)))
                continue;
            if (set.Shiny != ShinyUtil.GetIsShiny3(id32, pid))
                continue;

            var pk = new PK4
            {
                Species = species,
                Version = trainer.Version,
                BallDPPt = (byte)Ball.Poke,
                BallHGSS = (byte)Ball.Poke,
                ID32 = id32,
                OriginalTrainerGender = trainer.Gender,
                OriginalTrainerName = trainer.OT,
                Language = (int)Language.GetSafeLanguage456((LanguageID)trainer.Language),
                OriginalTrainerFriendship = rootInfo.HatchCycles,
                EggLocation = egg ? Locations.Daycare4 : (ushort)0,
                EggLocationExtended = egg ? Locations.Daycare4 : (ushort)0, // the HG/SS egg place, which the game-rules check reads
                EggMetDate = egg ? date : default,
                MetDate = date,
                MetLocation = egg ? Locations.HatchLocationHGSS : (ushort)1,
                MetLevel = (byte)(egg ? 0 : set.Level),
                PID = pid,
                Gender = gender,
                IsExpandedEngine = true,
                IsPsyduckYellow0029 = true,
            };
            pk.CurrentLevel = (byte)set.Level;
            pk.RefreshAbility((int)(pid & 1));
            if (criteria.IsSpecifiedIVs())
                criteria.SetRandomIVs(pk);
            else
                criteria.SetRandomIVs(pk, 3);
            if (set.HeldItem is > 0 and <= MaxItem4)
                pk.HeldItem = set.HeldItem;
            pk.Nickname = PsyduckYellowExpanded.SpeciesNames[species];
            pk.SetEVs(set.EVs);

            // moves: the requested ones if the game's line can know them, else the line's own level-up moves
            var requested = new ushort[4];
            for (int i = 0; i < 4 && i < set.Moves.Length; i++)
                requested[i] = set.Moves[i] <= MaxMove4 ? set.Moves[i] : (ushort)0;
            foreach (var moves in new[] { requested, GetLevelUpMoves(species, pk.CurrentLevel) })
            {
                pk.SetMoves(moves);
                pk.HealPP();
                pk.ResetPartyStats();
                pk.RefreshChecksum();
                if (new LegalityAnalysis(pk).Valid)
                {
                    result = pk;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// "Legalize this Pokémon" starts from the illegal Pokémon's own details: keep what the game allows and replace the rest
    /// (moves the line cannot know become the game's level-up moves, an ability the species does not have becomes the one its PID selects).
    /// </summary>
    private static bool TryRepair(PK4 pk, LegalityAnalysis la)
    {
        bool changed = false;
        var species = pk.Species;
        if (species < PsyduckYellowExpanded.Personal.Count)
        {
            var pi = PsyduckYellowExpanded.Personal[species];
            var expected = pk.AbilityNumber switch { 4 => pi.EngineHiddenAbility, 2 => pi.EngineAbility2, _ => pi.EngineAbility1 };
            if (pk.Ability != expected)
            {
                pk.Ability = expected;
                changed = true;
            }
        }
        var current = new[] { pk.Move1, pk.Move2, pk.Move3, pk.Move4 };
        var keep = new System.Collections.Generic.List<ushort>();
        for (int i = 0; i < 4; i++)
        {
            if (current[i] != 0 && i < la.Info.Moves.Length && la.Info.Moves[i].Valid && !keep.Contains(current[i]))
                keep.Add(current[i]);
        }
        foreach (var move in GetLevelUpMoves(species, pk.CurrentLevel).Reverse())
        {
            if (keep.Count >= 4)
                break;
            if (move != 0 && !keep.Contains(move))
                keep.Add(move);
        }
        var repaired = new ushort[4];
        for (int i = 0; i < keep.Count; i++)
            repaired[i] = keep[i];
        if (!repaired.SequenceEqual(current))
        {
            pk.SetMoves(repaired);
            pk.HealPP();
            changed = true;
        }
        if (changed)
            pk.RefreshChecksum();
        return changed;
    }

    private static ushort[] GetLevelUpMoves(ushort species, int level)
    {
        var moves = new System.Collections.Generic.List<ushort>();
        if (species < PsyduckYellowExpanded.Learnsets.Length)
        {
            foreach (var move in PsyduckYellowExpanded.Learnsets[species].GetMoveRange((byte)level))
            {
                if (!moves.Contains(move)) // the game's own move ids (newer moves included)
                    moves.Add(move);
            }
        }
        var last = moves.Count > 4 ? moves.GetRange(moves.Count - 4, 4) : moves;
        var result = new ushort[4];
        for (int i = 0; i < last.Count; i++)
            result[i] = last[i];
        return result;
    }
}
