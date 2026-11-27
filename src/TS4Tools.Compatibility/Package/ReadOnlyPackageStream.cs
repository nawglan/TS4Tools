// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 58-59, 237-240
// (legacy opens read-only packages with FileAccess.Read but still allows in-memory edits and SaveAs;
// only SavePackage fails, because the stream cannot be written)

namespace s4pi.Package;

/// <summary>
/// Wraps a read-only package file stream so that TS4Tools.Core treats the package as editable in memory.
/// </summary>
/// <remarks>
/// <see cref="global::TS4Tools.Package.DbpfPackage"/> marks a package opened on a non-writable stream as read-only and
/// rejects Add/Replace/Delete. Legacy s4pi lets callers edit a read-only package and save it elsewhere; this
/// wrapper reports <see cref="CanWrite"/> so the in-memory index is editable, while any attempt to write to the
/// underlying file fails. <see cref="Package.SavePackage"/> rejects read-only packages before that point.
/// </remarks>
internal sealed class ReadOnlyPackageStream(Stream inner) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => true;
    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => inner.Read(buffer);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => inner.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException("Package is read-only");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Package is read-only");

    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
