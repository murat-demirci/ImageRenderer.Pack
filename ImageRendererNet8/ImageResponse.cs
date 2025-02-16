namespace ImageRendererNet8;
public sealed class ImageResponse
{
    #region Variables
    public bool IsSuccess { get; set; }
    public List<ImageInfo> CompletedImageInfos { get; set; } = [];
    public List<ImageInfo> FailedImagesInfo { get; set; } = [];
    #endregion
    #region Constructors
    public ImageResponse()
    {

    }

    public ImageResponse(bool isSuccess, List<ImageInfo> completedImageInfos)
    {
        IsSuccess = isSuccess;
        CompletedImageInfos = completedImageInfos;
    }

    public ImageResponse(bool isSuccess, List<ImageInfo> completedImageInfos, List<ImageInfo> failedImagesInfo)
    {
        IsSuccess = isSuccess;
        CompletedImageInfos = completedImageInfos;
        FailedImagesInfo = failedImagesInfo;
    }
    #endregion
    #region Public Methods
    public ImageResponse SetFailedRepsone(string fileName = "", string message = "", ProccessType proccessType = ProccessType.Upload)
    {
        FailedImagesInfo.Add(new($"The file named {fileName} could not be {(proccessType is ProccessType.Upload ? "uploaded" : "deleted")}. [{message}]", fileName, false));
        return this;
    }

    public ImageResponse SetSuccessResponse(string path = "", ProccessType proccessType = ProccessType.Upload)
    {
        IsSuccess = true;
        CompletedImageInfos.Add(new($"The file has been {(proccessType is ProccessType.Upload ? "uploaded" : "deleted")} successfully. [{path}]", path, true));
        return this;
    }
    #endregion
}

public sealed class ImageInfo(string message, string fileNameOrPath, bool isFailed)
{
    public string Message { get; set; } = message;
    public string FileNameOrPath { get; set; } = fileNameOrPath;
    public bool IsFailed { get; set; } = isFailed;
}
