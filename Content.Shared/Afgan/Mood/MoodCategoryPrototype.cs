using Robust.Shared.Prototypes;

namespace Content.Shared._Afgan.Mood;

[Prototype]
public sealed class MoodCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
}
