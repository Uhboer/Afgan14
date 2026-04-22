using Content.Client.Audio;
using Content.Client.CombatMode;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

public sealed class CombatAudioSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;

    private static readonly SoundPathSpecifier CombatModeOnSound =
        new("/Audio/Afgan/Misc/combatSoundON.ogg");

    private static readonly SoundPathSpecifier CombatModeOffSound =
        new("/Audio/Afgan/Misc/combatSoundOFF.ogg");

    private static readonly SoundPathSpecifier CombatMusicPath =
        new("/Audio/Afgan/Misc/combat.ogg");

    private static readonly AudioParams CombatMusicParams = AudioParams.Default
        .WithLoop(true)
        .WithVolume(-15f);

    /// <summary>
    /// Currently playing combat music stream. Null when not in combat mode.
    /// </summary>
    private EntityUid? _combatMusicStream;

    public override void Initialize()
    {
        base.Initialize();
        _combat.LocalPlayerCombatModeUpdated += OnCombatUpdated;
        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _combat.LocalPlayerCombatModeUpdated -= OnCombatUpdated;
        StopCombatMusic();
    }

    private void OnCombatUpdated(bool isInCombatMode)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;

        if (isInCombatMode)
        {
            _audio.PlayEntity(CombatModeOnSound, player.Value, player.Value, null);
            _contentAudio.DisableAmbientMusic();
            StartCombatMusic();
        }
        else
        {
            _audio.PlayGlobal(CombatModeOffSound, player.Value, null);
            StopCombatMusic();
            _contentAudio.DelayAmbientMusic(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Blocks ambient music from starting while the player is in combat mode.
    /// </summary>
    private void OnPlayAmbientMusic(ref PlayAmbientMusicEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (_combat.IsInCombatMode())
            ev.Cancelled = true;
    }

    private void StartCombatMusic()
    {
        // Don't start a second stream if one is already playing
        if (_combatMusicStream != null && EntityManager.EntityExists(_combatMusicStream.Value))
            return;

        var stream = _audio.PlayGlobal(CombatMusicPath, Filter.Local(), false, CombatMusicParams);
        _combatMusicStream = stream?.Entity;
    }

    private void StopCombatMusic()
    {
        _combatMusicStream = _audio.Stop(_combatMusicStream);
    }
}
