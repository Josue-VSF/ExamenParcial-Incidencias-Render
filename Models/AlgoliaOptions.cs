namespace ExamenParcial_Incidencias_Render.Models;

public class AlgoliaOptions
{
    public const string SectionName = "Algolia";

    public string ApplicationId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApplicationId) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(IndexName);
}
