using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Web.Helpers;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/opengraph")]
[Tags("Link preview")]
public class OpenGraphController : ControllerBase
{
    private const int ImageWidth = 1200;
    private const int ImageHeight = 600;
    private const float CardTop = 180;
    private const float CardWidth = 340;
    private const float CardHeight = 360;
    private const float CardCornerRadius = 16;
    private const float CardPadding = 30;
    private const float PingCardLeft = 60;
    private const float DownloadCardLeft = 430;
    private const float UploadCardLeft = 800;

    private readonly ResultsService _results;

    public OpenGraphController(ResultsService results)
    {
        _results = results;
    }

    private sealed record CardStyle(SKPaint Fill, SKPaint Border, SKFont LabelFont, SKPaint LabelPaint, SKFont ValueFont);

    /// <summary>
    /// Get the link-preview image
    /// </summary>
    /// <remarks>
    /// A 1200 by 600 PNG with the latest completed test's ping, download and upload, for chat apps and social sites that show a preview of a shared link.
    /// </remarks>
    /// <response code="200">The image.</response>
    [HttpGet("image")]
    [Authorize(Policy = AccessPolicies.Read)]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/png")]
    public async Task<IActionResult> GetImage(CancellationToken cancellationToken)
    {
        var preview = LinkPreview.For(await _results.GetLatestCompletedAsync(cancellationToken));

        using var surface = SKSurface.Create(new SKImageInfo(ImageWidth, ImageHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColor.Parse("#0f1419"));

        using var cardFill = new SKPaint();
        cardFill.Color = SKColor.Parse("#1a2029");
        cardFill.Style = SKPaintStyle.Fill;
        using var cardBorder = new SKPaint();
        cardBorder.Color = SKColor.Parse("#2a3441");
        cardBorder.Style = SKPaintStyle.Stroke;
        cardBorder.StrokeWidth = 2;
        using var headerFont = new SKFont(SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold), 44);
        using var labelFont = new SKFont(SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal), 22);
        using var valueFont = new SKFont(SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold), 56);
        using var headerPaint = TextPaint("#e7edf4");
        using var labelPaint = TextPaint("#8b99ab");
        using var pingPaint = TextPaint("#f59e0b");
        using var downloadPaint = TextPaint("#06b6d4");
        using var uploadPaint = TextPaint("#8b5cf6");

        canvas.DrawText("Speedtest Watcher Network Performance", 60, 90, SKTextAlign.Left, headerFont, headerPaint);
        canvas.DrawText(preview.Subtitle, 60, 130, SKTextAlign.Left, labelFont, labelPaint);

        var style = new CardStyle(cardFill, cardBorder, labelFont, labelPaint, valueFont);
        DrawCard(canvas, PingCardLeft, "PING", preview.Ping, pingPaint, preview.PingCaption, style);
        DrawCard(canvas, DownloadCardLeft, "DOWNLOAD", preview.Download, downloadPaint, "Mbps", style);
        DrawCard(canvas, UploadCardLeft, "UPLOAD", preview.Upload, uploadPaint, "Mbps", style);

        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Position = 0;

        return File(ms, "image/png");
    }

    private static SKPaint TextPaint(string hex) => new() { Color = SKColor.Parse(hex), IsAntialias = true };

    private static void DrawCard(SKCanvas canvas, float left, string title, string value, SKPaint valuePaint, string? caption, CardStyle style)
    {
        using var card = new SKRoundRect(new SKRect(left, CardTop, left + CardWidth, CardTop + CardHeight), CardCornerRadius, CardCornerRadius);
        canvas.DrawRoundRect(card, style.Fill);
        canvas.DrawRoundRect(card, style.Border);

        var textLeft = left + CardPadding;
        canvas.DrawText(title, textLeft, CardTop + 60, SKTextAlign.Left, style.LabelFont, style.LabelPaint);
        canvas.DrawText(value, textLeft, CardTop + 150, SKTextAlign.Left, style.ValueFont, valuePaint);
        if (caption != null)
        {
            canvas.DrawText(caption, textLeft, CardTop + 210, SKTextAlign.Left, style.LabelFont, style.LabelPaint);
        }
    }
}
