using System.Text.RegularExpressions;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using HandlebarsDotNet;

namespace Export.Services
{
    /// <summary>
    /// Output service.
    /// </summary>
    public partial class WriterService : IWriterService
    {
        #region [ Properties ]

        /// <summary>
        /// Work items that should be outputted.
        /// </summary>
        public List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> WorkItems
        {
            get;
            private set;
        }


        /// <summary>
        /// List of query's work item relations.
        /// </summary>
        public List<WorkItemLink> Relations
        {
            get;
            private set;
        }


        /// <summary>
        /// List of work items comments.
        /// </summary>
        public Dictionary<int, List<WorkItemComment>> Comments
        {
            get;
            private set;
        }


        /// <summary>
        /// Output directory.
        /// </summary>
        public DirectoryInfo OutputDirectory
        {
            get;
            private set;
        }


        /// <summary>
        /// Personal access token.
        /// </summary>
        public string Pat
        {
            get;
            private set;
        } = string.Empty;


        /// <summary>
        /// Azure DevOps collection URL.
        /// </summary>
        public string Url
        {
            get;
            private set;
        } = string.Empty;

        #endregion


        #region [ Methods : Public ]

        /// <summary>
        /// Sets the work items.
        /// </summary>
        /// <param name="workItems">Work items that should be outputted.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        public IWriterService SetWorkItems(List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> workItems)
        {
            this.WorkItems = workItems;

            return this;
        }


        /// <summary>
        /// Sets the query's work item relations.
        /// </summary>
        /// <param name="relations">List of query's work item relations.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        public IWriterService SetRelations(List<WorkItemLink> relations)
        {
            this.Relations = relations;

            return this;
        }


        /// <summary>
        /// Sets the work items comments.
        /// </summary>
        /// <param name="comments">List of work items comments</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        public IWriterService SetComments(Dictionary<int, List<WorkItemComment>> comments)
        {
            this.Comments = comments;

            return this;
        }


        /// <summary>
        /// Sets the output directory.
        /// </summary>
        /// <param name="outputDirectory">Output directory.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        public IWriterService SetOutputDirectory(DirectoryInfo outputDirectory)
        {
            this.OutputDirectory = outputDirectory;

            return this;
        }


        /// <summary>
        /// Sets the personal access token.
        /// </summary>
        /// <param name="pat">Personal access token.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        public IWriterService SetPat(string pat)
        {
            this.Pat = pat ?? string.Empty;

            return this;
        }


        /// <summary>
        /// Sets the Azure DevOps collection URL.
        /// </summary>
        /// <param name="url">Collection url address.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        public IWriterService SetUrl(string url)
        {
            this.Url = url ?? string.Empty;

            return this;
        }


        /// <summary>
        /// Writes the index file.
        /// </summary>
        /// <param name="title">Output's title.</param>
        /// <returns>Returns <see cref="Task"/>.</returns>
        public async Task WriteAsync(string title)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(title);
            ArgumentNullException.ThrowIfNull(this.WorkItems);
            ArgumentNullException.ThrowIfNull(this.OutputDirectory);

            var children = this.WorkItems
                .Where(wit => wit is not null)
                .Select(wit => new Models.WorkItem(wit!)
                {
                    Path = GetPaths(this.Relations, wit!.Id ?? -1).First()
                })
                .OrderByDescending(wit => wit.LastChangedDate ?? DateTimeOffset.MinValue)
                .ToList();

