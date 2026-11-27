// Source: legacy_references/Sims4Tools/s4pe Helpers/DDSHelper/Program.cs lines 41-65 (/export: DSTResource.ToDDS)
// Source: legacy_references/Sims4Tools/s4pe Helpers/DDSHelper/Import.cs lines 55-72 (/import: DDS stored as the resource)
// Source: legacy_references/Sims4Tools/s4pe Helpers/DDSHelper/DDSImport.helper, DDSExport.helper (types 0x00B2D882 0x8FFB80F6 0xB6C8B6A0)
// Source: legacy_references/Sims4Tools/s4pe Helpers/RLEDDSHelper/Import.cs lines 55-72 (ImportToRLE)
// Source: legacy_references/Sims4Tools/s4pe Helpers/RLESDDSHelper/ImportRLES.cs lines 55-72 (ImportToRLE RLES)
// Source: legacy_references/Sims4Tools/s4pe Helpers/DMAPImageHelper/Program.cs lines 33-87 (/export and /exportRobe)
// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/DSTResource.cs lines 82-134 (ImportToDST)

using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;

namespace TS4Tools.UI.Services;

/// <summary>Image exports offered for the selected resource.</summary>
public enum ResourceExportKind
{
    /// <summary>DDS (unshuffled) from a DDS/DST image or RLE/RLES texture.</summary>
    Dds,
    /// <summary>24-bit BMP of a deformer map's skin-tight data.</summary>
    DeformerMapSkin,
    /// <summary>24-bit BMP of a deformer map's robe data.</summary>
    DeformerMapRobe,
}

/// <summary>Image imports offered for the selected resource.</summary>
public enum ResourceImportKind
{
    /// <summary>DDS into a DDS/DST image (stored as DDS) or RLE/RLES texture (re-encoded).</summary>
    Dds,
    /// <summary>DDS converted to the shuffled DST layout.</summary>
    DdsToDst,
    /// <summary>24-bit BMP as a deformer map's skin data.</summary>
    DeformerMapBitmap,
}

/// <summary>
/// The in-process equivalents of the legacy s4pe image helpers (DDSHelper, RLEDDSHelper, RLESDDSHelper,
/// DMAPImageHelper): each export reads the wrapper, each import replaces the wrapper's data in place so the change
/// goes through the normal edit/commit path.
/// </summary>
public static class ResourceImageTools
{
    /// <summary>Resource types the legacy DDS helper is registered for.</summary>
    public static readonly IReadOnlyList<uint> DdsHelperTypes = [0x00B2D882, 0x8FFB80F6, 0xB6C8B6A0];

    /// <summary>Whether <paramref name="kind"/> can be exported from <paramref name="wrapper"/>.</summary>
    public static bool CanExport(object? wrapper, ResourceExportKind kind) => kind switch
    {
        ResourceExportKind.Dds => wrapper is ImageResource { Format: ImageFormat.Dds or ImageFormat.Dst } or RleResource,
        ResourceExportKind.DeformerMapSkin => wrapper is DeformerMapResource,
        ResourceExportKind.DeformerMapRobe => wrapper is DeformerMapResource,
        _ => false,
    };

    /// <summary>Whether <paramref name="kind"/> can be imported into <paramref name="wrapper"/>.</summary>
    public static bool CanImport(object? wrapper, ResourceImportKind kind) => kind switch
    {
        ResourceImportKind.Dds => wrapper is RleResource
            || wrapper is ImageResource image && (image.Format is ImageFormat.Dds or ImageFormat.Dst
                || DdsHelperTypes.Contains(image.Key.ResourceType)),
        ResourceImportKind.DdsToDst => wrapper is ImageResource dst && (dst.Format is ImageFormat.Dds or ImageFormat.Dst
            || DdsHelperTypes.Contains(dst.Key.ResourceType)),
        ResourceImportKind.DeformerMapBitmap => wrapper is DeformerMapResource,
        _ => false,
    };

    /// <summary>Default file extension for an export.</summary>
    public static string GetExtension(ResourceExportKind kind) => kind == ResourceExportKind.Dds ? ".dds" : ".bmp";

