using System.Linq;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Shared._ERRORGATE.Hearing;
using Content.Shared.Audio.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Audio;

// Part of ContentAudioSystem that is responsible for lobby music playing/stopping and round-end sound-effect.
public sealed partial class ContentAudioSystem
{

    public void InitializeDeaf()
    {
        SubscribeLocalEvent<AudioComponent, ComponentInit>(OnDeafMapInit);
    }

    private void OnDeafMapInit(EntityUid uid, AudioComponent component, ref ComponentInit args)
    {
        if (TryComp<DeafComponent>(_player.LocalEntity, out var comp))
            Audio.SetGain(uid, 0f, component);
    }
}
