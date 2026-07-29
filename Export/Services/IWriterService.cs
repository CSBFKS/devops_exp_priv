using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Export.Services
{
    /// <summary>
    /// Interface which defines Output service functionality.
    /// </summary>
    public interface IWriterService
    {
        #region [ Properties ]

        /// <summary>
        /// Work items that should be outputted.
        /// </summary>
        List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> WorkItems
        {
            get;
        }


        /// <summary>
        /// List of query's work item relations.
        /// </summary>
        List<WorkItemLink> Relations
        {
            get; 
        }


        /// <summary>
        /// List of work items comments.
        /// </summary>
        Dictionary<int, List<WorkItemComment>> Comments
        {
            get;
        }


        /// <summary>
        /// Output directory.
        /// </summary>
        DirectoryInfo OutputDirectory
        {
            get;
        }


        /// <summary>
        /// Personal access token.
        /// </summary>
        string Pat
        {
            get;
        }


        /// <summary>
        /// Azure DevOps collection URL.
        /// </summary>
        string Url
        {
            get;
        }

        #endregion


        #region [ Methods ]

        /// <summary>
        /// Sets the work items.
        /// </summary>
        /// <param name="workItems">Work items that should be outputted.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        IWriterService SetWorkItems(List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> workItems);


        /// <summary>
        /// Sets the query's work item relations.
        /// </summary>
        /// <param name="relations">List of query's work item relations.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        IWriterService SetRelations(List<WorkItemLink> relations);


        /// <summary>
        /// Sets the work items comments.
        /// </summary>
        /// <param name="comments">List of work items comments</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        IWriterService SetComments(Dictionary<int, List<WorkItemComment>> comments);


        /// <summary>
        /// Sets the output directory.
        /// </summary>
        /// <param name="outputDirectory">Output directory.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        IWriterService SetOutputDirectory(DirectoryInfo outputDirectory);


        /// <summary>
        /// Sets the personal access token.
        /// </summary>
        /// <param name="pat">Personal access token.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        IWriterService SetPat(string pat);


        /// <summary>
        /// Sets the Azure DevOps collection URL.
        /// </summary>
        /// <param name="url">Collection url address.</param>
        /// <returns>Returns <see cref="IWriterService"/>.</returns>
        IWriterService SetUrl(string url);


        /// <summary>
        /// Writes the index file.
        /// </summary>
        /// <param name="title">Output's title.</param>
        /// <returns>Returns <see cref="Task"/>.</returns>
        Task WriteAsync(string title);


        /// <summary>
        /// Writes the <paramref name="wit"/> work item file.
        /// </summary>
        /// <param name="wit">Work item to be written.</param>
        /// <returns>Returns <see cref="Task"/>.</returns>
        Task WriteWorkItemAsync(Models.WorkItem wit);



        /// <summary>
        /// Writes the <paramref name="content"/> file.
        /// </summary>
        /// <param name="attachment">Attachment's model.</param>
        /// <param name="content">Attachment's content.</param>
        /// <returns>Returns <see cref="Task"/>.</returns>
        Task WriteAttachmentAsync(Models.Attachment attachment, Stream content);

        #endregion
    }
}
