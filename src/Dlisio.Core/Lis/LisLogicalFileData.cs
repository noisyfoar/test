using System.Collections.Generic;

namespace Dlisio.Core.Lis
{
    public sealed class LisLogicalFileData
    {
        public LisLogicalFileData(
            LisFileHeaderRecord? fileHeader,
            LisFileTrailerRecord? fileTrailer,
            IReadOnlyList<LisTextRecord> textRecords,
            IReadOnlyList<LisDataFormatSpecificationRecord> dataFormatSpecifications,
            IReadOnlyList<LisFrameData> frames)
        {
            FileHeader = fileHeader;
            FileTrailer = fileTrailer;
            TextRecords = textRecords;
            DataFormatSpecifications = dataFormatSpecifications;
            Frames = frames;
        }

        public LisFileHeaderRecord? FileHeader { get; }

        public LisFileTrailerRecord? FileTrailer { get; }

        public IReadOnlyList<LisTextRecord> TextRecords { get; }

        public IReadOnlyList<LisDataFormatSpecificationRecord> DataFormatSpecifications { get; }

        public IReadOnlyList<LisFrameData> Frames { get; }
    }
}
