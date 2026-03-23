namespace Lis.Core.Lis
{
    public sealed class LisFrameChannelData
    {
        /// <summary>
        /// Подробно выполняет операцию «LisFrameChannelData» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisFrameChannelData(string mnemonic, object[] samples)
        {
            Mnemonic = mnemonic;
            Samples = samples;
        }

        public string Mnemonic { get; }

        public object[] Samples { get; }
    }
}
