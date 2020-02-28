using System;
using System.Collections.Generic;
using System.Text;

namespace ExternalLCDIntegration.Models
{
    public class LedReadingRequest
    {
        public int PrimaryDimension { get; set; }
        public int SecondaryDimension { get; set; }
        public int SideLedCount { get; set; }
        public int CurrentLedCount { get; set; }
        public int Stride { get; set; }
        public int BPPModifier { get; set; }
        public IntPtr ScreenPointer { get; set; }
        public byte[] ColourArray { get; set; }
        public bool StartFromZero { get; set; }
        
    }
}
