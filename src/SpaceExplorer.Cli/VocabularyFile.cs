using System.Text.Json;
using SpaceExplorer.Core.Registry;
using SpaceExplorer.Core.Shared;

namespace SpaceExplorer.Cli;

/// <summary>
/// Reads an authored vocabulary from its JSON source form into the canonical <see cref="TemplateVocabulary"/>
/// record, which is the authority; the JSON is an authoring convenience the tool owns (decision 0035).
/// A missing parameter takes its descriptor's full range; a missing connector declares no tags.
/// </summary>
internal sealed record VocabularyFile(TemplateVocabulary Vocabulary, IReadOnlyList<SetRequest> Requests, uint Retries)
{
    public static VocabularyFile Read(string path, CategoryRegistry registry)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        JsonElement rootElement = document.RootElement;

        PackId pack = PackId.Parse(Required(rootElement, "pack").GetString() ?? throw Invalid("pack must be 32 hexadecimal characters"));
        uint retries = rootElement.TryGetProperty("retries", out JsonElement retriesElement) ? retriesElement.GetUInt32() : 16;

        var templates = new List<PrimitiveTemplate>();
        foreach (JsonElement element in Required(rootElement, "templates").EnumerateArray())
        {
            uint id = Required(element, "id").GetUInt32();
            string label = Required(element, "label").GetString() ?? throw Invalid("template label must be text");
            string categoryLabel = Required(element, "category").GetString() ?? throw Invalid("template category must be text");
            CategoryDefinition category = registry.TryFindByLabel(categoryLabel) ?? throw Invalid($"template '{label}' names unknown category '{categoryLabel}'");

            JsonElement parameters = element.TryGetProperty("parameters", out JsonElement p) ? p : default;
            var ranges = new List<ParameterRange>();
            foreach (ParameterDescriptor descriptor in category.Parameters)
            {
                JsonElement rangeElement = default;
                bool given = parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty(descriptor.Label, out rangeElement) && rangeElement.ValueKind != JsonValueKind.Null;
                // From registry revision 4 a template that ranges nothing for a parameter pins it to the
                // category's default; before it, silence widened to the whole range, which was a policy
                // in this tool behind no record (decision 0060).
                ranges.Add(given
                    ? ParseRange(rangeElement, descriptor, label)
                    : registry.CarriesUnits ? Pinned(descriptor, label) : FullRange(descriptor));
            }

            JsonElement connectors = element.TryGetProperty("connectors", out JsonElement c) ? c : default;
            var declarations = new List<ConnectorDeclaration>();
            foreach (ConnectorKind connector in category.Connectors)
            {
                if (connectors.ValueKind == JsonValueKind.Object && connectors.TryGetProperty(connector.Label, out JsonElement declaration))
                {
                    declarations.Add(new ConnectorDeclaration(Strings(declaration, "provides"), Strings(declaration, "requires")));
                }
                else
                {
                    declarations.Add(ConnectorDeclaration.Empty);
                }
            }

            // What definitions of this template suit, which a reference elsewhere may require of them
            // (decision 0055). A revision that carries no tags refuses a template that declares any.
            string[] tags = element.TryGetProperty("tags", out JsonElement tagsElement) ? Strings(tagsElement) : [];

            templates.Add(PrimitiveTemplate.Create(registry, id, label, category.Id, ranges, declarations, tags));
        }

        var requests = new List<SetRequest>();
        foreach (JsonElement element in Required(rootElement, "requests").EnumerateArray())
        {
            requests.Add(new SetRequest(Required(element, "template").GetUInt32(), Required(element, "count").GetUInt32()));
        }

