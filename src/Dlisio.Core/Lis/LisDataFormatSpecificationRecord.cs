using System.Collections.Generic;

namespace Dlisio.Core.Lis
{
    public sealed class LisDataFormatSpecificationRecord
    {
        public LisDataFormatSpecificationRecord(
            IReadOnlyList<LisDfsrEntryBlock> entries,
            IReadOnlyList<LisDfsrSpecBlock> specBlocks,
            byte subtype)
        {
            Entries = entries;
            SpecBlocks = specBlocks;
            Subtype = subtype;
        }

        public IReadOnlyList<LisDfsrEntryBlock> Entries { get; }

        public IReadOnlyList<LisDfsrSpecBlock> SpecBlocks { get; }

        public byte Subtype { get; }
    }
}
