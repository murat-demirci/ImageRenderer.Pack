using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Collections.Concurrent;

namespace ImageRenderer8._0;
public sealed class ImageProcessor
{
    private readonly ImageProcessorOptions _options;
    public ImageProcessor(IOptions<ImageProcessorOptions> options)
    {
        _options = options.Value;
    }

    #region Upload Proccess
    public async Task<(bool, string)> SaveImageToLocalAsync(
        IFormFile file,
        string fileName,
        string path = "Images",
        Quality quality = Quality.High,
        FileExtension fileExtension = FileExtension.Webp,
        int widthSize = 1024,
        int heightSize = 1024
    )
    {
        var isValid = CheckFile(file);
        if (!isValid.Item1)
        {
            return isValid;
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
                return (false, "Resizing failed.");

            using var image = SKImage.FromBitmap(resized);

            using var encodedData = image.Encode((SKEncodedImageFormat)fileExtension, (int)quality);
            await using var output = new FileStream(Path.Combine(path, imageName), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await output.WriteAsync(encodedData.ToArray().AsMemory(0, (int)encodedData.Size));

            return (true, Path.Combine(path, imageName).ToString());
        }
        catch (Exception ex)
        {
            return (false, $"UnexpectedError: {ex.Message}");
        }
    }

    public async Task<(bool, IEnumerable<string>)> SaveImageToLocalAsync(
        IEnumerable<IFormFile> files,
        string fileName,
        string path = "Images",
        Quality quality = Quality.High,
        FileExtension fileExtension = FileExtension.Webp,
        int widthSize = 1024,
        int heightSize = 1024
    )
    {
        if (!files.Any())
            return (false, new[] { "File is empty." });

        var imageResultList = new List<(SKData, string)>();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var errorMessages = new List<string>();
        var lockObject = new object();

        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            await Task.WhenAll(files.Select(async file =>
            {
                try
                {
                    var isValid = CheckFile(file);
                    if (!isValid.Item1)
                    {
                        lock (lockObject)
                        {
                            errorMessages.Add(isValid.Item2);
                        }
                        return;
                    }

                    var imageName = $"{widthSize}x{heightSize}_{fileName}_{timestamp}_{Guid.NewGuid():N}.{fileExtension.ToString().ToLower()}";
                    var filePath = Path.Combine(path, imageName);

                    await using var stream = file.OpenReadStream();
                    using var original = SKBitmap.Decode(stream);

                    SKSamplingOptions options = new(SKCubicResampler.Mitchell);
                    using var resized = original.Resize(new SKImageInfo(widthSize, heightSize), options);

                    if (resized == null)
                    {
                        lock (lockObject)
                        {
                            errorMessages.Add($"Resizing failed for file '{file.FileName}'.");
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
                        imageResultList.Add((encodedData, imageName));
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        errorMessages.Add($"An error occurred while processing file '{file.FileName}': {ex.Message}");
                    }
                }
            }));

            if (errorMessages.Any())
            {
                return (false, errorMessages.ToArray());
            }

            return (true, imageResultList.Select(x => $"{path}/{x.Item2}"));
        }
        catch (Exception ex)
        {
            return (false, new[] { $"UnexpectedError: {ex.Message}" });
        }
    }
    #endregion

    #region Delete Proccess
    public async Task<bool> DeleteImageAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            // Log the exception if necessary
            return false;
        }
    }

    public async Task<IEnumerable<(bool, string)>> DeleteImagesAsync(IEnumerable<string> filePaths)
    {
        var results = new ConcurrentBag<(bool, string)>();

        await Task.WhenAll(filePaths.Select(async filePath =>
        {
            var result = await DeleteImageAsync(filePath);
            results.Add((result, filePath));
        }));

        return results;
    }
    #endregion

    #region Private Methods
    private bool IsSupportedImageFormat(string fileExtension)
    {
        if (_options.AllowwedFileExtension.Length == 0)
            return true;
        return _options.AllowwedFileExtension.Contains(fileExtension.TrimStart('.'));
    }

    private (bool, string) CheckFile(IFormFile file)
    {
        if (file == null)
            return (false, "File is empty");

        if (file.Length > _options.MaxFileSizeInMb)
        {
            return (false, $"File '{file.FileName}' exceeds the maximum allowed size of {_options.MaxFileSizeInMb / (1024 * 1024)} MB.");
        }

        if (!IsSupportedImageFormat(Path.GetExtension(file.FileName)))
        {
            return (false, $"File '{file.FileName}' has not supported file extension");
        }

        return (true, "");
    }
    #endregion
}
