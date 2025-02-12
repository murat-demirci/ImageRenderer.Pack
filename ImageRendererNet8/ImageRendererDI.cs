using Microsoft.Extensions.DependencyInjection;

namespace ImageRenderer8._0;
public static class ImageRendererDI
{
    public static void AddImageRenderer(this IServiceCollection services, string[]? allowwedFileExtension = default, long maxFileSizeInMb = 10)
    {
        services.AddScoped<ImageProcessor>();
        services.Configure<ImageProcessorOptions>(options =>
        {
            options.AllowwedFileExtension = allowwedFileExtension ?? ["jpg", "jpeg", "png", "gif", "ico"];
            options.MaxFileSizeInMb = maxFileSizeInMb;
        });
    }
}
