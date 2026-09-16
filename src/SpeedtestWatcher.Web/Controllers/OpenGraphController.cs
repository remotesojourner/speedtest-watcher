using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.Interfaces;
using SkiaSharp;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/opengraph")]
public class OpenGraphController : ControllerBase
{
    private readonly ISpeedtestRepository _repository;

    // Who may fetch the preview is decided before this runs, like every other request:
    // with sign-in on, only signed-in users, or anyone when visitors have read-only access.
    public OpenGraphController(ISpeedtestRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("image")]
    public async Task<IActionResult> GetImage()
    {
        var latest = await _repository.GetLatestAsync();
        int width = 1200;
        int height = 600;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Background
        canvas.Clear(new SKColor(15, 20, 25)); // #0f1419

        // Paint definitions
        using var cardPaint = new SKPaint
        {
            Color = new SKColor(26, 32, 41), // #1a2029
            Style = SKPaintStyle.Fill
        };

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(42, 52, 65), // #2a3441
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        using var headerFont = new SKFont(SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold), 44);
        using var labelFont = new SKFont(SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal), 22);
        using var statFont = new SKFont(SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold), 56);

        using var headerPaint = new SKPaint { Color = new SKColor(231, 237, 244), IsAntialias = true };
        using var labelPaint = new SKPaint { Color = new SKColor(139, 153, 171), IsAntialias = true };
        using var pingPaint = new SKPaint { Color = new SKColor(245, 158, 11), IsAntialias = true };
        using var downPaint = new SKPaint { Color = new SKColor(6, 182, 212), IsAntialias = true };
        using var upPaint = new SKPaint { Color = new SKColor(139, 92, 246), IsAntialias = true };

        // Title
        canvas.DrawText("Speedtest Watcher Network Performance", 60, 90, SKTextAlign.Left, headerFont, headerPaint);
        string dateStr = latest != null ? $"Latest test: {latest.Created:yyyy-MM-dd HH:mm:ss} UTC" : "No speedtests recorded yet";
        canvas.DrawText(dateStr, 60, 130, SKTextAlign.Left, labelFont, labelPaint);

        // 3 Cards: Ping, Download, Upload
        int cardWidth = 340;
        int cardHeight = 360;
        int startY = 180;
        int[] xs = [60, 430, 800];

        // Ping card
        var pingRect = new SKRoundRect(new SKRect(xs[0], startY, xs[0] + cardWidth, startY + cardHeight), 16, 16);
        canvas.DrawRoundRect(pingRect, cardPaint);
        canvas.DrawRoundRect(pingRect, borderPaint);
        canvas.DrawText("PING", xs[0] + 30, startY + 60, SKTextAlign.Left, labelFont, labelPaint);
        string pingVal = latest != null ? $"{latest.Ping} ms" : "--";
        canvas.DrawText(pingVal, xs[0] + 30, startY + 150, SKTextAlign.Left, statFont, pingPaint);
        if (latest?.Jitter.HasValue == true)
        {
            canvas.DrawText($"±{latest.Jitter.Value:F1} ms jitter", xs[0] + 30, startY + 210, SKTextAlign.Left, labelFont, labelPaint);
        }

        // Download card
        var downRect = new SKRoundRect(new SKRect(xs[1], startY, xs[1] + cardWidth, startY + cardHeight), 16, 16);
        canvas.DrawRoundRect(downRect, cardPaint);
        canvas.DrawRoundRect(downRect, borderPaint);
        canvas.DrawText("DOWNLOAD", xs[1] + 30, startY + 60, SKTextAlign.Left, labelFont, labelPaint);
        string downVal = latest != null ? $"{latest.Download:F1}" : "--";
        canvas.DrawText(downVal, xs[1] + 30, startY + 150, SKTextAlign.Left, statFont, downPaint);
        canvas.DrawText("Mbps", xs[1] + 30, startY + 210, SKTextAlign.Left, labelFont, labelPaint);

        // Upload card
        var upRect = new SKRoundRect(new SKRect(xs[2], startY, xs[2] + cardWidth, startY + cardHeight), 16, 16);
        canvas.DrawRoundRect(upRect, cardPaint);
        canvas.DrawRoundRect(upRect, borderPaint);
        canvas.DrawText("UPLOAD", xs[2] + 30, startY + 60, SKTextAlign.Left, labelFont, labelPaint);
        string upVal = latest != null ? $"{latest.Upload:F1}" : "--";
        canvas.DrawText(upVal, xs[2] + 30, startY + 150, SKTextAlign.Left, statFont, upPaint);
        canvas.DrawText("Mbps", xs[2] + 30, startY + 210, SKTextAlign.Left, labelFont, labelPaint);

        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Position = 0;

        return File(ms, "image/png");
    }
}