    /// <summary>
    /// Produces the exported file.
    /// </summary>
    /// <exception cref="InvalidOperationException">The wrapper does not support the export or holds no image.</exception>
    public static byte[] Export(object wrapper, ResourceExportKind kind)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        byte[]? result = (kind, wrapper) switch
        {
            // Source: DDSHelper/Program.cs line 59 (dst.ToDDS())
            (ResourceExportKind.Dds, ImageResource image) => ToPlainDds(image),
            // Source: RLEDDSHelper/RLESDDSHelper Program.cs (/export writes RLEResource.ToDDS())
            (ResourceExportKind.Dds, RleResource rle) => rle.ToDds(),
            // Source: DMAPImageHelper/Program.cs lines 41-55 (GetSkinBitMap) and 57-71 (robe)
            (ResourceExportKind.DeformerMapSkin, DeformerMapResource map) => DeformerMapImageConverter.ToBitmap(map, DeformerMapImageType.Skin),
            (ResourceExportKind.DeformerMapRobe, DeformerMapResource map) => DeformerMapImageConverter.ToBitmap(map, DeformerMapImageType.Robe),
            _ => throw new InvalidOperationException($"{wrapper.GetType().Name} does not support {kind} export."),
        };
        return result ?? throw new InvalidOperationException("The resource holds no image data to export.");
    }

    /// <summary>
    /// Replaces the wrapper's data from an imported file.
    /// </summary>
    /// <exception cref="InvalidDataException">The file is not a usable DDS/BMP.</exception>
    /// <exception cref="NotSupportedException">The DDS format cannot be converted (e.g. DXT3 to DST).</exception>
    public static void Import(object wrapper, ResourceImportKind kind, ReadOnlySpan<byte> file)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        switch (kind, wrapper)
        {
            case (ResourceImportKind.Dds, ImageResource image):
                // Source: DDSHelper/Import.cs lines 61-67 - DSTResource keeps an unshuffled DDS as-is.
                RequireDds(file);
                if (DdsImage.TryLoad(file) == null)
                    throw new InvalidDataException("The DDS file could not be decoded.");
                image.SetImageData(file);
                break;
            case (ResourceImportKind.Dds, RleResource rle):
                // Source: RLEDDSHelper/Import.cs line 63 and RLESDDSHelper/ImportRLES.cs line 63
                RequireDds(file);
                rle.ImportToRle(file, rle.Key.ResourceType == RleResource.TypeIdAlternate || rle.Version == RleVersion.Rles
                    ? RleVersion.Rles
                    : RleVersion.Rle2);
                break;
            case (ResourceImportKind.DdsToDst, ImageResource dst):
                RequireDds(file);
                dst.ImportToDst(file);
                break;
            case (ResourceImportKind.DeformerMapBitmap, DeformerMapResource map):
                DeformerMapImageConverter.ImportFromBitmap(map, file);
                break;
            default:
                throw new InvalidOperationException($"{wrapper.GetType().Name} does not support {kind} import.");
        }
    }

    // ------------------------------------------------------------------ image editing

    /// <summary>Whether the wrapper holds a DDS/DST/RLE image that can be edited in place.</summary>
    public static bool CanEditImage(object? wrapper) =>
        wrapper is ImageResource { Format: ImageFormat.Dds or ImageFormat.Dst } or RleResource;

    /// <summary>
    /// Decodes the wrapper's image for editing.
    /// </summary>
    /// <exception cref="InvalidOperationException">The wrapper holds no editable image.</exception>
    /// <exception cref="InvalidDataException">The image cannot be decoded.</exception>
    /// <remarks>Source: DDSPanel.cs lines 289-375 (DDSLoad / RLELoad / DSTLoad: the wrapper's DDS is loaded into a DdsFile).</remarks>
    public static DdsImage LoadForEdit(object wrapper)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        var dds = wrapper switch
        {
            ImageResource { Format: ImageFormat.Dds or ImageFormat.Dst } image => ToPlainDds(image),
            RleResource rle => rle.ToDds(),
            _ => throw new InvalidOperationException($"{wrapper.GetType().Name} holds no editable image."),
        };
        return dds != null ? DdsImage.Load(dds) : throw new InvalidDataException("The image cannot be converted to DDS.");
    }

    /// <summary>
    /// Writes an edited image back into the wrapper in the wrapper's own layout (DDS as DDS, DST shuffled,
    /// RLE re-encoded). Mipmaps are regenerated.
    /// </summary>
    /// <param name="wrapper">The wrapper to update.</param>
    /// <param name="image">The edited image.</param>
    /// <param name="format">The DDS format to write; null keeps the format the image was loaded with.</param>
    /// <exception cref="NotSupportedException">The format cannot be stored in the wrapper.</exception>
    /// <remarks>
    /// Source: DDSHelper/Import.cs lines 55-72, RLEDDSHelper/Import.cs lines 55-72 (the DDS saved from the DDSPanel is
    /// stored through ImportToDST / ImportToRLE) and DDSPanel.cs lines 468-476 (DDSSave).
    /// </remarks>
    public static void StoreEdited(object wrapper, DdsImage image, DdsFormat? format = null)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        ArgumentNullException.ThrowIfNull(image);
        switch (wrapper)
        {
            case ImageResource { Format: ImageFormat.Dds or ImageFormat.Dst } texture when IsShuffled(texture):
                // DST1/DST5 hold DXT1/DXT5 only
                var dstFormat = format ?? image.SaveFormat;
                texture.ImportToDst(image.SaveToDds(dstFormat is DdsFormat.Dxt1 ? DdsFormat.Dxt1 : DdsFormat.Dxt5, generateMipmaps: true));
                break;
            case ImageResource { Format: ImageFormat.Dds or ImageFormat.Dst } dds:
                dds.SetImageData(image.SaveToDds(format, generateMipmaps: true));
                break;
            case RleResource rle:
                // RLE stores DXT5 (Source: RleResource.EncodeFromDds)
                rle.ImportToRle(image.SaveToDds(DdsFormat.Dxt5, generateMipmaps: true), rle.Key.ResourceType == RleResource.TypeIdAlternate || rle.Version == RleVersion.Rles
                    ? RleVersion.Rles
                    : RleVersion.Rle2);
                break;
            default:
                throw new InvalidOperationException($"{wrapper.GetType().Name} holds no editable image.");
        }
    }

    /// <summary>
    /// Replaces the wrapper's image with decoded PNG/JPEG pixels. DXT1 images stay DXT1, everything else is DXT5.
    /// </summary>
    /// <remarks>Source: DDSPanel.cs lines 417-462 (Import) followed by DDSSave; DdsFile.CreateImage(Bitmap).</remarks>
    public static void ImportRgba(object wrapper, ReadOnlySpan<byte> rgba, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        var current = wrapper switch
        {
            ImageResource { Format: ImageFormat.Dds or ImageFormat.Dst } image => image.DxtFormat,
            _ => DxtFormat.Dxt5,
        };

        var imported = DdsImage.FromRgba32(rgba, width, height);
        StoreEdited(wrapper, imported, current == DxtFormat.Dxt1 ? DdsFormat.Dxt1 : DdsFormat.Dxt5);
    }

    // A DST texture is a DDS file whose FourCC is DST1/DST5 and whose blocks are shuffled; the wrapper
    // reports it as Dds when it carries the "DDS " signature, so the shuffle is detected from the FourCC.
    // Source: DSTResource.cs lines 54-72 (ToDDS: isShuffled)
    private static bool IsShuffled(ImageResource image) =>
        image.Format == ImageFormat.Dst || DstDecoder.IsDst(image.ImageData.Span);

    private static byte[]? ToPlainDds(ImageResource image) =>
        DstDecoder.IsDst(image.ImageData.Span) ? DstDecoder.UnshuffleToDds(image.ImageData.Span) : image.ToDds();

    private static void RequireDds(ReadOnlySpan<byte> file)
    {
        if (!ResourceViewerFactory.IsDds(file))
            throw new InvalidDataException("Not a DDS file.");
    }
}
