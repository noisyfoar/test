using System;
using Dlisio.Core.Lis;
using Xunit;

namespace Dlisio.Tests.Lis
{
    public sealed class LisFdataParserTests
    {
        [Fact]
        public void ParseFrames_SingleFrame_ParsesChannelSamples()
        {
            var format = BuildFormat(
                BuildSpec("DEPTH", samples: 1, reprc: (byte)LisRepresentationCode.Int16),
                BuildSpec("GR", samples: 2, reprc: (byte)LisRepresentationCode.Byte));

            // DEPTH: 0x012C = 300, GR: 10, 20
            var record = BuildFdataRecord(LisRecordType.NormalData, new byte[] { 0x01, 0x2C, 0x0A, 0x14 });
            var parser = new LisFdataParser();

            var frames = parser.ParseFrames(record, format);

            Assert.Single(frames);
            Assert.Equal(2, frames[0].Channels.Count);
            Assert.Equal("DEPTH", frames[0].Channels[0].Mnemonic);
            Assert.Single(frames[0].Channels[0].Samples);
            Assert.Equal((short)300, frames[0].Channels[0].Samples[0]);
            Assert.Equal("GR", frames[0].Channels[1].Mnemonic);
            Assert.Equal(2, frames[0].Channels[1].Samples.Length);
            Assert.Equal((byte)10, frames[0].Channels[1].Samples[0]);
            Assert.Equal((byte)20, frames[0].Channels[1].Samples[1]);
        }

        [Fact]
        public void ParseFrames_MultipleFrames_ParsesAllFrames()
        {
            var format = BuildFormat(
                BuildSpec("IDX", samples: 1, reprc: (byte)LisRepresentationCode.Int16),
                BuildSpec("C1", samples: 1, reprc: (byte)LisRepresentationCode.Int8));

            // Frame0: IDX=1, C1=-2 ; Frame1: IDX=2, C1=3
            var record = BuildFdataRecord(LisRecordType.AlternateData, new byte[]
            {
                0x00, 0x01, 0xFE,
                0x00, 0x02, 0x03
            });
            var parser = new LisFdataParser();

            var frames = parser.ParseFrames(record, format);

            Assert.Equal(2, frames.Count);
            Assert.Equal((short)1, frames[0].Channels[0].Samples[0]);
            Assert.Equal((sbyte)-2, frames[0].Channels[1].Samples[0]);
            Assert.Equal((short)2, frames[1].Channels[0].Samples[0]);
            Assert.Equal((sbyte)3, frames[1].Channels[1].Samples[0]);
        }

        [Fact]
        public void ParseFrames_FloatRepCode_ReturnsFloatValue()
        {
            var format = BuildFormat(
                BuildSpec("FS", samples: 1, reprc: (byte)LisRepresentationCode.Float32));

            var record = BuildFdataRecord(LisRecordType.NormalData, new byte[] { 0x00, 0x00, 0x00, 0x00 });
            var parser = new LisFdataParser();

            var frames = parser.ParseFrames(record, format);

            Assert.Single(frames);
            Assert.Equal(0.0f, (float)frames[0].Channels[0].Samples[0]);
        }

        [Fact]
        public void ParseFrames_SelectedMnemonics_ReturnsOnlyRequestedChannels()
        {
            var format = BuildFormat(
                BuildSpec("DEPTH", samples: 1, reprc: (byte)LisRepresentationCode.Int16),
                BuildSpec("GR", samples: 1, reprc: (byte)LisRepresentationCode.Byte));

            var record = BuildFdataRecord(LisRecordType.NormalData, new byte[] { 0x00, 0x64, 0x2A });
            var parser = new LisFdataParser();
            var selected = new[] { "GR" };
            var metrics = new LisReadMetrics();

            var frames = parser.ParseFrames(record, format, new System.Collections.Generic.HashSet<string>(selected), metrics);

            Assert.Single(frames);
            Assert.Single(frames[0].Channels);
            Assert.Equal("GR", frames[0].Channels[0].Mnemonic);
            Assert.Equal((byte)0x2A, frames[0].Channels[0].Samples[0]);
            Assert.Equal(1, metrics.SamplesDecoded);
            Assert.Equal(1, metrics.SamplesSkipped);
        }

