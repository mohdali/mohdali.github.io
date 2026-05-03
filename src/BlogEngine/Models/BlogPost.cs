namespace BlogEngine;

public sealed record BlogPost(
    string Title,
    string Url,
    DateTime Timestamp,
    Type Type,
    string Description,
    string[] Tags,
    string? Image,
    string? ImageAlt);
