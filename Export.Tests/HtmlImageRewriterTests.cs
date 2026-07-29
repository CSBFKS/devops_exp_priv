using Export.Services;
using Export.Models;
using Xunit;

namespace Export.Tests;

public class HtmlImageRewriterTests
{
    [Fact]
    public void RewriteAttachmentUrls_ConvertsMarkdownImageSyntaxToHtmlImages()
    {
        var html = "![image.png](../.attachments/image.png)";
        var attachmentFiles = new Dictionary<Guid, string>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = "image.png"
        };

        var result = HtmlImageRewriter.RewriteAttachmentUrls(html, attachmentFiles);

        Assert.Contains("<img", result);
        Assert.Contains("src=\"../.attachments/image.png\"", result);
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

        Assert.Contains("alt=\"My &quot;quoted&quot; image\"", result);
        Assert.Contains("src=\"../.attachments/sample &amp; file.png\"", result);
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
