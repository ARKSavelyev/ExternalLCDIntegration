using System;
using System.Collections.Generic;
using System.Text;

namespace ExternalLCDIntegration.Extensions
{
    public static class TypeExtensions
    {
        public static void CleanArray(this long[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = 0;
            }
        }
    }
}
