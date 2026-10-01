using System.Text;
using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

// Each "JPEG" is the text "jpeg-{side}", so a test can tell which size landed in which file.
internal sealed class FakePhotoResizer : IPhotoResizer
{
    // The source isn't an image (ResizeToJpegAsync returns null).
    public bool Unreadable { get; set; }

    // Fails like an out-of-memory decode or a storage hiccup would.
    public bool Throws { get; set; }

    public int Calls { get; private set; }

    public Task<IReadOnlyList<byte[]>?> ResizeToJpegAsync(Stream source, IReadOnlyList<int> longestSides, CancellationToken cancellationToken)
    {
        Calls++;
        if (Throws)
        {
            throw new InvalidOperationException("Resize failed.");
        }

        return Task.FromResult<IReadOnlyList<byte[]>?>(
            Unreadable ? null : longestSides.Select(side => Encoding.UTF8.GetBytes($"jpeg-{side}")).ToList());
    }
}
