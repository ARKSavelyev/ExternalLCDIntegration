using System;
using System.Collections.Generic;
using System.Text;

namespace ExternalLCDIntegration.Models
{
    public class SideLedReadingRequest
    {
        public int X { get; set; }//Primary Dimension
        public int Y { get; set; }//Secondary Dimension
        public int SideLedCount { get; set; }
        public int CurrentLedCount { get; set; }
        public int Stride { get; set; }
        public int BPPModifier { get; set; }
        public IntPtr ScreenPointer { get; set; }
        public byte[] ColourArray { get; set; }
        public bool StartFromZero { get; set; }

        public bool isHorizontal { get; set; }

    }
}
