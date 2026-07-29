using System.Text.RegularExpressions;

namespace Export.Services
{
    /// <summary>
    /// Rewrites Azure DevOps attachment image URLs to local file paths.
    /// </summary>
    public static partial class HtmlImageRewriter
    {
        private static readonly Regex AttachmentUrlRegex = new(
            @"(?<url>https?://[^\s""']+/_apis/wit/attachments/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\?fileName=[^\s""']*)?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MarkdownImageRegex = new(
            @"!\[(?<alt>[^\]]*)\]\((?<url>[^)]+)\)",
            RegexOptions.Compiled);

        /// <summary>
        /// Rewrites Azure DevOps attachment URLs in an HTML fragment to local paths.
        /// </summary>
        /// <param name="html">HTML content.</param>
        /// <param name="attachmentFiles">Map of attachment id to local file name.</param>
        /// <returns>Rewritten HTML.</returns>
        public static string RewriteAttachmentUrls(string html, IReadOnlyDictionary<Guid, string> attachmentFiles)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var rewritten = AttachmentUrlRegex.Replace(html, match =>
            {
                var id = match.Groups["id"].Value;
                if (!Guid.TryParse(id, out var attachmentId))
                {
                    return match.Value;
                }

                if (!attachmentFiles.TryGetValue(attachmentId, out var fileName))
                {
                    return match.Value;
                }

                return $"../.attachments/{fileName}";
            });

            rewritten = MarkdownImageRegex.Replace(rewritten, match =>
            {
                var url = match.Groups["url"].Value.Trim();
                var alt = match.Groups["alt"].Value.Trim();
                if (string.IsNullOrWhiteSpace(url))
                {
                    return match.Value;
                }

                var safeUrl = System.Net.WebUtility.HtmlEncode(url);
                var safeAlt = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(alt) ? "Image" : alt);
                return $"<img src=\"{safeUrl}\" alt=\"{safeAlt}\" />";
            });

            return rewritten;
        }
    }
}
