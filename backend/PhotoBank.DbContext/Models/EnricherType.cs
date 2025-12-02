using System;

namespace PhotoBank.DbContext.Models
{
    [Flags]
    public enum EnricherType
    {
        None = 0,
        Adult = 1,
        Analyze = 2,
        Caption = 4,
        Category = 16,
        Color = 32,
        Face = 64,
        Metadata = 128,
        ObjectProperty = 256,
        Preview = 512,
        Tag = 1024,
        Thumbnail = 2048,
        Duplicate = 4096
    }
}
