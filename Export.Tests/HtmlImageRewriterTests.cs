using Export.Services;
using Export.Models;
using Xunit;

namespace Export.Tests;

public class HtmlImageRewriterTests
{
    [Fact]
    public void RewriteAttachmentUrls_ConvertsMarkdownImageSyntaxToLinks()
    {
        var html = "![image.png](../.attachments/image.png)";
        var attachmentFiles = new Dictionary<Guid, string>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = "image.png"
        };

        var result = HtmlImageRewriter.RewriteAttachmentUrls(html, attachmentFiles);

        Assert.Contains("<a href=\"../.attachments/image.png\">image.png</a>", result);
        Assert.DoesNotContain("![image.png]", result);
    }

    [Fact]
    public void RewriteAttachmentUrls_EscapesHtmlCharactersInImageAttributes()
    {
        var html = "![My \"quoted\" image](https://example.test/_apis/wit/attachments/11111111-1111-1111-1111-111111111111?fileName=sample&file=1.png)";
        var attachmentFiles = new Dictionary<Guid, string>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = "sample & file.png"
        };

        var result = HtmlImageRewriter.RewriteAttachmentUrls(html, attachmentFiles);

        Assert.Contains("href=\"../.attachments/sample &amp; file.png\"", result);
    }

    [Fact]
    public void WorkItem_ReproSteps_ReturnsConfiguredFieldValue()
    {
        var source = new Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem
        {
            Fields = new Dictionary<string, object>
            {
                [Fields.ReproSteps] = "1. Open the app"
            }
        };

        var workItem = new WorkItem(source);

        Assert.Equal("1. Open the app", workItem.ReproSteps);
    }

    [Fact]
    public void RewriteAttachmentUrls_ReplacesGenericPlaceholderImagePathsWithDownloadedFiles()
    {
        var html = "<img src=\"../.attachments/image.png\" alt=\"Image\"> <img src=\"../.attachments/image.png\" alt=\"Image\">";
        var attachmentFiles = new Dictionary<Guid, string>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = "first-image.png",
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = "second-image.png"
        };

        var result = HtmlImageRewriter.RewriteAttachmentUrls(html, attachmentFiles);

        Assert.Contains("../.attachments/first-image.png", result);
        Assert.Contains("../.attachments/second-image.png", result);
        Assert.DoesNotContain("../.attachments/image.png", result);
    }

    [Fact]
    public void Attachment_FileName_UsesConfiguredNameWhenProvided()
    {
        var attachment = new Attachment(new Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemRelation
        {
            Url = "https://dev.azure.com/example/_apis/wit/attachments/11111111-1111-1111-1111-111111111111",
            Attributes = new Dictionary<string, object>
            {
                ["name"] = "my-image.png"
            }
        });

        Assert.Equal("my-image.png", attachment.FileName);
    }

    [Fact]
    public void WorkItem_Category_ReturnsConfiguredCategoryFieldValue()
    {
        var source = new Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem
        {
            Fields = new Dictionary<string, object>
            {
                [Fields.State] = "Active",
                [Fields.Category] = "Bug"
            }
        };

        var workItem = new WorkItem(source);

        Assert.Equal("Bug", workItem.Category);
    }

    [Fact]
    public void WorkItem_StateCategory_ReturnsOnHoldForHoldStates()
    {
        var source = new Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem
        {
            Fields = new Dictionary<string, object>
            {
                [Fields.State] = "On Hold"
            }
        };

        var workItem = new WorkItem(source);

        Assert.Equal("On hold", workItem.StateCategory);
    }

    [Fact]
    public void IsTransientNetworkException_RecognizesCommonNetworkErrors()
    {
        var ex = new IOException("connection reset");
        var service = new DevOpsService();

        var method = typeof(DevOpsService).GetMethod("IsTransientNetworkException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, new object[] { ex })!;

        Assert.True(result);
    }
}
