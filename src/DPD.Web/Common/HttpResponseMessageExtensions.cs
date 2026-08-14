using System.Net;

namespace DPD.Web.Common;

/// <summary>
/// Aide à diagnostiquer les échecs d'appels HTTP (ex: 403 Forbidden) en journalisant
/// le corps de la réponse et en levant un message clair pour l'utilisateur,
/// plutôt qu'un simple EnsureSuccessStatusCode() qui masque le détail de l'erreur.
/// </summary>
public static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessOrThrowAsync(
        this HttpResponseMessage response,
        ILogger logger,
        string actionDescription)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();

        logger.LogWarning(
            "Échec de l'action '{Action}': {StatusCode} {ReasonPhrase}. Corps de la réponse: {Body}",
            actionDescription,
            (int)response.StatusCode,
            response.ReasonPhrase,
            string.IsNullOrWhiteSpace(body) ? "(vide)" : body);

        var message = response.StatusCode switch
        {
            HttpStatusCode.Forbidden =>
                $"Action refusée (403 Forbidden) : votre rôle actuel ne dispose pas des permissions requises pour '{actionDescription}'. " +
                "Changez de rôle mock (menu latéral) pour un rôle autorisé, puis réessayez.",
            HttpStatusCode.Unauthorized =>
                $"Non authentifié (401) pour '{actionDescription}'. Veuillez vous reconnecter.",
            _ =>
                $"Échec de '{actionDescription}' : {(int)response.StatusCode} {response.ReasonPhrase}." +
                (string.IsNullOrWhiteSpace(body) ? string.Empty : $" Détail: {body}")
        };

        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
