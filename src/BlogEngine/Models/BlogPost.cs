namespace BlogEngine;

public sealed record BlogPost(
    string Title,
    string Url,
    DateTime Timestamp,
    Type Type,
    string Description,
    string[] Tags,
    string? Image,
    string? ImageAlt,
    string? ImageType,
    int? ImageWidth,
    int? ImageHeight,
    string? CardImage,
    string? CardImageAlt);

public sealed record BlogTag(string Name, string Slug, int PostCount);
