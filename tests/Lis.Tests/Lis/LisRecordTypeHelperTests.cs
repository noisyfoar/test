using Lis.Core.Lis;
using Xunit;

namespace Lis.Tests.Lis
{
    public sealed class LisRecordTypeHelperTests
    {
        [Theory]
        [InlineData((byte)LisRecordType.NormalData)]
        [InlineData((byte)LisRecordType.FileHeader)]
        [InlineData((byte)LisRecordType.FileTrailer)]
        [InlineData((byte)LisRecordType.DataFormatSpecification)]
        [InlineData((byte)LisRecordType.OperatorCommandInputs)]
        [InlineData((byte)LisRecordType.BlankRecord)]
        public void IsValid_KnownTypes_ReturnsTrue(byte value)
        {
            Assert.True(LisRecordTypeHelper.IsValid(value));
        }

        [Theory]
        [InlineData((byte)2)]
        [InlineData((byte)31)]
        [InlineData((byte)33)]
        [InlineData((byte)90)]
        [InlineData((byte)200)]
        [InlineData((byte)255)]
        public void IsValid_UnknownTypes_ReturnsFalse(byte value)
        {
            Assert.False(LisRecordTypeHelper.IsValid(value));
        }
    }
}
