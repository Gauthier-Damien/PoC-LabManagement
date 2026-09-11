namespace DPD.Application.Common.Models;

/// <summary>
/// Enveloppe générique de résultat paginé, exposant le total pour permettre au frontend
/// de construire une pagination réelle (exigence TDD : "Toutes les listes métier > 100 lignes
/// doivent être paginées").
/// </summary>
public sealed record PaginatedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
