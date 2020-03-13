using System;
using System.Collections.Generic;
using System.Text;

namespace ExternalLCDIntegration.Models
{
    public class BaseLedRequest
    {
        public IntPtr ScreenPointer { get; set; }
        public int Stride { get; set; }
        public int BPPModifier { get; set; }
    }

    public class SideLedReadingRequest : BaseLedRequest
    {
        public int X { get; set; }//Primary Dimension
        public int Y { get; set; }//Secondary Dimension
        public int Depth { get; set; }
        public int SideLedCount { get; set; }
        public int CurrentLedCount { get; set; }
        public byte[] ColourArray { get; set; }
        public bool StartFromZero { get; set; }
        public bool IsHorizontal { get; set; }
        public bool IsIncremental { get; set; }
    }

    public class ScreenSectionReadingRequest : BaseLedRequest
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
    }

    public class ScreenLedCountModel
    {
        public byte HorizontalLedCountTop { get; set; }
        public byte HorizontalLedCountBottom { get; set; }
        public byte VerticalLedCountLeft { get; set; }
        public byte VerticalLedCountRight { get; set; }

        public int TotalSum()
        {
            return HorizontalLedCountBottom + HorizontalLedCountTop + VerticalLedCountLeft + VerticalLedCountRight;
        }
    }
}