        return new VocabularyFile(TemplateVocabulary.Create(registry, pack, templates), requests, retries);
    }

    private static ParameterRange ParseRange(JsonElement element, ParameterDescriptor descriptor, string template)
    {
        switch (descriptor.Kind)
        {
            case ParameterKind.Bool:
                {
                    bool[] allowed = [.. element.EnumerateArray().Select(item => item.GetBoolean())];
                    return ParameterRange.Bool(allowed.Contains(false), allowed.Contains(true));
                }

            case ParameterKind.Enum:
                {
                    uint[] indices = [.. element.EnumerateArray().Select(item =>
                    {
                        string name = item.GetString() ?? throw Invalid($"'{template}/{descriptor.Label}' enum entries must be label text");
                        int index = descriptor.EnumLabels.ToList().IndexOf(name);
                        return index < 0 ? throw Invalid($"'{template}/{descriptor.Label}' has no label '{name}'") : (uint)index;
                    }).Order()];
                    return ParameterRange.Enum(indices);
                }

            case ParameterKind.Integer:
                {
                    (long min, long max) = Pair(element);
                    return ParameterRange.Integer(min, max);
                }

            case ParameterKind.BinaryTurn:
                {
                    (long min, long max) = Pair(element);
                    return ParameterRange.BinaryTurn(checked((int)min), checked((int)max));
                }

            case ParameterKind.Rotation:
                {
                    (long, long)[] pairs = Triple(element);
                    return ParameterRange.Rotation(
                        (checked((int)pairs[0].Item1), checked((int)pairs[0].Item2)),
                        (checked((int)pairs[1].Item1), checked((int)pairs[1].Item2)),
                        (checked((int)pairs[2].Item1), checked((int)pairs[2].Item2)));
                }

            case ParameterKind.Vector3:
                {
                    (long, long)[] pairs = Triple(element);
                    return ParameterRange.Vector3(pairs[0], pairs[1], pairs[2]);
                }

            case ParameterKind.Colour:
                {
                    (long, long)[] pairs = [.. element.EnumerateArray().Select(Pair)];
                    if (pairs.Length != 4)
                    {
                        throw Invalid($"'{template}/{descriptor.Label}' needs four [min, max] channel ranges");
                    }

                    return ParameterRange.Colour(Channel(pairs[0]), Channel(pairs[1]), Channel(pairs[2]), Channel(pairs[3]));
                }

            case ParameterKind.PrimitiveRef:
                // A reference states the tags what it draws must carry, as a list of text; an absent or
                // empty list draws from any definition of the category (decision 0055).
                return ParameterRange.Ref(Strings(element));

            default:
                throw Invalid($"'{template}/{descriptor.Label}' has an unknown kind");
        }
    }

    /// <summary>The range that admits only the descriptor's default, which is what silence now means.</summary>
    private static ParameterRange Pinned(ParameterDescriptor descriptor, string template)
    {
        if (descriptor.Kind == ParameterKind.PrimitiveRef)
        {
            // A reference has no default and keeps drawing from every definition of its category.
            return ParameterRange.Ref();
        }

        ParameterValue standard = descriptor.Default
            ?? throw Invalid($"'{template}/{descriptor.Label}' states no range and the category declares no default");

        return descriptor.Kind switch
        {
            ParameterKind.Bool => ParameterRange.Bool(!standard.AsBool, standard.AsBool),
            ParameterKind.Enum => ParameterRange.Enum(standard.AsEnumIndex),
            ParameterKind.Integer => ParameterRange.Integer(standard.AsInteger, standard.AsInteger),
            ParameterKind.BinaryTurn => ParameterRange.BinaryTurn(standard.AsBinaryTurn, standard.AsBinaryTurn),
            ParameterKind.Rotation => Rotation(standard),
            ParameterKind.Vector3 => Vector(standard),
            ParameterKind.Colour => Channels(standard),
            _ => throw Invalid($"'{template}/{descriptor.Label}' has an unknown kind"),
        };
    }

    private static ParameterRange Rotation(ParameterValue standard)
    {
        (int yaw, int pitch, int roll) = standard.AsRotation;
        return ParameterRange.Rotation((yaw, yaw), (pitch, pitch), (roll, roll));
    }

    private static ParameterRange Vector(ParameterValue standard)
    {
        (long x, long y, long z) = standard.AsVector3;
        return ParameterRange.Vector3((x, x), (y, y), (z, z));
    }

    private static ParameterRange Channels(ParameterValue standard)
    {
        (byte red, byte green, byte blue, byte alpha) = standard.AsColour;
        return ParameterRange.Colour((red, red), (green, green), (blue, blue), (alpha, alpha));
    }

    private static ParameterRange FullRange(ParameterDescriptor descriptor) => descriptor.Kind switch
    {
        ParameterKind.Bool => ParameterRange.Bool(true, true),
        ParameterKind.Enum => ParameterRange.Enum([.. Enumerable.Range(0, descriptor.EnumLabels.Count).Select(index => (uint)index)]),
        ParameterKind.Integer => ParameterRange.Integer(descriptor.Min, descriptor.Max),
        ParameterKind.BinaryTurn => ParameterRange.BinaryTurn((int)descriptor.Min, (int)descriptor.Max),
        ParameterKind.Rotation => ParameterRange.Rotation((int.MinValue, int.MaxValue), (int.MinValue, int.MaxValue), (int.MinValue, int.MaxValue)),
        ParameterKind.Vector3 => ParameterRange.Vector3((descriptor.Min, descriptor.Max), (descriptor.Min, descriptor.Max), (descriptor.Min, descriptor.Max)),
        ParameterKind.Colour => ParameterRange.Colour((0, 255), (0, 255), (0, 255), (0, 255)),
        ParameterKind.PrimitiveRef => ParameterRange.Ref(),
        _ => throw Invalid($"'{descriptor.Label}' has an unknown kind"),
    };

    private static (long Min, long Max) Pair(JsonElement element)
    {
        long[] values = [.. element.EnumerateArray().Select(item => item.GetInt64())];
        return values.Length == 2 ? (values[0], values[1]) : throw Invalid("a range is [min, max]");
    }

    private static (long, long)[] Triple(JsonElement element)
    {
        // Either one [min, max] applied to every component or three of them.
        if (element.GetArrayLength() == 2 && element[0].ValueKind == JsonValueKind.Number)
        {
            (long, long) pair = Pair(element);
            return [pair, pair, pair];
        }

        (long, long)[] pairs = [.. element.EnumerateArray().Select(Pair)];
        return pairs.Length == 3 ? pairs : throw Invalid("a three-component range is [min, max] or three of them");
    }

    private static (byte Min, byte Max) Channel((long Min, long Max) pair) => (checked((byte)pair.Min), checked((byte)pair.Max));

    private static IReadOnlyList<string> Strings(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement list) ? Strings(list) : [];

    /// <summary>A list of text, which a tag list and a reference's requirement both are.</summary>
    private static string[] Strings(JsonElement list) =>
        list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Select(item => item.GetString() ?? throw Invalid("tag entries must be text"))]
            : throw Invalid("a tag list must be an array of text");

    private static JsonElement Required(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) ? value : throw Invalid($"missing '{name}'");

    private static FormatException Invalid(string message) => new($"Vocabulary file: {message}.");
}
