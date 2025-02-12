namespace ImageRenderer8._0;
public sealed class ImageProcessorOptions
{
    private long maxFileSize;
    public string[] AllowwedFileExtension { get; set; }
    public long MaxFileSizeInMb { get { return maxFileSize; } set { maxFileSize = value * 1024 * 1024; } }
}