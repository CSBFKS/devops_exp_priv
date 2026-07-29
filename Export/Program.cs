using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Export.Services;
using Export.Extensions;

namespace Export
{
    /// <summary>
    /// Main program.
    /// 
    /// <a href="https://learn.microsoft.com/en-us/dotnet/standard/commandline">System.CommandLine overview</a>
    /// <a href="https://github.com/microsoft/azure-devops-dotnet-samples/blob/main/ClientLibrary/Samples/WorkItemTracking/AttachmentsSample.cs">AttachmentsSample.cs</a>
    /// </summary>
    public class Program
    {
        #region [ Methods : Public ]

        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">Input arguments.</param>
        /// <returns>Returns status code.</returns>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
                // Ignore when the process is running without a console.
            }

            var services = new ServiceCollection()
                .AddSingleton<IDevOpsService, DevOpsService>()
                .AddSingleton<IWriterService, WriterService>()
                .BuildServiceProvider();

            var options = GetOptions();

            var cmd = new RootCommand("Downloads work items and saves them into the target folder");
            cmd.AddOption(options.Url);
            cmd.AddOption(options.Token);
            cmd.AddOption(options.Project);
            cmd.AddOption(options.QueryId);
            cmd.AddOption(options.Output);

            cmd.SetHandler(async (url, token, project, queryId, output) =>
            {
                // Setup devops service
                "Configuring service".Debug();
                var devops = services
                    .GetRequiredService<IDevOpsService>()
                    .SetPat(token)
                    .SetUrl(url)
                    .SetProject(project);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                "ok".Success().Eol();

                // Download query
                $"Downloading query '{queryId}'".Debug();
                var ids = await devops.QueryByIdAsync(queryId);
                "ok".Success().Eol();

                // Download work items
                var wits = await devops.GetWorkItemsAsync(ids);
                
                // Download work item comments
                var comments = await devops.GetCommentsAsync(ids);

                // Setup writer service
                ArgumentNullException.ThrowIfNull(output);

                var writer = services
                    .GetRequiredService<IWriterService>()
                    .SetOutputDirectory(output)
                    .SetPat(token)
                    .SetUrl(url)
                    .SetWorkItems(wits)
                    .SetRelations(devops.Relations)
                    .SetComments(comments);

                // Download and save attachments
                foreach (var task in devops.DownloadAttachmentsAsync(wits))
                {
                    using var content = await task.Content;
                    await writer.WriteAttachmentAsync(task.Attachment, content);
                }

                // Generate output
                $"Creating output".Debug();
                await writer.WriteAsync(devops.QueryName);
                "ok".Success().Eol();
            }, options.Url, options.Token, options.Project, options.QueryId, options.Output);

            var builder = new CommandLineBuilder(cmd)
                .UseExceptionHandler(OnException)
                .UseDefaults()
                .UseHelp(Help)
                .Build();

            return await builder.InvokeAsync(args.Length > 0 ? args : new string[] { "-h" });
        }

        #endregion


        #region [ Methods : Private ]

        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <returns>Returns options.</returns>
        private static (Option<string> Token, Option<string> Url, Option<string> Project, Option<Guid> QueryId, Option<DirectoryInfo?> Output) GetOptions()
        {
            var tokenOption = new Option<string>(
                name: "--token",
                description: "Personal access token")
            {
                IsRequired = true
            };

            var urlOption = new Option<string>(
                name: "--url",
                description: "Collection url address")
            {
                IsRequired = true
            };

            var projectOption = new Option<string>(
                name: "--project",
                description: "Project name")
            {
                IsRequired = true
            };

            var queryIdOption = new Option<Guid>(
                name: "--queryId",
                description: "Query Id")
            {
                IsRequired = true
            };

            var outputOption = new Option<DirectoryInfo?>(
                name: "--output",
                description: "Output directory",
                parseArgument: ParseDirectoryArgument)
            {
                IsRequired = true
            };

            return (
                Token: tokenOption,
                Url: urlOption,
                Project: projectOption,
                QueryId: queryIdOption,
                Output: outputOption);
        }


        /// <summary>
        /// Customizes help.
        /// </summary>
        /// <param name="ctx">Help context.</param>
        private static void Help(HelpContext ctx)
        {
            ctx.HelpBuilder.CustomizeLayout(_ =>
            {
                var assembly = Assembly.GetExecutingAssembly().GetName();

                return HelpBuilder.Default
                    .GetLayout()
                    .Prepend(helpContext =>
                    {
                        helpContext.Output.WriteLine($"{assembly?.Name} v{assembly?.Version}");
                    });
            });
        }


        /// <summary>
        /// On exception
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="context">Invocation context</param>
        private static void OnException(Exception ex, InvocationContext context)
        {
            //Log.Fatal("Exception occurred during process initialization.", ex);
            Environment.Exit(1);
        }


        /// <summary>
        /// Parses the output directory argument result and checks if the specified directory exists.
        /// </summary>
        /// <param name="result">Argument.</param>
        /// <returns>Returns FileInfo or null.</returns>
        private static DirectoryInfo? ParseDirectoryArgument(ArgumentResult result)
        {
            var dirPath = result.Tokens.Single().Value;

            if (Directory.Exists(dirPath))
            {
                return new DirectoryInfo(dirPath);
            }

            result.ErrorMessage = $"Directory '{dirPath}' does not exist";
            return null;
        }

        #endregion
    }
}
