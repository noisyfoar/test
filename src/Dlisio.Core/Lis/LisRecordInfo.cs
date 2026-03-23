namespace Dlisio.Core.Lis
{
    public sealed class LisRecordInfo
    {
        public LisRecordInfo(
            long offset,
            LisRecordType type,
            byte headerAttributes,
            int physicalRecordCount,
            int dataLength)
        {
            Offset = offset;
            Type = type;
            HeaderAttributes = headerAttributes;
            PhysicalRecordCount = physicalRecordCount;
            DataLength = dataLength;
        }

        public long Offset { get; }

        public LisRecordType Type { get; }

        public byte HeaderAttributes { get; }

        public int PhysicalRecordCount { get; }

        public int DataLength { get; }

        public bool IsImplicitRecord
        {
            get { return Type == LisRecordType.NormalData || Type == LisRecordType.AlternateData; }
        }
    }
}
