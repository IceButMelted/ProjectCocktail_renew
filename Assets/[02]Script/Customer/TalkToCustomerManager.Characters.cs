// ============================================================
//  TalkToCustomerManager.Characters.cs — part of TalkToCustomerManager.
//
//  The characters at the bar: spots to stand on, coming in, going out,
//  and the Yarn commands that drive CharacterView (expression, look,
//  enter, exit). Plan: Docs/Bar410_CharacterPresentation_Plan.md
//
//  Long form:   <<CharacterExpression CharacterName.Isla Sad>>   <<CharacterLook CharacterName.Isla L>>
//               <<CharacterEnter CharacterName.Isla 2>>          <<CharacterExit CharacterName.Isla>>
//  Short form:  <<Isla Sad>>  <<Isla 1>>  <<Isla Look L>>  <<Isla Enter 2>>  <<Isla Exit>>
//  Both end up in the same methods below. Enter and Exit hold Yarn until the fade is over.
//  A new character needs one more short-form line at the bottom of this file.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using static E_Cocktail;

public partial class TalkToCustomerManager
{
    [Header("Characters")]
    [Tooltip("Places a character can stand, shared by everyone. <<Isla Enter 2>> means _spots[2].")]
    [SerializeField] private Transform[] _spots;

    [Tooltip("Seconds for one fade in or out, for every character.")]
    [SerializeField, Min(0f)] private float _fadeSeconds = 0.8f;

    [Tooltip("Small up-and-down steps during every fade in and out, as if walking. Height 0 = no motion.")]
    [SerializeField] private WalkBob _walkBob = new WalkBob { Height = 0.03f, Steps = 2 };

    [Tooltip("SFX Sound ID (SoundManager) played when a character comes into the shop. Empty = silent.")]
    [SerializeField] private string _enterSoundId;

    [Tooltip("Seconds between the enter sound and the start of the fade in. Yarn waits for it.")]
    [SerializeField, Min(0f)] private float _enterSoundLead = 0.6f;

    [Tooltip("SFX Sound ID played after a character has left the shop. Empty = silent.")]
    [SerializeField] private string _exitSoundId;

    [Tooltip("Seconds after the fade out ends before the exit sound plays. Yarn does not wait for it.")]
    [SerializeField, Min(0f)] private float _exitSoundDelay = 0.6f;

    /// <summary>Skipping or loading finishes a fade at once, like wait_scene.</summary>
    private float FadeSeconds => SceneLoaderBridge.IsSilentReplay || DialogueControls.IsSkipping ? 0f : _fadeSeconds;

    /// <summary>Enter/exit sounds and their waits, off while skipping or loading.</summary>
    private bool SoundsOn => !SceneLoaderBridge.IsSilentReplay && !DialogueControls.IsSkipping;

    // ── Enter / Exit ───────────────────────────────────────

    /// <summary>
    /// Brings a character to a spot. Not in the shop: fade in there. Already there: nothing.
    /// Standing elsewhere: fade out, move, fade in. Yarn waits for all of it.
    /// </summary>
    public IEnumerator Enter(CharacterView view, int spot)
    {
        if (_spots == null || spot < 0 || spot >= _spots.Length || _spots[spot] == null)
        {
            Debug.LogWarning($"[TalkToCustomerManager] {view.Name}: spot {spot} does not exist.", this);
            yield break;
        }

        if (view.IsPresent && view.Spot == spot) yield break;
        // Moving between spots stays inside the shop: no enter sound.
        if (view.IsPresent) yield return view.FadeOut(FadeSeconds, _walkBob);
        else if (SoundsOn && !string.IsNullOrEmpty(_enterSoundId))
        {
            SoundManager.Instance?.PlaySFX(_enterSoundId);
            if (_enterSoundLead > 0f) yield return new WaitForSeconds(_enterSoundLead);
        }

        // ponytail: only warns, the other character is not asked to leave. Add when a script needs it.
        foreach (var other in _byName.Values)
            if (other != view && other.IsPresent && other.Spot == spot)
                Debug.LogWarning($"[TalkToCustomerManager] {view.Name} enters spot {spot} where {other.Name} stands.", this);

        view.Spot = spot;
        view.transform.position = _spots[spot].position;
        yield return view.FadeIn(FadeSeconds, _walkBob);
    }

    /// <summary>Fades a character out of the shop. Not in the shop: nothing.</summary>
    public IEnumerator Exit(CharacterView view)
    {
        if (!view.IsPresent) yield break;
        yield return view.FadeOut(FadeSeconds, _walkBob);
        if (SoundsOn && !string.IsNullOrEmpty(_exitSoundId)) StartCoroutine(PlayExitSound());
    }

    private IEnumerator PlayExitSound()
    {
        if (_exitSoundDelay > 0f) yield return new WaitForSeconds(_exitSoundDelay);
        SoundManager.Instance?.PlaySFX(_exitSoundId);
    }

    // ── Save / load ────────────────────────────────────────

