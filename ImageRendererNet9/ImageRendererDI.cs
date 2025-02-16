using Microsoft.Extensions.DependencyInjection;

namespace ImageRendererNet9;
public static class ImageRendererDI
{
    public static void AddImageRenderer(this IServiceCollection services, string[]? allowwedFileExtension = null, long maxFileSizeInMb = 0)
    {
        services.AddScoped<ImageProcessor>();
        services.Configure<ImageProcessorOptions>(options =>
        {
            options.AllowwedFileExtension = allowwedFileExtension?.Length > 0 ? allowwedFileExtension : ["jpg", "jpeg", "png", "gif", "ico"];
            options.MaxFileSizeInMb = maxFileSizeInMb;
        });
    }
}
