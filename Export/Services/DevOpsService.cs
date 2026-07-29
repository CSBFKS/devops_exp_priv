using Export.Extensions;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Export.Services
{
    /// <summary>
    /// Azure DevOps service.
    /// </summary>
    public class DevOpsService : IDevOpsService
    {
        #region [ Properties ]

        /// <summary>
        /// Name of the query.
        /// </summary>
        public string QueryName
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
        /// Personal access token.
        /// </summary>
        public string Pat
        {
            get;
            private set;
        }


        /// <summary>
        /// Project name.
        /// </summary>
        public string Project
        {
            get;
            private set;
        }


        /// <summary>
        /// Url of the collection.
        /// </summary>
        public string Url
        {
            get;
            private set;
        }

        #endregion


        #region [ Methods : Public ]

        /// <summary>
        /// Sets the personal access token.
        /// </summary>
        /// <param name="pat">Personal access token.</param>
        /// <returns>Returns <see cref="IDevOpsService"/>.</returns>
        public IDevOpsService SetPat(string pat)
        {
            this.Pat = pat;

            return this;
        }


        /// <summary>
        /// Sets the project name.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <returns>Returns <see cref="IDevOpsService"/>.</returns>
        public IDevOpsService SetProject(string project)
        {
            this.Project = project;

            return this;
        }


        /// <summary>
        /// Sets the url of the collection.
        /// </summary>
        /// <param name="url">Url of the collection.</param>
        /// <returns>Returns <see cref="IDevOpsService"/>.</returns>
        public IDevOpsService SetUrl(string url)
        {
            this.Url = url;

            return this;
        }


        /// <summary>
        /// Gets the results of the query given the <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Query Id.</param>
        /// <returns>Returns list of work item ids.</returns>
        public async Task<List<int>> QueryByIdAsync(Guid id)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(this.Pat);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(this.Url);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(this.Project);
            ArgumentNullException.ThrowIfNull(id);

            using var client = new DevOpsClient(this.Pat, this.Url);

            // Get the query
            var query = await client.Get().GetQueryAsync(this.Project, id.ToString());
            this.QueryName = query.Name;

            // Get work item ids and split the result into groups of 200
            var results = (await client.Get().QueryByIdAsync(this.Project, id));
            var wits = (results.QueryType == QueryType.Flat) ?
                results.WorkItems.Select(w => w.Id).ToList() :
                results.WorkItemRelations.Select(w => w.Target.Id).ToList();

            // Store relations if neccessary
            if ((results.QueryType == QueryType.OneHop) || (results.QueryType == QueryType.Tree))
            {
                this.Relations = results.WorkItemRelations.ToList();
            }

            return wits;
        }


        /// <summary>
        /// Returns a list of work items.
        /// </summary>
        /// <param name="ids">List of work item ids.</param>
        /// <returns>Returns list of <see cref="WorkItem"/>.</returns>
        public async Task<List<WorkItem>> GetWorkItemsAsync(List<int> ids)
        {
            // Split ids by count of 200
            var groups = Enumerable
                .Range(0, ids.Count())
                .GroupBy(i => i / 200, i => ids[i]);

            using var client = new DevOpsClient(this.Pat, this.Url);

            var result = new List<WorkItem>(ids.Count);

            foreach (var g in groups)
            {
                if (g.Count() == 0)
                {
                    continue;
                }

                // Get work items for the ids found in query
                $"Downloading work {g.Count()} items [{string.Join(',', g.Take(5).ToArray())}, ...]".Debug();
                var wits = await client.Get().GetWorkItemsAsync(g.ToArray(), null, null, WorkItemExpand.All);
                "ok".Success().Eol();

                result.AddRange(wits.Where(w => w is not null).Cast<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem>());
            }

            return result;
        }


        /// <summary>
        /// Returns a list of work item comments.
        /// </summary>
        /// <param name="ids">List of work item ids.</param>
        /// <returns>.</returns>
        public async Task<Dictionary<int, List<WorkItemComment>>> GetCommentsAsync(List<int> ids)
        {
            var result = new Dictionary<int, List<WorkItemComment>>();

            using var client = new DevOpsClient(this.Pat, this.Url);
            foreach (int id in ids)
            {
                $"Downloading comments for work item #{id}".Debug();
                var comments = await client.Get().GetCommentsAsync(id);
                "ok".Success().Eol();

                result.Add(id, comments.Comments.ToList());
            }

            return result;
        }


        /// <summary>
        /// Downloadsall the attachemnts.
        /// </summary>
        /// <param name="wits">List of work items.</param>
        /// <param name="output">Output directory.</param>
        /// <returns>List of streams.</returns>
        public IEnumerable<(Task<Stream> Content, Models.Attachment Attachment)> DownloadAttachmentsAsync(List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> wits)
        {
            var workitems = wits.Where(wit => wit is not null).Select(wit => new Models.WorkItem(wit!));

            foreach (var w in workitems)
            {
                if (!w.Attachments.Any())
                {
                    continue;
                }
                
                $"Downloading attachments for work item #{w.Id}".Debug().Eol();
                using var client = new DevOpsClient(this.Pat, this.Url);
                foreach (var a in w.Attachments)
                {
                    a.Name.Indent().Debug();
                    Task<Stream>? stream = null;
                    try
                    {
                        stream = this.GetAttachmentContentWithRetryAsync(client, a.Id, a.Name);
                        "ok".Success().Eol();
                    }
                    catch (Exception ex)
                    {
                        "error".Error().Eol();
                        ex.Message.Error().Eol();
                    }
                    yield return (Content: stream, Attachment: a);
                }
            }
        }

        private async Task<Stream> GetAttachmentContentWithRetryAsync(DevOpsClient client, Guid id, string name)
        {
            const int maxAttempts = 3;
            Exception? lastException = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await client.Get().GetAttachmentContentAsync(id);
                }
                catch (Exception ex) when (attempt < maxAttempts && IsTransientNetworkException(ex))
                {
                    lastException = ex;
                    $"Retrying attachment download for '{name}' (attempt {attempt}/{maxAttempts})".Debug().Eol();
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }

            throw lastException ?? new IOException($"Failed to download attachment '{name}'.");
        }


        private static bool IsTransientNetworkException(Exception ex)
        {
            return ex is IOException or System.Net.Http.HttpRequestException or TimeoutException;
        }

        #endregion


        #region [ Constructors ]

        /// <summary>
        /// Constructor.
        /// </summary>
        public DevOpsService()
        {
            this.Relations = new List<WorkItemLink>();
        }

        #endregion
    }
}
