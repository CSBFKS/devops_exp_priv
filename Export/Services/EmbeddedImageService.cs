using System.Text.RegularExpressions;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Export.Services
{
    /// <summary>
    /// Downloads embedded Azure DevOps image attachments referenced from HTML.
    /// </summary>
    public class EmbeddedImageService : IEmbeddedImageService
    {
        private readonly string pat;
        private readonly string url;
        private readonly IWriterService writerService;

        private static readonly Regex AttachmentUrlRegex = new(
            @"(?<url>https?://[^\s""']+/_apis/wit/attachments/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\?fileName=[^\s""']*)?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public EmbeddedImageService(string pat, string url, IWriterService writerService)
        {
            this.pat = pat;
            this.url = url;
            this.writerService = writerService;
        }

        public IReadOnlyDictionary<Guid, string> ExtractAndDownloadImages(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return new Dictionary<Guid, string>();
            }

            var attachments = new Dictionary<Guid, string>();
            var matches = AttachmentUrlRegex.Matches(html);
            foreach (Match match in matches)
            {
                if (!Guid.TryParse(match.Groups["id"].Value, out var attachmentId))
                {
                    continue;
                }

                if (attachments.ContainsKey(attachmentId))
                {
                    continue;
                }

                using var client = new DevOpsClient(this.pat, this.url);
                try
                {
                    using var content = client.Get().GetAttachmentContentAsync(attachmentId).Result;
                    var fileName = GetLocalFileName(match.Value, attachmentId);
                    var attachment = new Models.Attachment(new WorkItemRelation
                    {
                        Url = $"https://dev.azure.com/_apis/wit/attachments/{attachmentId}",
                        Attributes = new Dictionary<string, object> { ["name"] = fileName }
                    });

                    this.writerService.WriteAttachmentAsync(attachment, content).GetAwaiter().GetResult();
                    attachments[attachmentId] = fileName;
                }
                catch
                {
                    // Ignore image download failures so export still succeeds.
                }
            }

            return attachments;
        }

        private static string GetLocalFileName(string url, Guid attachmentId)
        {
            var query = new Uri(url).Query.TrimStart('?');
            if (!string.IsNullOrWhiteSpace(query))
            {
                foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var segments = part.Split('=', 2);
                    if ((segments.Length == 2) && segments[0].Equals("fileName", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileName = Uri.UnescapeDataString(segments[1]);
                        var extension = Path.GetExtension(fileName);
                        var baseName = Path.GetFileNameWithoutExtension(fileName);

                        if (string.IsNullOrWhiteSpace(baseName) || baseName.Equals("image", StringComparison.OrdinalIgnoreCase) || baseName.Equals("attachment", StringComparison.OrdinalIgnoreCase))
                        {
                            return $"{attachmentId}{(string.IsNullOrWhiteSpace(extension) ? ".png" : extension)}";
                        }

                        var safeBaseName = Regex.Replace(baseName, "[^a-zA-Z0-9._-]+", "-");
                        return $"{safeBaseName}-{attachmentId}{(string.IsNullOrWhiteSpace(extension) ? ".png" : extension)}";
                    }
                }
            }

            return $"{attachmentId}.png";
        }
    }
}
