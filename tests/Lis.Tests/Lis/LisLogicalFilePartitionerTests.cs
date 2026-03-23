using System;
using Lis.Core.Lis;
using Xunit;

namespace Lis.Tests.Lis
{
    public sealed class LisLogicalFilePartitionerTests
    {
        [Fact]
        public void Partition_NullIndex_ThrowsArgumentNullException()
        {
            var partitioner = new LisLogicalFilePartitioner();
            Assert.Throws<ArgumentNullException>(() => partitioner.Partition(null!));
        }

        [Fact]
        public void Partition_EmptyIndex_ReturnsEmptyCollection()
        {
            var partitioner = new LisLogicalFilePartitioner();
            var index = new LisRecordIndex(Array.Empty<LisRecordInfo>());

            var files = partitioner.Partition(index);

            Assert.Empty(files);
        }

        [Fact]
        public void Partition_SingleCompleteLogicalFile_ReturnsCompleteFile()
        {
            var index = new LisRecordIndex(new[]
            {
                MakeInfo(0, LisRecordType.TapeHeader),
                MakeInfo(100, LisRecordType.FileHeader),
                MakeInfo(200, LisRecordType.DataFormatSpecification),
                MakeInfo(300, LisRecordType.NormalData),
                MakeInfo(400, LisRecordType.FileTrailer),
                MakeInfo(500, LisRecordType.TapeTrailer),
            });

            var partitioner = new LisLogicalFilePartitioner();
            var files = partitioner.Partition(index);

            Assert.Single(files);
            Assert.True(files[0].IsComplete);
            Assert.NotNull(files[0].FileHeader);
            Assert.NotNull(files[0].FileTrailer);
            Assert.Equal(4, files[0].Records.Count);
            Assert.Equal(LisRecordType.FileHeader, files[0].Records[0].Type);
            Assert.Equal(LisRecordType.FileTrailer, files[0].Records[3].Type);
        }

        [Fact]
        public void Partition_MissingTrailer_ReturnsIncompleteFile()
        {
            var index = new LisRecordIndex(new[]
            {
                MakeInfo(100, LisRecordType.FileHeader),
                MakeInfo(200, LisRecordType.DataFormatSpecification),
                MakeInfo(300, LisRecordType.NormalData),
            });

            var partitioner = new LisLogicalFilePartitioner();
            var files = partitioner.Partition(index);

            Assert.Single(files);
            Assert.False(files[0].IsComplete);
            Assert.NotNull(files[0].FileHeader);
            Assert.Null(files[0].FileTrailer);
            Assert.Equal(3, files[0].Records.Count);
        }

        [Fact]
        public void Partition_TwoFiles_SeparatesByHeadersAndTrailers()
        {
            var index = new LisRecordIndex(new[]
            {
                MakeInfo(100, LisRecordType.FileHeader),
                MakeInfo(110, LisRecordType.NormalData),
                MakeInfo(120, LisRecordType.FileTrailer),

                MakeInfo(200, LisRecordType.FileHeader),
                MakeInfo(210, LisRecordType.DataFormatSpecification),
                MakeInfo(220, LisRecordType.AlternateData),
                MakeInfo(230, LisRecordType.FileTrailer),
            });

            var partitioner = new LisLogicalFilePartitioner();
            var files = partitioner.Partition(index);

            Assert.Equal(2, files.Count);
            Assert.True(files[0].IsComplete);
            Assert.True(files[1].IsComplete);
            Assert.Equal(3, files[0].Records.Count);
            Assert.Equal(4, files[1].Records.Count);
        }

        [Fact]
        public void Partition_NewHeaderBeforeTrailer_ClosesPreviousAsIncomplete()
        {
            var index = new LisRecordIndex(new[]
            {
                MakeInfo(100, LisRecordType.FileHeader),
                MakeInfo(110, LisRecordType.NormalData),
                MakeInfo(200, LisRecordType.FileHeader),
                MakeInfo(210, LisRecordType.NormalData),
                MakeInfo(220, LisRecordType.FileTrailer),
            });

            var partitioner = new LisLogicalFilePartitioner();
            var files = partitioner.Partition(index);

            Assert.Equal(2, files.Count);
            Assert.False(files[0].IsComplete);
            Assert.True(files[1].IsComplete);
            Assert.Equal(2, files[0].Records.Count);
            Assert.Equal(3, files[1].Records.Count);
        }

        [Fact]
        public void Partition_RecordsBeforeFirstHeader_AreIgnored()
        {
            var index = new LisRecordIndex(new[]
            {
                MakeInfo(0, LisRecordType.TapeHeader),
                MakeInfo(1, LisRecordType.ReelHeader),
                MakeInfo(100, LisRecordType.FileHeader),
                MakeInfo(200, LisRecordType.FileTrailer),
            });

            var partitioner = new LisLogicalFilePartitioner();
            var files = partitioner.Partition(index);

            Assert.Single(files);
            Assert.Equal(2, files[0].Records.Count);
            Assert.Equal(LisRecordType.FileHeader, files[0].Records[0].Type);
        }

        [Fact]
        public void LogicalFile_OfType_FiltersRecords()
        {
            var index = new LisRecordIndex(new[]
            {
                MakeInfo(100, LisRecordType.FileHeader),
                MakeInfo(110, LisRecordType.NormalData),
                MakeInfo(120, LisRecordType.AlternateData),
                MakeInfo(130, LisRecordType.FileTrailer),
            });

            var partitioner = new LisLogicalFilePartitioner();
            var files = partitioner.Partition(index);

            Assert.Single(files);
            var implicitRecords = files[0].OfType(LisRecordType.NormalData);
            Assert.Single(implicitRecords);
            Assert.Equal(LisRecordType.NormalData, implicitRecords[0].Type);
        }

        private static LisRecordInfo MakeInfo(long offset, LisRecordType type)
        {
            return new LisRecordInfo(offset, type, headerAttributes: 0, physicalRecordCount: 1, dataLength: 0);
        }
    }
}
