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

    [Theory]
    [InlineData("Image", "/image/300")]
    [InlineData("Sketch", "https://plm.example.com/style/image/300")]
    [InlineData("Photo", "12345")]
    public void IsImageValue_field_name_plus_url_or_file_id(string field, string value)
    {
        Assert.True(CardSheet.IsImageValue(value, field));
        Assert.True(CardSheet.IsRenderableImage(value, field, flaggedIsImage: false));
    }

    [Fact]
    public void IsImageValue_plm_path_without_field_name_is_not_image()
    {
        Assert.False(CardSheet.IsImageValue("/image/300"));
    }

    [Theory]
    [InlineData("2026-06-24T00:00:00", "Jun 24, 2026")]
    [InlineData("2026-06-04", "Jun 4, 2026")]
    [InlineData("Approved", "Approved")]
    public void FormatDisplayValue_short_dates(string input, string expected)
    {
        Assert.Equal(expected, CardSheet.FormatDisplayValue(input));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 2, 1)]
    [InlineData(4, 2, 2)]
    [InlineData(6, 3, 2)]
    [InlineData(9, 3, 3)]
    [InlineData(29, 3, 3)]
    public void SuggestPrintGrid_catalog_tiles(int count, int cols, int rows)
    {
        CardSheet.SuggestPrintGrid(count, out var c, out var r);
        Assert.Equal(cols, c);
        Assert.Equal(rows, r);
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

    [Fact]
    public void HtmlForm_grid_and_cards_prefer_embedded_image_bytes()
    {
        // Minimal 1×1 PNG
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var plm = "https://plm.example.com/style/image/300";
        var doc = FormDocument.FromTable(
            "Styles",
            new[] { "Image", "Name" },
            new[]
            {
                new List<FormCell>
                {
                    new FormCell { Text = plm, IsImage = true, ImageBytes = png },
                    new FormCell { Text = "A" }
                }
            });
        var gridHtml = HtmlForm.Write(doc);
        Assert.Contains("data:image/png;base64,", gridHtml);
        Assert.DoesNotContain(plm, gridHtml);

        var sheet = new CardSheet { Title = "Cards", CardsPerRow = 2, CardRows = 2 };
        sheet.Cards.Add(new List<CardField>
        {
            new CardField { Label = "Image", Value = plm, IsImage = true, ImageBytes = png, Role = "primaryImage" },
            new CardField { Label = "Name", Value = "A", Role = "title" }
        });
        var cardHtml = HtmlForm.WriteCards(sheet, forPrint: true);
        Assert.Contains("data:image/png;base64,", cardHtml);
        Assert.Contains("mcp-card-hero", cardHtml);
    }
}
