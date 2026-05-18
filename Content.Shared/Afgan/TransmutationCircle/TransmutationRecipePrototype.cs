using Robust.Shared.Prototypes;

namespace Content.Shared._AFGAN.Prototypes;

[Prototype("transmutationRecipeSimple")]
public sealed partial class TransmutationRecipeSimple : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField("result", required: true)] public string Result = default!;
    [DataField("materials", required: true)] public Dictionary<string, int> Materials = new();
}