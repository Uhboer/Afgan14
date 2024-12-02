using Content.Client.CombatMode;
using Linguini.Bundle.Errors;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

public sealed class CombatAudioSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    public override void Initialize()
    {
        _combat.LocalPlayerCombatModeUpdated += OnCombatUpdated;
    }

    private void OnCombatUpdated(bool isInCombatMode)
    {
        var player = _player.LocalEntity;
        var combatModeON = new SoundPathSpecifier("/Audio/Afgan/Misc/combatSoundON.ogg");
        var combatModeOFF = new SoundPathSpecifier("/Audio/Afgan/Misc/combatSoundOFF.ogg");

        if (player == null)
            return;

        if (isInCombatMode)
        {
            _audio.PlayEntity(combatModeON, player.Value, player.Value, null);
        }
        else
        {
            _audio.PlayGlobal(combatModeOFF, player.Value, null);
        }
    }
}
