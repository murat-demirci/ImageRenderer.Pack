using ImageRenderer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TestAPI.Controllers;
[Route("api/[controller]")]
[ApiController]
public sealed class TestController(ImageProcessor imageProcessor) : ControllerBase
{
    [HttpPost("Upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty");
        }
        var result = await imageProcessor.SaveImageToLocalAsync(file, "Images",quality: Quality.Ultra,fileExtension:FileExtension.Webp);
        return Ok(result.Item2);
    }

    [HttpPost("UploadMulti")]
    public async Task<IActionResult> UploadMulti(IEnumerable<IFormFile> files)
    {
        if (files == null || !files.Any())
        {
            return BadRequest("File is empty");
        }
        var result = await imageProcessor.SaveImageToLocalAsync(files, "Images", quality: Quality.Ultra, fileExtension: FileExtension.Webp);
        return Ok(result.Item2);
    }

    [HttpPost("Delete")]
    public async Task<IActionResult> Delete(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("path is empty");
        }
        var result = await imageProcessor.DeleteImageAsync(path);
        return Ok(result);
    }

    [HttpPost("DeleteMulti")]
    public async Task<IActionResult> DeleteMulti(IEnumerable<string> paths)
    {
        if (!paths.Any() || paths is null)
        {
            return BadRequest("path is empty");
        }
        var result = await imageProcessor.DeleteImagesAsync(paths);
        return Ok(result);
    }
}