            var overviewGroups = children
                .GroupBy(wit => wit.StateCategory)
                .Select(group => new
                {
                    Category = group.Key,
                    AnchorId = GetAnchorId(group.Key),
                    Items = group.OrderByDescending(wit => wit.LastChangedDate ?? DateTimeOffset.MinValue)
                        .Select(wit => new
                        {
                            Id = wit.Id,
                            Title = wit.Title,
                            State = wit.State,
                            Category = string.IsNullOrWhiteSpace(wit.Category) ? wit.StateCategory : wit.Category,
                            Folder = wit.Folder,
                            LastChangedDate = wit.LastChangedDateDisplay,
                            LastChangedBy = wit.LastChangedBy,
                            CommentCount = wit.CommentsCount,
                            AttachmentsCount = wit.Attachments.Count,
                            Tags = wit.Tags
                        })
                        .ToList()
                })
                .OrderBy(group => group.Category.Equals("On hold", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(group => group.Category, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var template = Handlebars.Compile(WriterService.IndexTemplate);
            var result = template(new
            {
                Title = title,
                Children = children,
                OverviewGroups = overviewGroups
            });

            using var file = new StreamWriter(Path.Combine(this.OutputDirectory.FullName, WriterService.IndexFile), false);
            await file.WriteAsync(result);

            foreach (var child in children)
            {
                await this.WriteWorkItemAsync(child);
            }

            return;
        }


        /// <summary>
        /// Writes the <paramref name="wit"/> work item file.
        /// </summary>
        /// <param name="wit">Work item to be written.</param>
        /// <returns>Returns <see cref="Task"/>.</returns>
        public async Task WriteWorkItemAsync(Models.WorkItem wit)
        {
            var folder = Path.Combine(this.OutputDirectory.FullName, wit.Folder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var comments = this.Comments.TryGetValue(wit.Id ?? -1, out var workItemComments)
                ? workItemComments
                : new List<WorkItemComment>();

            var imageService = new EmbeddedImageService(this.Pat, this.Url, this);
            var embeddedImages = new Dictionary<Guid, string>();

            var descriptionImages = imageService.ExtractAndDownloadImages(wit.Description);
            foreach (var image in descriptionImages)
            {
                embeddedImages[image.Key] = image.Value;
            }

            var reproStepsImages = imageService.ExtractAndDownloadImages(wit.ReproSteps);
            foreach (var image in reproStepsImages)
            {
                embeddedImages[image.Key] = image.Value;
            }

            wit.DescriptionHtml = HtmlImageRewriter.RewriteAttachmentUrls(wit.Description, embeddedImages);
            wit.ReproStepsHtml = HtmlImageRewriter.RewriteAttachmentUrls(wit.ReproSteps, embeddedImages);

            var processedComments = comments
                .Select(c =>
                {
                    var commentImages = imageService.ExtractAndDownloadImages(c.Text);
                    foreach (var image in commentImages)
                    {
                        embeddedImages[image.Key] = image.Value;
                    }

                    return new
                    {
                        Text = HtmlImageRewriter.RewriteAttachmentUrls(c.Text, embeddedImages),
                        EmbeddedImages = commentImages
                            .Select(image => new
                            {
                                FileName = image.Value,
                                Name = Path.GetFileName(image.Value)
                            })
                            .ToList(),
                        CreatedDate = c.RevisedDate != default ? c.RevisedDate.ToString("yyyy-MM-dd HH:mm") : "—",
                        CreatedBy = GetCommentAuthor(c)
                    };
                })
                .ToList();

            var descriptionImageLinks = descriptionImages
                .Select(image => new
                {
                    FileName = image.Value,
                    Name = Path.GetFileName(image.Value)
                })
                .ToList();

            var reproStepsImageLinks = reproStepsImages
                .Select(image => new
                {
                    FileName = image.Value,
                    Name = Path.GetFileName(image.Value)
                })
                .ToList();

            var template = Handlebars.Compile(WriterService.WorkItemTemplate);
            var result = template(new
            {
                WorkItem = wit,
                Comments = processedComments,
                DescriptionImages = descriptionImageLinks,
                ReproStepsImages = reproStepsImageLinks
            });
            using var file = new StreamWriter(Path.Combine(folder, WriterService.IndexFile), false);
            await file.WriteAsync(result);
        }


        /// <summary>
        /// Writes the <paramref name="content"/> file.
        /// </summary>
        /// <param name="attachment">Attachment's model.</param>
        /// <param name="content">Attachment's content.</param>
        /// <returns>Returns <see cref="Task"/>.</returns>
        public async Task WriteAttachmentAsync(Models.Attachment attachment, Stream content)
        {
            // Ensure folder
            var folder = Path.Combine(this.OutputDirectory.FullName, ".attachments");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var path = Path.Combine(folder, attachment.FileName);
            using var writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            await content.CopyToAsync(writer);
        }

        #endregion


        #region [ Methods : Private ]

        /// <summary>
        /// Gets a display-friendly author name for a comment.
        /// </summary>
        /// <param name="comment">Comment.</param>
        /// <returns>Author display name.</returns>
        private static string GetCommentAuthor(WorkItemComment comment)
        {
            if (comment.RevisedBy is null)
            {
                return "Unknown";
            }

            if (!string.IsNullOrWhiteSpace(comment.RevisedBy.DisplayName))
            {
                return comment.RevisedBy.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(comment.RevisedBy.UniqueName))
            {
                return comment.RevisedBy.UniqueName;
            }

            return "Unknown";
        }

        private static string GetAnchorId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "overview";
            }

            var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-");
            normalized = Regex.Replace(normalized, "-{2,}", "-").Trim('-');

            return string.IsNullOrWhiteSpace(normalized) ? "overview" : normalized;
        }

        /// <summary>
        /// Gets the list of paths
        /// </summary>
        /// <param name="relations">List of query's work item relations</param>
        /// <param name="id">Work item Id.</param>
        private static string[] GetPaths(List<WorkItemLink> relations, int id)
        {
            var itm = relations.Where(r => r.Target.Id == id).ToList();

            if ((itm.Count == 0) || ((itm.Count == 1) && (itm[0].Source == null)))
            {
                return new []
                {
                    id.ToString()
                };
            }

            return itm.Select(i => string.Join("|", GetPaths(relations, i.Source.Id).Select(p => $"{p}/{id}"))).ToArray();
        }

        #endregion
    }
}
