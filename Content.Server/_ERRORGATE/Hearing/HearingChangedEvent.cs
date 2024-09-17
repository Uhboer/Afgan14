namespace Content.Server._ERRORGATE.Hearing;

public sealed class HearingChangedEvent : EntityEventArgs
{
    public EntityUid Target;

    public bool Undeafen;

    public bool Permanent;

    public float Duration;

    public string DeafChatMessage;

    public HearingChangedEvent(EntityUid target, bool undeafen)
    {
        Target = target;
        Undeafen = undeafen;
        Permanent = false;
        Duration = 0f;
        DeafChatMessage = "";
    }

    public HearingChangedEvent(EntityUid target, bool undeafen, bool permanent, float duration, string deafChatMessage)
    {
        Target = target;
        Undeafen = undeafen;
        Permanent = permanent;
        Duration = duration;
        DeafChatMessage = deafChatMessage;
    }
}
