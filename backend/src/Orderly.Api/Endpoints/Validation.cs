namespace Orderly.Api.Endpoints;

/// <summary>Acumula erros de validação no formato de ValidationProblem.</summary>
internal sealed class Validation
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool IsValid => _errors.Count == 0;

    public Validation Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            _errors[field] = list = [];
        }

        list.Add(message);
        return this;
    }

    public Validation Text(string field, string? value, int maxLength, bool required = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                Add(field, "Obrigatório.");
            }
        }
        else if (value.Trim().Length > maxLength)
        {
            Add(field, $"Máximo de {maxLength} caracteres.");
        }

        return this;
    }

    public IResult Problem() =>
        Results.ValidationProblem(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
}
