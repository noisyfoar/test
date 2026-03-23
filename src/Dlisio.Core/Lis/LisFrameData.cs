using System.Collections.Generic;

namespace Dlisio.Core.Lis
{
    public sealed class LisFrameData
    {
        public LisFrameData(IReadOnlyList<LisFrameChannelData> channels)
        {
            Channels = channels;
        }

        public IReadOnlyList<LisFrameChannelData> Channels { get; }
    }
}
