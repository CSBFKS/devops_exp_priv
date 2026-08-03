using System.Text;
using System.Text.RegularExpressions;

namespace Export.Services
{
    /// <summary>
    /// Rewrites Azure DevOps attachment image URLs to local file paths.
    /// </summary>
    public static partial class HtmlImageRewriter
    {
        private static readonly Regex AttachmentUrlRegex = new(
            @"(?<url>https?://[^\s""'()]+/_apis/wit/attachments/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\?fileName=[^\s""'()]*)?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            var placeholderIndex = 0;
            var attachmentFileNames = attachmentFiles.Values.ToList();
            var withGenericPlaceholders = Regex.Replace(rewritten, @"\./\.attachments/image\.png|\.\./\.attachments/image\.png", match =>
            {
                if (placeholderIndex < attachmentFileNames.Count)
                {
                    var fileName = attachmentFileNames[placeholderIndex++];
                    return $"../.attachments/{fileName}";
                }

                return match.Value;
            });

            return RewriteMarkdownImages(withGenericPlaceholders, attachmentFiles);
        }

        private static string RewriteMarkdownImages(string html, IReadOnlyDictionary<Guid, string> attachmentFiles)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(html.Length);
            for (var i = 0; i < html.Length; i++)
            {
                if (html[i] == '!' && i + 1 < html.Length && html[i + 1] == '[')
                {
                    var altStart = i + 2;
                    var altEnd = html.IndexOf(']', altStart);
                    if (altEnd > altStart && altEnd + 1 < html.Length && html[altEnd + 1] == '(')
                    {
                        var urlStart = altEnd + 2;
                        var urlEnd = FindClosingParenthesis(html, urlStart);
                        if (urlEnd > urlStart)
                        {
                            var alt = html.Substring(altStart, altEnd - altStart).Trim();
                            var url = html.Substring(urlStart, urlEnd - urlStart).Trim();
                            var attachmentUrl = ResolveAttachmentUrl(url, attachmentFiles);
                            var safeUrl = System.Net.WebUtility.HtmlEncode(attachmentUrl);
                            var displayText = string.IsNullOrWhiteSpace(alt) ? "Image" : alt;
                            builder.Append($"<a href=\"{safeUrl}\">{displayText}</a>");
                            i = urlEnd;
                            continue;
                        }
                    }
                }

                builder.Append(html[i]);
            }

            return builder.ToString();
        }

        private static int FindClosingParenthesis(string html, int startIndex)
        {
            var depth = 0;
            for (var i = startIndex; i < html.Length; i++)
            {
                if (html[i] == '(')
                {
                    depth++;
                }
                else if (html[i] == ')')
                {
                    if (depth == 0)
                    {
                        return i;
                    }

                    depth--;
                }
            }

            return -1;
        }

        private static string ResolveAttachmentUrl(string url, IReadOnlyDictionary<Guid, string> attachmentFiles)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            if (url.StartsWith("../.attachments/", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var attachmentMatch = AttachmentUrlRegex.Match(url);
            if (attachmentMatch.Success && Guid.TryParse(attachmentMatch.Groups["id"].Value, out var attachmentId) && attachmentFiles.TryGetValue(attachmentId, out var fileName))
            {
                return $"../.attachments/{fileName}";
            }

            return url;
        }
    }
}
