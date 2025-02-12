using SkiaSharp;

namespace ImageRenderer8._0;
public enum Quality
{
    Low = 25,
    Medium = 50,
    High = 75,
    Ultra = 100
}

public enum FileExtension
{
    Webp = SKEncodedImageFormat.Webp,
    Png = SKEncodedImageFormat.Png,
    Jpeg = SKEncodedImageFormat.Jpeg,
    Gif = SKEncodedImageFormat.Gif,
    Ico = SKEncodedImageFormat.Ico
}