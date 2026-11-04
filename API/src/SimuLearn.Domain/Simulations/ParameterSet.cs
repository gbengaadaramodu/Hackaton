using System.Globalization;

namespace SimuLearn.Domain.Simulations;

/// <summary>Immutable, case-insensitive set of simulation parameter values.</summary>
public sealed class ParameterSet
{
    private readonly Dictionary<string, double> _values;

    public ParameterSet(IEnumerable<KeyValuePair<string, double>> values)
    {
        _values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            _values[key] = value;
        }
    }

    public static ParameterSet Empty { get; } = new([]);

    public IReadOnlyDictionary<string, double> Values => _values;

    public int Count => _values.Count;

    public double this[string name] =>
        _values.TryGetValue(name, out var value)
            ? value
            : throw new KeyNotFoundException($"Parameter '{name}' is not present in the state.");

    public bool TryGet(string name, out double value) => _values.TryGetValue(name, out value);

    public ParameterSet With(string name, double value)
    {
        var copy = new Dictionary<string, double>(_values, StringComparer.OrdinalIgnoreCase) { [name] = value };
        return new ParameterSet(copy);
    }

    public Dictionary<string, double> ToDictionary() =>
        new(_values, StringComparer.OrdinalIgnoreCase);

    public static string Format(double value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);
}
