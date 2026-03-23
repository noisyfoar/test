namespace Lis.Core.Lis
{
    public sealed class LisPhysicalRecordHeader
    {
        public const int HeaderLength = 4;

        // LIS79 PRH attribute bits.
        public const ushort AttributeChecksum = 0x3000;
        public const ushort AttributeFileNumber = 0x0400;
        public const ushort AttributeRecordNumber = 0x0200;
        public const ushort AttributePredecessor = 0x0002;
        public const ushort AttributeSuccessor = 0x0001;

        public LisPhysicalRecordHeader(ushort length, ushort attributes)
        {
            Length = length;
            Attributes = attributes;
        }

        public ushort Length { get; }

        public ushort Attributes { get; }

        public bool HasPredecessor
        {
            get { return (Attributes & AttributePredecessor) != 0; }
        }

        public bool HasSuccessor
        {
            get { return (Attributes & AttributeSuccessor) != 0; }
        }

        public bool HasRecordNumberTrailer
        {
            get { return (Attributes & AttributeRecordNumber) != 0; }
        }

        public bool HasFileNumberTrailer
        {
            get { return (Attributes & AttributeFileNumber) != 0; }
        }

        public bool HasChecksumTrailer
        {
            get { return (Attributes & AttributeChecksum) != 0; }
        }

        public int TrailerLength
        {
            get
            {
                int trailerLength = 0;
                if (HasRecordNumberTrailer)
                {
                    trailerLength += 2;
                }

                if (HasFileNumberTrailer)
                {
                    trailerLength += 2;
                }

                if (HasChecksumTrailer)
                {
                    trailerLength += 2;
                }

                return trailerLength;
            }
        }

        public int MinimumValidLength
        {
            get
            {
                // First PR in a LR must include LRH (2 bytes). Continuation PR does not.
                int baseLength = HasPredecessor ? HeaderLength : HeaderLength + LisLogicalRecordHeader.HeaderLength;
                return baseLength + TrailerLength;
            }
        }
    }
}
