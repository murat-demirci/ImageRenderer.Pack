using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace ImageRendererNet8;
public sealed class ImageProcessor(IOptions<ImageProcessorOptions> options)
{
    private readonly ImageProcessorOptions _options = options.Value;
    private readonly ImageResponse _imageResponse = new();
    private readonly object lockObject = new();

    #region Upload Proccess
    public async Task<ImageResponse> SaveImageToLocalAsync(
        IFormFile file,
        string path = "Images",
        string fileName = "",
        Quality quality = Quality.High,
        FileExtension fileExtension = FileExtension.Webp,
        int widthSize = 1024,
        int heightSize = 1024
    )
    {
        var fileControl = CheckFile(file);
        if (fileControl.FailedImagesInfo.Count > 0)
        {
            return fileControl;
        }

        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var imageName = $"{widthSize}x{heightSize}_{fileName}_{timestamp}_{Guid.NewGuid():N}.{fileExtension.ToString().ToLower()}";

            await using var stream = file.OpenReadStream();
            using var original = SKBitmap.Decode(stream);

            SKSamplingOptions options = new SKSamplingOptions(SKCubicResampler.Mitchell);
            using var resized = original.Resize(new SKImageInfo(widthSize, heightSize), options);

            if (resized == null)
                return _imageResponse.SetFailedRepsone(fileName: file.FileName, message: "Resizing failed.");

            using var image = SKImage.FromBitmap(resized);

            using var encodedData = image.Encode((SKEncodedImageFormat)fileExtension, (int)quality);
            await using var output = new FileStream(Path.Combine(path, imageName), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await output.WriteAsync(encodedData.ToArray().AsMemory(0, (int)encodedData.Size));

            return _imageResponse.SetSuccessResponse($"{path}/{imageName}");
        }
        catch (Exception ex)
        {
            return _imageResponse.SetFailedRepsone(fileName: file.FileName, message: $"ERR_500: {ex.Message}");
        }
    }

    public async Task<ImageResponse> SaveImageToLocalAsync(
        IEnumerable<IFormFile> files,
        string path = "Images",
        string fileName = "",
        Quality quality = Quality.High,
        FileExtension fileExtension = FileExtension.Webp,
        int widthSize = 1024,
        int heightSize = 1024
    )
    {
        if (files is null || !files.Any())
            return _imageResponse.SetFailedRepsone(message: "Could not be empty");

        var imageResultList = new List<(SKData, string)>();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var errorMessages = new List<string>();

        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            await Task.WhenAll(files.Select(async (file, index) =>
            {
                try
                {
                    lock (lockObject)
                    {
                        CheckFile(file);
                    }

                    var imageName = $"{widthSize}x{heightSize}_{fileName + index}_{timestamp}_{Guid.NewGuid():N}.{fileExtension.ToString().ToLower()}";
                    var filePath = Path.Combine(path, imageName);

                    await using var stream = file.OpenReadStream();
                    using var original = SKBitmap.Decode(stream);

                    SKSamplingOptions options = new(SKCubicResampler.Mitchell);
                    using var resized = original.Resize(new SKImageInfo(widthSize, heightSize), options);

                    if (resized == null)
                    {
                        lock (lockObject)
                        {
                            _imageResponse.SetFailedRepsone(fileName: file.FileName, message: "Resizing failed.");
                        }
                        return;
                    }

                    using var image = SKImage.FromBitmap(resized);
                    using var encodedData = image.Encode((SKEncodedImageFormat)fileExtension, (int)quality);

                    await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        encodedData.SaveTo(fileStream);
                    }

                    lock (lockObject)
                    {
                        _imageResponse.SetSuccessResponse($"{path}/{imageName}");
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        _imageResponse.SetFailedRepsone(fileName: file.FileName, message: $"ERR_500: {ex.Message}");
                    }
                }
            }));

            return _imageResponse;
        }
        catch (Exception ex)
        {
            return _imageResponse.SetFailedRepsone(message: $"ERR_500: {ex.Message}");
        }
    }
    #endregion

    #region Delete Proccess
    public async Task<ImageResponse> DeleteImageAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                lock (lockObject)
                    return _imageResponse.SetSuccessResponse(Path.GetFileName(filePath));
            }
            lock (lockObject)
                return _imageResponse.SetFailedRepsone(fileName: Path.GetFileName(filePath), message: $"Not Found");
        }
        catch (Exception ex)
        {
            lock (lockObject)
                return _imageResponse.SetFailedRepsone(fileName: Path.GetFileName(filePath), message: $"ERR_500: {ex.Message}");
        }
    }

    public async Task<ImageResponse> DeleteImagesAsync(IEnumerable<string> filePaths)
    {
        await Task.WhenAll(filePaths.Select(async filePath =>
        {
            await DeleteImageAsync(filePath);
        }));

        return _imageResponse;
    }
    #endregion

    #region Private Methods
    private bool IsSupportedImageFormat(string fileExtension)
    {
        if (_options.AllowwedFileExtension.Length == 0)
            return true;
        return _options.AllowwedFileExtension.Contains(fileExtension.TrimStart('.'));
    }

    private ImageResponse CheckFile(IFormFile file)
    {
        if (file is null)
            return _imageResponse.SetFailedRepsone(message: "Could not be empty");

        if (_options.MaxFileSizeInMb is not 0 && file.Length > _options.MaxFileSizeInMb)
        {
            return _imageResponse.SetFailedRepsone(fileName: file.FileName, message: $"Exceeds the maximum allowed size of {_options.MaxFileSizeInMb / (1024 * 1024)} MB.");
        }

        if (!IsSupportedImageFormat(Path.GetExtension(file.FileName)))
        {
            return _imageResponse.SetFailedRepsone(fileName: file.FileName, message: $"Not supported file extension");
        }

        return _imageResponse;
    }
    #endregion
}
