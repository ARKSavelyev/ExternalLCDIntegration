using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExternalLCDIntegration.Models;

namespace ExternalLCDIntegration.Services
{
    public static class ArrayService
    {
        /// <summary>
        /// create a byte array based on total led count.
        /// </summary>
        /// <param name="ledCountModel"></param>
        /// <returns></returns>
        public static byte[] CreateByteArray(ScreenLedCountModel ledCountModel)
        {
            var newLength = ledCountModel.TotalSum() * 3;
            var arrayRGB = new byte[newLength];
            return arrayRGB;
        }

        /// <summary>
        /// Add a single colour reading to the array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="colourModel"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static byte[] AdColourToByteArray(byte[] array, AverageColour colourModel, int position)
        {
            var index = position * 3;
            array[index++] = colourModel.AverageR;
            array[index++] = colourModel.AverageG;
            array[index] = colourModel.AverageB;
            return array;
        }

        /// <summary>
        /// converts the jagged array into a sequential one, for LED Readings.
        /// </summary>
        /// <param name="jaggedArray"></param>
        /// <returns></returns>
        public static byte[] ConvertToSingleArray(byte[][] jaggedArray)
        {
            var returnArray = new byte[0];
            if (jaggedArray.Length <= 0) 
                return returnArray;
            IEnumerable<byte> collection = new List<byte>();
            collection = jaggedArray.Length > 1 ? jaggedArray.Aggregate(collection, (current, ledArray) => current.Concat(ledArray)) : jaggedArray[0];
            returnArray = collection.ToArray();

            return returnArray;
        }


        /// <summary>
        /// Fills entire array with same colour data, for testing and uniform presentation.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Return an array of byte arrays Tasks, for parallel screen readings.
        /// </summary>
        /// <param name="arrayLength"></param>
        /// <returns></returns>
        public static Task<byte[]>[] CreateTaskByteArray(int arrayLength)
        {
            return new Task<byte[]>[arrayLength];
        }

        public static Task<AverageColour>[] CreateTaskAverageColourArray(int arrayLength)
        {
            return new Task<AverageColour>[arrayLength];
        }

        public static byte[][] AwaitTaskByteArray(Task<byte[]>[] taskArray)
        {
            var length = taskArray.Length;
            var resultsArray = new byte[length][];
            for (var loopCount = 0; loopCount < length; loopCount++)
            {
                resultsArray[loopCount] = taskArray[loopCount].Result;
            }
            return resultsArray;
        }

        public static AverageColour[] AwaitTaskAverageColourArray(Task<AverageColour>[] taskArray)
        {
            var length = taskArray.Length;
            var resultsArray = new AverageColour[length];
            for (var loopCount = 0; loopCount < length; loopCount++)
            {
                resultsArray[loopCount] = taskArray[loopCount].Result;
            }
            return resultsArray;
        }
    }
}