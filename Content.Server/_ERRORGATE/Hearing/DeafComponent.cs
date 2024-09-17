using System.Threading;

namespace Content.Server._ERRORGATE.Hearing;

/// <summary>
/// Changes all incoming chat messages to DeafChatMessage.
/// Added by the DeafnessSystem on the HearingChangedEvent
/// </summary>
[RegisterComponent]
public sealed partial class DeafComponent : Component
{
    [DataField]
    public string DeafChatMessage = "You can almost hear something...";

    [DataField]
    public bool Permanent = false;

    [DataField]
    public float Duration = 0f; // In seconds

    public CancellationTokenSource? TokenSource;
}
