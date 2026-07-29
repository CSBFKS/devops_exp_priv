namespace Export.Services
{
    /// <summary>
    /// Provides embedded image extraction helpers.
    /// </summary>
    public interface IEmbeddedImageService
    {
        /// <summary>
        /// Extracts image references from HTML and downloads the corresponding attachment content.
        /// </summary>
        /// <param name="html">HTML content.</param>
        /// <returns>Dictionary keyed by attachment id and value as local file name.</returns>
        IReadOnlyDictionary<Guid, string> ExtractAndDownloadImages(string? html);
    }
}
