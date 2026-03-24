namespace Lis.Core.Lis
{
    public sealed class LisDfsrEntryBlock
    {
        /// <summary>
        /// Подробно выполняет операцию «LisDfsrEntryBlock» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisDfsrEntryBlock(
            byte type,
            byte size,
            byte representationCode,
            byte[] valueBytes,
            int? numericValue,
            string? textValue)
        {
            Type = type;
            Size = size;
            RepresentationCode = representationCode;
            ValueBytes = valueBytes;
            NumericValue = numericValue;
            TextValue = textValue;
        }

        public byte Type { get; }

        public byte Size { get; }

        public byte RepresentationCode { get; }

        public byte[] ValueBytes { get; }

        public int? NumericValue { get; }

        public string? TextValue { get; }

        public bool IsTerminator
        {
            get { return Type == (byte)LisDfsrEntryType.Terminator; }
        }
    }
}
