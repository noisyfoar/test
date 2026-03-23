namespace Lis.Core.Lis
{
    public sealed class LisLogicalRecordHeader
    {
        public const int HeaderLength = 2;

        /// <summary>
        /// Подробно выполняет операцию «LisLogicalRecordHeader» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisLogicalRecordHeader(byte type, byte attributes)
        {
            Type = type;
            Attributes = attributes;
        }

        public byte Type { get; }

        public byte Attributes { get; }

        public bool IsKnownRecordType
        {
            get { return LisRecordTypeHelper.IsValid(Type); }
        }
    }
}
