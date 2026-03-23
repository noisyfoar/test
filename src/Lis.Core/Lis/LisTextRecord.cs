namespace Lis.Core.Lis
{
    public sealed class LisTextRecord
    {
        public LisTextRecord(LisRecordType type, string message)
        {
            Type = type;
            Message = message;
        }

        public LisRecordType Type { get; }

        public string Message { get; }
    }
}
