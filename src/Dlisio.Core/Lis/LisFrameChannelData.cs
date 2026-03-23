namespace Dlisio.Core.Lis
{
    public sealed class LisFrameChannelData
    {
        public LisFrameChannelData(string mnemonic, object[] samples)
        {
            Mnemonic = mnemonic;
            Samples = samples;
        }

        public string Mnemonic { get; }

        public object[] Samples { get; }
    }
}
