namespace Orderly.Domain.Entities;

/// <summary>Formas de login aceitas. Não existe login com senha.</summary>
public enum AuthProvider
{
    Email = 1,
    Apple = 2,
    Google = 3,
}
