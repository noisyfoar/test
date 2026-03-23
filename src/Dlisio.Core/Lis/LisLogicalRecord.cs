namespace Dlisio.Core.Lis
{
    public sealed class LisLogicalRecord
    {
        public LisLogicalRecord(
            LisLogicalRecordHeader header,
            byte[] data,
            int physicalRecordCount)
        {
            Header = header;
            Data = data;
            PhysicalRecordCount = physicalRecordCount;
        }

        public LisLogicalRecordHeader Header { get; }

        public byte[] Data { get; }

        public int PhysicalRecordCount { get; }
    }
}