    /// <summary>Who is in the shop, where, and how they look, for the save system.</summary>
    public Dictionary<CharacterName, NPCData> Snapshot()
    {
        var snapshot = new Dictionary<CharacterName, NPCData>();

        foreach (var pair in _byName)
        {
            var view = pair.Value;
            snapshot[pair.Key] = new NPCData
            {
                isPresent = view.IsPresent,
                spot = view.Spot,
                expressionIndex = view.ExpressionIndex,
                look = view.Look
            };
        }

        return snapshot;
    }

    /// <summary>Puts characters back as <see cref="Snapshot"/> found them, without a fade. Names with no view are skipped.</summary>
    public void Restore(IReadOnlyDictionary<CharacterName, NPCData> saved)
    {
        foreach (var pair in saved)
        {
            var view = Find(pair.Key);
            if (view == null) continue;

            var state = pair.Value;
            view.SetExpression(state.expressionIndex.ToString());
            // LEGACY VISUAL: single-sheet art. Remove when every character has layered art (Docs/Bar410_LegacyCharacterVisual_Plan.md).
            // Legacy art shows whichever came last; the save keeps no order, so a side look wins and Look F after an expression loads as the expression.
            if (!view.UsesLegacyVisual || state.look != Facing.Forward) view.SetLook(state.look);
            view.Spot = state.spot;

            if (state.isPresent && _spots != null && state.spot >= 0 && state.spot < _spots.Length && _spots[state.spot] != null)
            {
                view.transform.position = _spots[state.spot].position;
                view.ShowInstant();
            }
            else view.HideInstant();
        }
    }

    // ── Yarn commands, long form ───────────────────────────

    [YarnCommand("CharacterExpression")]
    public static void Yarn_CharacterExpression(string character, string expression)
        => Current?.Find(character)?.SetExpression(expression);

    /// <summary>L, F or R (Left, Forward, Right also work).</summary>
    [YarnCommand("CharacterLook")]
    public static void Yarn_CharacterLook(string character, string look)
    {
        var view = Current?.Find(character);
        if (view == null) return;

        string raw = look.Contains(".") ? look.Split('.')[1] : look;
        switch (raw.ToLowerInvariant())
        {
            case "l": case "left": view.SetLook(Facing.Left); break;
            case "r": case "right": view.SetLook(Facing.Right); break;
            case "f": case "forward": view.SetLook(Facing.Forward); break;
            default: Debug.LogWarning($"[TalkToCustomerManager] Unknown look '{look}' for {view.Name}. Use L, F or R."); break;
        }
    }

    [YarnCommand("CharacterEnter")]
    public static IEnumerator Yarn_CharacterEnter(string character, int spot = 0)
    {
        var view = Current?.Find(character);
        if (view != null) yield return Current.Enter(view, spot);
    }

    [YarnCommand("CharacterExit")]
    public static IEnumerator Yarn_CharacterExit(string character)
    {
        var view = Current?.Find(character);
        if (view != null) yield return Current.Exit(view);
    }

    // ── Yarn commands, short form: <<Isla Sad>> <<Isla Look L>> <<Isla Enter 2>> <<Isla Exit>> ──

    private static IEnumerator Short(CharacterName who, string verb, string arg)
    {
        var view = Current?.Find(who);
        if (view == null) yield break;

        switch (verb.ToLowerInvariant())
        {
            case "look": Yarn_CharacterLook(who.ToString(), arg ?? ""); break;
            case "enter":
                int spot = 0;
                if (arg != null && !int.TryParse(arg, out spot))
                {
                    Debug.LogWarning($"[TalkToCustomerManager] '{who} Enter {arg}': the spot must be a number.");
                    break;
                }
                yield return Current.Enter(view, spot);
                break;
            case "exit": yield return Current.Exit(view); break;
            default: view.SetExpression(verb); break;
        }
    }

    [YarnCommand("Cole")] public static IEnumerator Cole(string verb, string arg = null) => Short(CharacterName.Cole, verb, arg);
    [YarnCommand("Owen")] public static IEnumerator Owen(string verb, string arg = null) => Short(CharacterName.Owen, verb, arg);
    [YarnCommand("Walter")] public static IEnumerator Walter(string verb, string arg = null) => Short(CharacterName.Walter, verb, arg);
    [YarnCommand("Faye")] public static IEnumerator Faye(string verb, string arg = null) => Short(CharacterName.Faye, verb, arg);
    [YarnCommand("Isla")] public static IEnumerator Isla(string verb, string arg = null) => Short(CharacterName.Isla, verb, arg);
    [YarnCommand("Claire")] public static IEnumerator Claire(string verb, string arg = null) => Short(CharacterName.Claire, verb, arg);
    [YarnCommand("Eliana")] public static IEnumerator Eliana(string verb, string arg = null) => Short(CharacterName.Eliana, verb, arg);
    [YarnCommand("Arthur")] public static IEnumerator Arthur(string verb, string arg = null) => Short(CharacterName.Arthur, verb, arg);
}
