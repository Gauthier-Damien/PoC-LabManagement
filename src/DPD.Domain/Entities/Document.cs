using DPD.Domain.Common;

namespace DPD.Domain.Entities;

public sealed class Document : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public int Version { get; set; } = 1;
}
