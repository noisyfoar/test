using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisLogicalFileData
    {
        public LisLogicalFileData(
            LisFileHeaderRecord? fileHeader,
            LisFileTrailerRecord? fileTrailer,
            IReadOnlyList<LisTextRecord> textRecords,
            IReadOnlyList<LisDataFormatSpecificationRecord> dataFormatSpecifications,
            IReadOnlyList<LisFrameData> frames,
            IReadOnlyDictionary<string, IReadOnlyList<object>>? curves = null)
        {
            FileHeader = fileHeader;
            FileTrailer = fileTrailer;
            TextRecords = textRecords;
            DataFormatSpecifications = dataFormatSpecifications;
            Frames = frames;
            Curves = curves ?? EmptyCurves;
        }

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<object>> EmptyCurves =
            new Dictionary<string, IReadOnlyList<object>>();

        public LisFileHeaderRecord? FileHeader { get; }

        public LisFileTrailerRecord? FileTrailer { get; }

        public IReadOnlyList<LisTextRecord> TextRecords { get; }

        public IReadOnlyList<LisDataFormatSpecificationRecord> DataFormatSpecifications { get; }

        public IReadOnlyList<LisFrameData> Frames { get; }

        public IReadOnlyDictionary<string, IReadOnlyList<object>> Curves { get; }
    }
}
