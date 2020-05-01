using System.Collections.Generic;
using System.Linq;
using ExternalLCDIntegration.Models;

namespace ExternalLCDIntegration.Services
{
    public static class ArrayService
    {
        public static byte[] CreateByteArray(ScreenLedCountModel ledCountModel)
        {
            var newLength = ledCountModel.TotalSum() * 3;
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

        public static byte[] ConvertToSingleArray(byte[][] jaggedArray)
        {
            var returnArray = new byte[0];
            if (jaggedArray.Length <= 0) 
                return returnArray;
            IEnumerable<byte> collection = jaggedArray[0];
            if (jaggedArray.Length > 1)
            {
                collection = jaggedArray.Aggregate(collection, (current, ledArray) => current.Concat(ledArray));
            }
            returnArray = collection.ToArray();

            return returnArray;
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