// src/Dynamite.Modules/Welcome/Helpers/WelcomeImageGenerator.cs
namespace Dynamite.Modules.Welcome.Helpers;

using SkiaSharp;
using Microsoft.Extensions.Logging;

public class WelcomeImageGenerator
{
    private readonly ILogger<WelcomeImageGenerator> _logger;
    private readonly HttpClient _httpClient;

    // Canvas dimensions
    private const int Width = 900;
    private const int Height = 300;

    public WelcomeImageGenerator(
        ILogger<WelcomeImageGenerator> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Generates a welcome banner as a PNG stream.
    /// Caller is responsible for disposing the stream.
    /// </summary>
    public async Task<Stream?> GenerateAsync(
        string username, string guildName,
        int memberCount, string? avatarUrl)
    {
        try
        {
            // Download avatar nếu có
            SKBitmap? avatarBitmap = null;
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                avatarBitmap = await DownloadAvatarAsync(avatarUrl);
            }

            using var surface = SKSurface.Create(
                new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));

            var canvas = surface.Canvas;
            canvas.Clear();

            DrawBackground(canvas);
            DrawOverlay(canvas);

            if (avatarBitmap is not null)
            {
                DrawAvatar(canvas, avatarBitmap);
                avatarBitmap.Dispose();
            }

            DrawText(canvas, username, guildName, memberCount, avatarBitmap is not null);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 95);

            // Copy sang MemoryStream vì SKData sẽ bị dispose sau using block
            var ms = new MemoryStream();
            data.SaveTo(ms);
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate welcome image for {Username}", username);
            return null;
        }
    }

    private static void DrawBackground(SKCanvas canvas)
    {
        // Dark gradient background
        using var paint = new SKPaint();

        var colors = new SKColor[]
        {
            new(0x23, 0x27, 0x2A), // dark gray
            new(0x2C, 0x2F, 0x33), // slightly lighter
        };

        var positions = new float[] { 0f, 1f };

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(Width, Height),
            colors,
            positions,
            SKShaderTileMode.Clamp);

        paint.Shader = shader;
        canvas.DrawRect(SKRect.Create(Width, Height), paint);
    }

    private static void DrawOverlay(SKCanvas canvas)
    {
        // Subtle accent bar on the left
        using var paint = new SKPaint
        {
            Color = new SKColor(0x57, 0xF2, 0x87, 0xCC), // green, semi-transparent
            IsAntialias = true
        };
        canvas.DrawRect(SKRect.Create(0, 0, 6, Height), paint);

        // Bottom accent line
        using var linePaint = new SKPaint
        {
            Color = new SKColor(0x57, 0xF2, 0x87, 0x40),
            IsAntialias = true
        };
        canvas.DrawRect(SKRect.Create(0, Height - 3, Width, 3), linePaint);
    }

    private static void DrawAvatar(SKCanvas canvas, SKBitmap avatar)
    {
        const int avatarSize = 160;
        const int avatarX = 70;
        const int avatarY = (Height - avatarSize) / 2;
        const int avatarRadius = avatarSize / 2;

        var centerX = avatarX + avatarRadius;
        var centerY = avatarY + avatarRadius;

        // Clip to circle
        using var clipPath = new SKPath();
        clipPath.AddCircle(centerX, centerY, avatarRadius);

        canvas.Save();
        canvas.ClipPath(clipPath, SKClipOperation.Intersect, antialias: true);

        var destRect = SKRect.Create(avatarX, avatarY, avatarSize, avatarSize);
        canvas.DrawBitmap(avatar, destRect);

        canvas.Restore();

        // Circle border
        using var borderPaint = new SKPaint
        {
            Color = new SKColor(0x57, 0xF2, 0x87),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 4,
            IsAntialias = true
        };
        canvas.DrawCircle(centerX, centerY, avatarRadius + 2, borderPaint);
    }

    private static void DrawText(
        SKCanvas canvas, string username,
        string guildName, int memberCount, bool hasAvatar)
    {
        // Text starts after avatar if present, otherwise from left
        float textX = hasAvatar ? 270f : 60f;

        // "Welcome to" label
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(0xB9, 0xBB, 0xBE), // Discord gray
            IsAntialias = true,
            TextSize = 28f,
        };
        using var labelFont = new SKFont(SKTypeface.Default, 28f);
        canvas.DrawText("WELCOME TO", textX, 100f, labelFont, labelPaint);

        // Guild name
        using var guildPaint = new SKPaint
        {
            Color = new SKColor(0x57, 0xF2, 0x87), // green accent
            IsAntialias = true,
        };
        using var guildFont = new SKFont(SKTypeface.Default, 38f)
        {
            Embolden = true
        };
        var guildDisplay = guildName.Length > 28 ? guildName[..25] + "..." : guildName;
        canvas.DrawText(guildDisplay.ToUpperInvariant(), textX, 148f, guildFont, guildPaint);

        // Username
        using var userPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
        };
        using var userFont = new SKFont(SKTypeface.Default, 34f)
        {
            Embolden = true
        };
        var userDisplay = username.Length > 30 ? username[..27] + "..." : username;
        canvas.DrawText(userDisplay, textX, 200f, userFont, userPaint);

        // Member count
        using var countPaint = new SKPaint
        {
            Color = new SKColor(0xB9, 0xBB, 0xBE),
            IsAntialias = true,
        };
        using var countFont = new SKFont(SKTypeface.Default, 24f);
        canvas.DrawText($"Member #{memberCount}", textX, 240f, countFont, countPaint);
    }

    private async Task<SKBitmap?> DownloadAvatarAsync(string url)
    {
        try
        {
            // Thêm size param để lấy ảnh nhỏ hơn, load nhanh hơn
            var sizedUrl = url.Contains('?') ? url + "&size=256" : url + "?size=256";
            var bytes = await _httpClient.GetByteArrayAsync(sizedUrl);
            return SKBitmap.Decode(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download avatar from {Url}", url);
            return null;
        }
    }
}