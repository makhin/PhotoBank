using PhotoBank.Services;
using Xunit;

public class S3KeyBuilderTests
{
    [Fact]
    public void BuildFaceKey_PadsId()
    {
        var key = S3KeyBuilder.BuildFaceKey(15);
        Assert.Equal("faces/0000000015.jpg", key);
    }

    [Fact]
    public void BuildPreviewKey_UsesStorageAndRelativePath()
    {
        var key = S3KeyBuilder.BuildPreviewKey("My Storage", "dir1\\dir 2", 42);
        Assert.Equal("preview/My-Storage/dir1/dir-2/0000000042_preview.jpg", key);
    }
}
