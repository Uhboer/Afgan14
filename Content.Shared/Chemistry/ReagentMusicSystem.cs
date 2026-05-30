using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// Система для управления музыкой во время метаболизма реагентов.
    /// </summary>
    public abstract class SharedReagentMusicSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        private readonly List<Entity<ReagentMusicComponent>> _activeMusic = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<ReagentMusicComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<ReagentMusicComponent, ComponentShutdown>(OnComponentShutdown);
        }

        private void OnComponentStartup(Entity<ReagentMusicComponent> entity, ref ComponentStartup args)
        {
            Logger.Info($"ReagentMusicSystem: Component startup for entity {entity.Owner}");
            _activeMusic.Add(entity);
            
            // Звук будет проигран в ActivateMusic
            // Не проигрываем звук здесь, чтобы избежать дублирования
        }

        private void OnComponentShutdown(Entity<ReagentMusicComponent> entity, ref ComponentShutdown args)
        {
            Logger.Info($"ReagentMusicSystem: Component shutdown for entity {entity.Owner}");
            _activeMusic.Remove(entity);
            
            // Останавливаем звук при удалении компонента
            if (entity.Comp.PlayingStream != null && Exists(entity.Comp.PlayingStream.Value))
            {
                _audioSystem.Stop(entity.Comp.PlayingStream.Value);
            }
        }

        /// <summary>
        /// Активирует музыку для реагента на указанное время.
        /// </summary>
        public void ActivateMusic(EntityUid uid, string reagentId, SoundSpecifier sound, TimeSpan duration, float requiredAmount = 1.0f, AudioParams? soundParams = null)
        {
            Logger.Info($"ReagentMusicSystem: Activating music for reagent {reagentId} on entity {uid} for duration {duration}");
            
            // Проверяем, не играет ли уже музыка для этого реагента
            if (TryComp<ReagentMusicComponent>(uid, out var existingComponent) && existingComponent.ReagentId == reagentId)
            {
                // Музыка уже играет, просто обновляем таймер
                Logger.Info($"ReagentMusicSystem: Music already playing for reagent {reagentId} on entity {uid}, extending timer");
                existingComponent.MusicTimer = _gameTiming.CurTime + duration;
                
                if (soundParams != null)
                {
                    existingComponent.SoundParams = soundParams.Value;
                }
                
                Dirty(uid, existingComponent);
                return;
            }
            
            // Музыка не играет, создаем новый компонент
            var component = EnsureComp<ReagentMusicComponent>(uid);
            component.ReagentId = reagentId;
            component.MusicSound = sound;
            component.MusicTimer = _gameTiming.CurTime + duration;
            component.RequiredAmount = requiredAmount;
            
            if (soundParams != null)
            {
                component.SoundParams = soundParams.Value;
            }

            Dirty(uid, component);
            
            // Начинаем проигрывать музыку (только если компонент был создан)
            if (sound != null)
            {
                Logger.Info($"ReagentMusicSystem: Playing predicted sound for entity {uid}");
                component.PlayingStream = _audioSystem.PlayPvs(sound, uid, component.SoundParams)?.Entity;
            }
            else
            {
                Logger.Warning($"ReagentMusicSystem: Sound is null for entity {uid}");
            }
        }

        /// <summary>
        /// Деактивирует музыку для указанного реагента.
        /// </summary>
        public void DeactivateMusic(EntityUid uid, string reagentId)
        {
            if (!TryComp<ReagentMusicComponent>(uid, out var component) || component.ReagentId != reagentId)
                return;

            Logger.Info($"ReagentMusicSystem: Deactivating music for reagent {reagentId} on entity {uid}");
            
            // Останавливаем звук перед удалением компонента
            if (component.PlayingStream != null && Exists(component.PlayingStream.Value))
            {
                _audioSystem.Stop(component.PlayingStream.Value);
            }
            
            RemComp<ReagentMusicComponent>(uid);
        }

        /// <summary>
        /// Проверяет, активна ли музыка для указанного реагента.
        /// </summary>
        public bool IsMusicActive(EntityUid uid, string reagentId)
        {
            return TryComp<ReagentMusicComponent>(uid, out var component) && component.ReagentId == reagentId;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var currentTime = _gameTiming.CurTime;

            for (var i = _activeMusic.Count - 1; i >= 0; i--)
            {
                var music = _activeMusic[i];

                if (music.Comp.Deleted)
                {
                    _activeMusic.RemoveAt(i);
                    continue;
                }

                if (music.Comp.MusicTimer > currentTime)
                    continue;

                // Время музыки истекло, удаляем компонент
                Logger.Info($"ReagentMusicSystem: Music timer expired for entity {music.Owner}");
                
                // Останавливаем звук перед удалением компонента
                if (music.Comp.PlayingStream != null && Exists(music.Comp.PlayingStream.Value))
                {
                    _audioSystem.Stop(music.Comp.PlayingStream.Value);
                }
                
                _activeMusic.RemoveAt(i);
                EntityManager.RemoveComponent<ReagentMusicComponent>(music);
            }
        }
    }
}