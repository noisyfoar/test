namespace Dlisio.Core.Lis
{
    public sealed class LisLogicalRecordHeader
    {
        public const int HeaderLength = 2;

        public LisLogicalRecordHeader(byte type, byte attributes)
        {
            Type = type;
            Attributes = attributes;
        }

        public byte Type { get; }

        public byte Attributes { get; }

        public bool IsKnownRecordType
        {
            get { return LisRecordTypeHelper.IsValid(Type); }
        }
    }
}
