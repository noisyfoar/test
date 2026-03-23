namespace Lis.Core.Lis
{
    public sealed class LisRecordInfo
    {
        /// <summary>
        /// Подробно выполняет операцию «LisRecordInfo» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisRecordInfo(
            long offset,
            LisRecordType type,
            byte headerAttributes,
            int physicalRecordCount,
            int dataLength)
        {
            Offset = offset;
            Type = type;
            HeaderAttributes = headerAttributes;
            PhysicalRecordCount = physicalRecordCount;
            DataLength = dataLength;
        }

        public long Offset { get; }

        public LisRecordType Type { get; }

        public byte HeaderAttributes { get; }

        public int PhysicalRecordCount { get; }

        public int DataLength { get; }

        public bool IsImplicitRecord
        {
            get { return Type == LisRecordType.NormalData || Type == LisRecordType.AlternateData; }
        }
    }
}
