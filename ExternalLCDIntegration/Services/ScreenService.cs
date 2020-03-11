using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using ExternalLCDIntegration.Extensions;
using ExternalLCDIntegration.Models;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ExternalLCDIntegration.Services
{
    public static class ScreenService
    {

        public static Bitmap GetScreenBitmap(int screenWidth, int screenHeight, Size size)
        {
           return CopyFromTheScreen(CreateBitmap(screenWidth, screenHeight), size);
        }

        public static Bitmap CreateBitmap(int screenWidth, int screenHeight)
        {
            return new Bitmap(screenWidth, screenHeight);
        }

        public static Bitmap CopyFromTheScreen(Bitmap screenBitmap, Size size)
        {
            using (var g = Graphics.FromImage(screenBitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, size);
            }
            return screenBitmap;
        }

        public static void GetScreenResolution(out int screenWidth, out int screenHeight)
        {
            screenWidth = (int)Math.Floor(SystemParameters.PrimaryScreenWidth);
            screenHeight = (int)Math.Floor(SystemParameters.PrimaryScreenHeight);
        }

        public static byte GetAverageColour(long total, int count)
        {
            return (byte)(total / count);
        }

        public static string PrintRGB(int avrB, int avrG, int avrR)
        {
            return $"R: {avrR.ToString()} G: {avrG.ToString()} B: {avrB.ToString()}";
        }

        private static AverageColour GetSectionLED(ScreenSectionReadingRequest request)
        {
            var totals = new long[] { 0, 0, 0 };
            var pixelCount = 0;
            unsafe
            {
                var p = (byte*) (void*)request.ScreenPointer;
                for (var y = request.StartY; y < request.EndY; y++)
                {
                    for (var x = request.StartX; x < request.EndX; x++)
                    {
                        for (var color = 0; color < 3; color++)
                        {
                            var idx = y * request.Stride + x * request.BPPModifier + color;
                            totals[color] += p[idx];
                        }
                        pixelCount++;
                    }
                }
            }
            return new AverageColour
            {
                AverageB = GetAverageColour(totals[0], pixelCount),
                AverageG = GetAverageColour(totals[1], pixelCount),
                AverageR = GetAverageColour(totals[2], pixelCount)
            };
        }

        private static void GetStartAndEndSecondarySide(bool startFromZero,int depth, int dimension,out int start,out int end)
        {
            if (startFromZero)
            {
                start = 0;
                end = dimension / depth;
            }
            else
            {
                start = dimension - dimension / depth;
                end = dimension;
            }
        }

        private static ScreenSectionReadingRequest CreateSectionReadingRequest(SideLedReadingRequest request)
        {
            return new ScreenSectionReadingRequest
            {
                ScreenPointer = request.ScreenPointer,
                Stride = request.Stride,
                BPPModifier = request.BPPModifier
            };
        }

        private static byte[] GetHorizontalSide(SideLedReadingRequest request)
        {
            var blockX = request.X / request.SideLedCount;
            var sectionRequest = CreateSectionReadingRequest(request);
            GetStartAndEndSecondarySide(request.StartFromZero,request.Depth, request.Y, out var start, out var end);
            sectionRequest.StartY = start;
            sectionRequest.EndY = end;
            if (request.IsIncremental)
            {
                for (var count = 0; count < request.SideLedCount - 1; count++)
                {
                    sectionRequest.StartX = count * blockX;
                    sectionRequest.EndX = sectionRequest.StartX + blockX;
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
                }
                sectionRequest.StartX = blockX * (request.SideLedCount - 1);
                sectionRequest.EndX = request.X;
                request.ColourArray = 
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount);
            }
            sectionRequest.StartX = blockX * request.SideLedCount - 1;
            sectionRequest.EndX = request.X;
            request.ColourArray = 
                ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount);
            for (var count = request.SideLedCount - 2; count >= 0; count--) 
            {
                sectionRequest.StartX = count * blockX;
                sectionRequest.EndX = sectionRequest.StartX + blockX;
                request.ColourArray =
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
            }
            return request.ColourArray;
        }

        private static byte[] GetVerticalSide(SideLedReadingRequest request)
        {
            var blockY = request.Y / request.SideLedCount;
            var sectionRequest = CreateSectionReadingRequest(request);
            GetStartAndEndSecondarySide(request.StartFromZero, request.Depth, request.X, out var start, out var end);
            sectionRequest.StartX = start;
            sectionRequest.EndX = end;
            if (request.IsIncremental)
            {
                for (var count = 0; count < request.SideLedCount - 1; count++)
                {
                    sectionRequest.StartY = count * blockY;
                    sectionRequest.EndY = sectionRequest.StartY + blockY;
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
                }
                sectionRequest.StartY = blockY * (request.SideLedCount - 1);
                sectionRequest.EndY = request.Y;
                request.ColourArray =
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount);
            }
            sectionRequest.StartY = blockY * (request.SideLedCount - 1);
            sectionRequest.EndY = request.Y;
            request.ColourArray =
                ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount);
            for (var count = request.SideLedCount - 2; count >= 0; count--)
            {
                sectionRequest.StartY = count * blockY;
                sectionRequest.EndY = sectionRequest.StartY + blockY;
                request.ColourArray =
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
            }
            return request.ColourArray;
        }

        public static byte[] GetSideLED(SideLedReadingRequest request)
        {
            return request.IsHorizontal ? GetHorizontalSide(request) : GetVerticalSide(request);
        }

        
        private static byte[] AddLedColourToArray(long[] colourArray, byte[] outputArray, int pixelCount, int currentLedCount)
        {
            var colourModel = new AverageColour
            {
                AverageB = GetAverageColour(colourArray[0], pixelCount),
                AverageG = GetAverageColour(colourArray[1], pixelCount),
                AverageR = GetAverageColour(colourArray[2], pixelCount)
            };
            return ArrayService.AdColourToByteArray(outputArray, colourModel, currentLedCount);
        }
    }
}
