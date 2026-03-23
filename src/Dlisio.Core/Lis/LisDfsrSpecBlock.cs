namespace Dlisio.Core.Lis
{
    public sealed class LisDfsrSpecBlock
    {
        public LisDfsrSpecBlock(
            byte subtype,
            string mnemonic,
            string serviceId,
            string serviceOrderNumber,
            string units,
            short fileNumber,
            short reservedSize,
            byte samples,
            byte representationCode,
            byte apiLogType,
            byte apiCurveType,
            byte apiCurveClass,
            byte apiModifier,
            byte processLevel,
            int apiCodes,
            byte[] processIndicators)
        {
            Subtype = subtype;
            Mnemonic = mnemonic;
            ServiceId = serviceId;
            ServiceOrderNumber = serviceOrderNumber;
            Units = units;
            FileNumber = fileNumber;
            ReservedSize = reservedSize;
            Samples = samples;
            RepresentationCode = representationCode;
            ApiLogType = apiLogType;
            ApiCurveType = apiCurveType;
            ApiCurveClass = apiCurveClass;
            ApiModifier = apiModifier;
            ProcessLevel = processLevel;
            ApiCodes = apiCodes;
            ProcessIndicators = processIndicators;
        }

        public byte Subtype { get; }

        public string Mnemonic { get; }

        public string ServiceId { get; }

        public string ServiceOrderNumber { get; }

        public string Units { get; }

        public short FileNumber { get; }

        public short ReservedSize { get; }

        public byte Samples { get; }

        public byte RepresentationCode { get; }

        // Subtype 0 fields.
        public byte ApiLogType { get; }

        public byte ApiCurveType { get; }

        public byte ApiCurveClass { get; }

        public byte ApiModifier { get; }

        public byte ProcessLevel { get; }

        // Subtype 1 fields.
        public int ApiCodes { get; }

        public byte[] ProcessIndicators { get; }
    }
}