        [Fact]
        public void AccumulateCurves_SelectedMnemonics_CollectsOnlyRequestedCurve()
        {
            var format = BuildFormat(
                BuildSpec("DEPTH", samples: 1, reprc: (byte)LisRepresentationCode.Int16),
                BuildSpec("GR", samples: 1, reprc: (byte)LisRepresentationCode.Byte));

            var record = BuildFdataRecord(LisRecordType.NormalData, new byte[] { 0x00, 0x64, 0x2A });
            var parser = new LisFdataParser();
            var selected = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GR" };
            var curves = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object>>(StringComparer.OrdinalIgnoreCase);
            var metrics = new LisReadMetrics();

            parser.AccumulateCurves(record, format, curves, selected, metrics);

            Assert.Single(curves);
            Assert.True(curves.ContainsKey("GR"));
            Assert.Single(curves["GR"]);
            Assert.Equal((byte)0x2A, curves["GR"][0]);
            Assert.Equal(1, metrics.SamplesDecoded);
            Assert.Equal(1, metrics.SamplesSkipped);
        }

        [Fact]
        public void ParseFrames_InvalidRecordType_ThrowsLisParseException()
        {
            var format = BuildFormat(BuildSpec("IDX", 1, (byte)LisRepresentationCode.Int16));
            var record = BuildFdataRecord(LisRecordType.FileHeader, new byte[] { 0x00, 0x01 });
            var parser = new LisFdataParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.ParseFrames(record, format));

            Assert.Contains("record type", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseFrames_EmptySpecBlocks_ThrowsLisParseException()
        {
            var format = new LisDataFormatSpecificationRecord(
                Array.Empty<LisDfsrEntryBlock>(),
                Array.Empty<LisDfsrSpecBlock>(),
                subtype: 0);

            var record = BuildFdataRecord(LisRecordType.NormalData, Array.Empty<byte>());
            var parser = new LisFdataParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.ParseFrames(record, format));

            Assert.Contains("no spec blocks", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseFrames_MisalignedPayload_ThrowsLisParseException()
        {
            var format = BuildFormat(
                BuildSpec("IDX", samples: 1, reprc: (byte)LisRepresentationCode.Int16),
                BuildSpec("C1", samples: 1, reprc: (byte)LisRepresentationCode.Byte));

            var record = BuildFdataRecord(LisRecordType.NormalData, new byte[] { 0x00, 0x01, 0x02, 0x03 });
            var parser = new LisFdataParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.ParseFrames(record, format));

            Assert.Contains("aligned", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseFrames_VariableLengthRepCode_ThrowsLisParseException()
        {
            var format = BuildFormat(
                BuildSpec("TXT", samples: 1, reprc: (byte)LisRepresentationCode.String));
            var record = BuildFdataRecord(LisRecordType.NormalData, new byte[] { 0x04, 0x41, 0x42, 0x43, 0x44 });
            var parser = new LisFdataParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.ParseFrames(record, format));

            Assert.Contains("Variable-length", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseFrames_NullArguments_ThrowArgumentNullException()
        {
            var parser = new LisFdataParser();
            var format = BuildFormat(BuildSpec("IDX", 1, (byte)LisRepresentationCode.Int16));
            var record = BuildFdataRecord(LisRecordType.NormalData, new byte[] { 0x00, 0x01 });

            Assert.Throws<ArgumentNullException>(() => parser.ParseFrames(null!, format));
            Assert.Throws<ArgumentNullException>(() => parser.ParseFrames(record, null!));
        }

        private static LisDataFormatSpecificationRecord BuildFormat(params LisDfsrSpecBlock[] specs)
        {
            return new LisDataFormatSpecificationRecord(
                Array.Empty<LisDfsrEntryBlock>(),
                specs,
                subtype: 0);
        }

        private static LisDfsrSpecBlock BuildSpec(string mnemonic, byte samples, byte reprc)
        {
            return new LisDfsrSpecBlock(
                subtype: 0,
                mnemonic: mnemonic,
                serviceId: string.Empty,
                serviceOrderNumber: string.Empty,
                units: string.Empty,
                fileNumber: 0,
                reservedSize: 0,
                samples: samples,
                representationCode: reprc,
                apiLogType: 0,
                apiCurveType: 0,
                apiCurveClass: 0,
                apiModifier: 0,
                processLevel: 0,
                apiCodes: 0,
                processIndicators: Array.Empty<byte>());
        }

        private static LisLogicalRecord BuildFdataRecord(LisRecordType type, byte[] data)
        {
            return new LisLogicalRecord(
                new LisLogicalRecordHeader((byte)type, 0),
                data,
                physicalRecordCount: 1);
        }
    }
}
