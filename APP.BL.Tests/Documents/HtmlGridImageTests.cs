using App.BL.AIAgent.GenericAgent.Documents;

namespace APP.BL.Tests.Documents;

public class HtmlGridImageTests
{
    [Theory]
    [InlineData("/api/resources/Company_5047/Image/regular/abc123")]
    [InlineData("/api/resources/Company_5047/image/thumbnail/abc123")]
    [InlineData("https://cdn.example.com/a.png")]
    public void IsImageValue_detects_resource_urls(string url)
    {
        Assert.True(CardSheet.IsImageValue(url));
    }

    [Fact]
    public void HtmlForm_grid_writes_img_for_sketch_url()
    {
        var url = "/api/resources/Company_5047/Image/regular/deadbeef";
        var doc = FormDocument.FromTable(
            "Styles",
            new[] { "ReferenceId", "Sketch", "Article" },
            new[]
            {
                new List<FormCell>
                {
                    new FormCell { Text = "1545", IsNumber = true },
                    new FormCell { Text = url, IsImage = true },
                    new FormCell { Text = "MC-PD-50-J001" }
                }
            });

        var html = HtmlForm.Write(doc);
        Assert.Contains("<img src=\"" + url + "\"", html);
        Assert.Contains("data-role=\"image\"", html);
        Assert.DoesNotContain(">" + url + "<", html);
    }
}
