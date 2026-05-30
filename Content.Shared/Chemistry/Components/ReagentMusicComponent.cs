using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// Компонент для реагентов, который проигрывает музыку во время метаболизма.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ReagentMusicComponent : Component
    {
        /// <summary>
        /// Звук, который будет проигрываться во время метаболизма реагента.
        /// </summary>
        [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        [DataField(required: true)]
        public SoundSpecifier? MusicSound { get; set; }

        /// <summary>
        /// Параметры звука.
        /// </summary>
        [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public AudioParams SoundParams { get; set; } = AudioParams.Default.WithVolume(-2f).WithLoop(true);

        /// <summary>
        /// Когда текущая музыка должна закончиться.
        /// </summary>
        [AutoNetworkedField, ViewVariables]
        public TimeSpan MusicTimer { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Идентификатор реагента, который вызывает музыку.
        /// </summary>
        [AutoNetworkedField, ViewVariables]
        public string? ReagentId { get; set; }

        /// <summary>
        /// Количество реагента, необходимое для активации музыки.
        /// </summary>
        [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float RequiredAmount { get; set; } = 1.0f;

        /// <summary>
        /// Сущность звука, которая проигрывается.
        /// </summary>
        [ViewVariables]
        public EntityUid? PlayingStream { get; set; }
    }
}