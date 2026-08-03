using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;

namespace Export.Models
{
    /// <summary>
    /// Describes a work item.
    /// </summary>
    public partial class WorkItem : Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem
    {
        #region [ Properties ]

        /// <summary>
        /// Path in the query.
        /// </summary>
        public string Path
        {
            get;
            set;
        }


        /// <summary>
        /// The level of the work item in the query.
        /// </summary>
        public int Level
        {
            get => this.Path?.Split("/").Length ?? 0;
        }


        /// <summary>
        /// Title.
        /// </summary>
        public string Title
        {
            get => (string)this.Fields.GetValueOrDefault(Models.Fields.Title, string.Empty);
        }


        /// <summary>
        /// Description.
        /// </summary>
        public string Description
        {
            get => (string)this.Fields.GetValueOrDefault(Models.Fields.Description, string.Empty);
        }


        /// <summary>
        /// Repro steps.
        /// </summary>
        public string ReproSteps
        {
            get => (string)this.Fields.GetValueOrDefault(Models.Fields.ReproSteps, string.Empty);
        }


        /// <summary>
        /// Processed description HTML for rendering.
        /// </summary>
        public string DescriptionHtml
        {
            get;
            set;
        }


        /// <summary>
        /// Processed repro steps HTML for rendering.
        /// </summary>
        public string ReproStepsHtml
        {
            get;
            set;
        }


        /// <summary>
        /// Tags.
        /// </summary>
        public string[] Tags
        {
            get => ((string)this.Fields.GetValueOrDefault(Models.Fields.Tags, string.Empty)).Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
        }


        /// <summary>
        /// Node name or area.
        /// </summary>
        public string NodeName
        {
            get => (string)this.Fields.GetValueOrDefault(Models.Fields.NodeName, string.Empty);
        }
        
        
        /// <summary>
        /// State.
        /// </summary>
        public string State
        {
            get => (string)this.Fields.GetValueOrDefault(Models.Fields.State, string.Empty);
        }


        /// <summary>
        /// Itteration.
        /// </summary>
        public string IterationPath
        {
            get => string.Join(@"\", ((string)this.Fields.GetValueOrDefault(Models.Fields.IterationPath, string.Empty)).Split(@"\").Skip(1).Select(i => i));
        }


        /// <summary>
        /// Last change timestamp.
        /// </summary>
        public DateTimeOffset? LastChangedDate
        {
            get;
            private set;
        }


        /// <summary>
        /// Display string for the last change timestamp.
        /// </summary>
        public string LastChangedDateDisplay
        {
            get => this.LastChangedDate.HasValue ? this.LastChangedDate.Value.ToString("yyyy-MM-dd HH:mm") : "—";
        }


        /// <summary>
        /// Last change user.
        /// </summary>
        public string LastChangedBy
        {
            get;
            private set;
        } = string.Empty;


        /// <summary>
        /// Explicit work item category from the field set, if present.
        /// </summary>
        public string Category
        {
            get
            {
                var category = (string?)this.Fields.GetValueOrDefault(Models.Fields.Category, string.Empty);
                if (!string.IsNullOrWhiteSpace(category))
                {
                    return category;
                }

                return this.StateCategory;
            }
        }


        /// <summary>
        /// Overview category for sorting and grouping.
        /// </summary>
        public string StateCategory
        {
            get => GetStateCategory(this.State);
        }


        /// <summary>
        /// Comments count.
        /// </summary>
        public long CommentsCount
        {
            get => (long)this.Fields.GetValueOrDefault(Models.Fields.CommentCount, 0);
        }


        /// <summary>
        /// Name of the work item folder.
        /// </summary>
        public string Folder
        {
            get => $"{this.Id}_{Regex.Replace(Regex.Replace(ToAccentInsensitive(this.Title).ToLower(), "[^a-zA-Z0-9]", "-"), "-{2,}", "-")}";
        }


        /// <summary>
        /// List of attachment relations.
        /// </summary>
        public List<Models.Attachment> Attachments
        {
            get;
            private set;
        }

        #endregion


        #region [ Methods : Private ]

        /// <summary>
        /// Gets accent insensitive string.
        /// </summary>
        /// <param name="value">String to be proccesed.</param>
        /// <returns>Accent insensitive string.</returns>
        private static string ToAccentInsensitive(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Maps the work item state to a grouping category.
        /// </summary>
        /// <param name="state">State value.</param>
        /// <returns>Category label.</returns>
        private static string GetStateCategory(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return "Other";
            }

            var normalized = state.Trim().ToLowerInvariant();
            if (normalized.Contains("hold") || normalized.Contains("blocked") || normalized.Contains("waiting"))
            {
                return "On hold";
            }

            if (normalized.Contains("done") || normalized.Contains("closed") || normalized.Contains("resolved"))
            {
                return "Done";
            }

            if (normalized.Contains("active") || normalized.Contains("new") || normalized.Contains("approved"))
            {
                return "Active";
            }

            return state.Trim();
        }

        #endregion


        #region [ Constructors ]

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source work item.</param>
        public WorkItem(Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem? source)
        {
            if (source is null)
            {
                this.Fields = new Dictionary<string, object>();
                this.Relations = new List<WorkItemRelation>();
                this.Url = string.Empty;
                this.Attachments = new List<Models.Attachment>();
                this.DescriptionHtml = string.Empty;
                this.ReproStepsHtml = string.Empty;
                return;
            }

            this.Id = source.Id;
            this.Fields = source.Fields ?? new Dictionary<string, object>();
            this.Relations = source.Relations ?? new List<WorkItemRelation>();
            this.Url = source.Url ?? string.Empty;
            this.Attachments = source.Relations?.Where(r => r.Rel == "AttachedFile").Select(a => new Models.Attachment(a)).ToList() ?? new List<Models.Attachment>();
            this.DescriptionHtml = this.Description;
            this.ReproStepsHtml = this.ReproSteps;

            if (source.Fields?.TryGetValue(Models.Fields.ChangedDate, out var changedDate) == true)
            {
                this.LastChangedDate = changedDate switch
                {
                    DateTimeOffset dto => dto,
                    DateTime dt => new DateTimeOffset(dt),
                    _ => null
                };
            }

            if (source.Fields?.TryGetValue(Models.Fields.ChangedBy, out var changedBy) == true)
            {
                this.LastChangedBy = changedBy?.ToString() ?? string.Empty;
            }
        }

        #endregion
    }
}
