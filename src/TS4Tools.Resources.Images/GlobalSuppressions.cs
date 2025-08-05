using System.Diagnostics.CodeAnalysis;

// DDS format enums use uint to match the native DDS format specifications
[assembly: SuppressMessage("Design", "CA1028:Enum storage values should be Int32", Scope = "type", Target = "~T:TS4Tools.Resources.Images.DdsFourCC")]
[assembly: SuppressMessage("Design", "CA1028:Enum storage values should be Int32", Scope = "type", Target = "~T:TS4Tools.Resources.Images.DdsPixelFormatFlags")]
[assembly: SuppressMessage("Design", "CA1028:Enum storage values should be Int32", Scope = "type", Target = "~T:TS4Tools.Resources.Images.DdsFlags")]
[assembly: SuppressMessage("Design", "CA1028:Enum storage values should be Int32", Scope = "type", Target = "~T:TS4Tools.Resources.Images.DdsCaps")]
[assembly: SuppressMessage("Design", "CA1028:Enum storage values should be Int32", Scope = "type", Target = "~T:TS4Tools.Resources.Images.DdsCaps2")]

// Factory method pattern is appropriate here
[assembly: SuppressMessage("Design", "CA1024:Use properties where appropriate", Scope = "member", Target = "~M:TS4Tools.Resources.Images.ImageResourceFactory.GetSupportedFormats~System.Collections.Generic.IReadOnlyDictionary{TS4Tools.Resources.Images.ImageFormat,System.String}")]
