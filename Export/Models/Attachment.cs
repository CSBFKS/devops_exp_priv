using Microsoft.VisualStudio.Services.Common;

namespace Export.Models
{
    /// <summary>
    /// Describes an attachment.
    /// </summary>
    public class Attachment : Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemRelation
    {
        #region [ Properties ]

        /// <summary>
        /// Collection of link attributes.
        /// </summary>
        public Guid Id
        {
            get => new Guid(this.Url.ToString().Split("/").Last());
        }


        /// <summary>
        /// Attachment's name.
        /// </summary>
        public string Name
        {
            get => (string)this.Attributes.GetValueOrDefault("name", string.Empty);
        }


        /// <summary>
        /// Attachment's file name.
        /// </summary>
        public string FileName
        {
            get
            {
                if (this.Attributes.TryGetValue("name", out var name) && name is string configuredName && !string.IsNullOrWhiteSpace(configuredName))
                {
                    return Path.GetFileName(configuredName);
                }

                return $"{this.Id.ToString()}{Path.GetExtension(this.Name)}";
            }
        }

        #endregion



        #region [ Constructors ]

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source work item relation.</param>
        public Attachment(Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemRelation source)
        {
            this.Attributes = source.Attributes;
            this.Url = source.Url;
        }

        #endregion
    }
}
