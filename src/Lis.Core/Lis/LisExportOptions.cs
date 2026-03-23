namespace Lis.Core.Lis
{
    public sealed class LisExportOptions
    {
        public LisExportOptions(ushort maxPhysicalRecordLength = ushort.MaxValue)
        {
            MaxPhysicalRecordLength = maxPhysicalRecordLength;
        }

        public ushort MaxPhysicalRecordLength { get; }
    }
}
