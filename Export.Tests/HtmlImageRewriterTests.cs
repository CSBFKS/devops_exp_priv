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

        Assert.Contains("My &quot;quoted&quot; image", result);
        Assert.Contains("href=\"../.attachments/sample &amp; file.png\"", result);
    }

    [Fact]
    public void RewriteAttachmentUrls_ConvertsHtmlImageTagsToLinks()
    {
        var html = "<div><img src=\"https://example.test/_apis/wit/attachments/11111111-1111-1111-1111-111111111111?fileName=sample.png\" alt=\"Sample image\"></div>";
        var attachmentFiles = new Dictionary<Guid, string>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = "sample.png"
        };

        var result = HtmlImageRewriter.RewriteAttachmentUrls(html, attachmentFiles);

        Assert.Contains("<a href=\"../.attachments/sample.png\">Sample image</a>", result);
        Assert.DoesNotContain("<img", result);
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
    public void WorkItem_Category_ReturnsSystemCategoryWhenAvailable()
    {
        var source = new Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem
        {
            Fields = new Dictionary<string, object>
            {
                [Fields.Category] = "Requirement",
                [Fields.WorkItemType] = "Task"
            }
        };

        var workItem = new WorkItem(source);

        Assert.Equal("Requirement", workItem.Category);
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
