using ExternalLCDIntegration.Models;

namespace ExternalLCDIntegration.Services
{
    public static class ArrayService
    {
        public static byte[] CreateByteArray(int _horizontalLedCountTop, int _horizontalLedCountBottom, int _verticalLedCountLeft, int _verticalLedCountRight)
        {
            var newLength = (_horizontalLedCountTop + _horizontalLedCountBottom + _verticalLedCountLeft + _verticalLedCountRight) * 3;
            var arrayRGB = new byte[newLength];
            return arrayRGB;
        }

        public static byte[] AdColourToByteArray(byte[] array, AverageColour colourModel, int position)
        {
            var index = position * 3;
            array[index++] = colourModel.AverageR;
            array[index++] = colourModel.AverageG;
            array[index] = colourModel.AverageB;
            return array;
        }

        public static byte[] FillByteArray(byte[] array, byte R, byte G, byte B)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                array[++i] = R;
                array[++i] = G;
                array[i] = B;
            }
            return array;
        }
    }
}