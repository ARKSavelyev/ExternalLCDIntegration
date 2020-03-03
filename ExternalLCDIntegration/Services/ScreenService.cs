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

        public static string PrintRGB(ref string output,int avrB, int avrG, int avrR)
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
        
        public static byte[] GetSideLED(SideLedReadingRequest request)
        {
            AverageColour colours;
            if (request.isHorizontal)
            {
                var blockX = request.X / request.SideLedCount;
                var sectionRequest = new ScreenSectionReadingRequest
                {
                    ScreenPointer = request.ScreenPointer,
                    Stride =  request.Stride,
                    BPPModifier =  request.BPPModifier
                };
                if (request.StartFromZero)
                {
                    sectionRequest.StartY = 0;
                    sectionRequest.EndY = request.Y / request.Depth;
                }
                else
                {
                    sectionRequest.StartY =request.Y - request.Y / request.Depth;
                    sectionRequest.StartY = request.Y;
                }
                for (var count = 0; count < request.SideLedCount - 1; count++)
                {
                    sectionRequest.StartX = count * blockX;
                    sectionRequest.EndX = sectionRequest.StartX + blockX;
                    colours = GetSectionLED(sectionRequest);
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(request.ColourArray, colours, request.CurrentLedCount++);
                }
                sectionRequest.StartX = blockX * (request.SideLedCount - 1);
                sectionRequest.EndX = request.X;
                colours = GetSectionLED(sectionRequest);
                request.ColourArray =
                    ArrayService.AdColourToByteArray(request.ColourArray, colours, request.CurrentLedCount);
            }
            else
            {
                var blockY = request.Y / request.SideLedCount;
                var sectionRequest = new ScreenSectionReadingRequest
                {
                    ScreenPointer = request.ScreenPointer,
                    Stride = request.Stride,
                    BPPModifier = request.BPPModifier
                };
                if (request.StartFromZero)
                {
                    sectionRequest.StartX = 0;
                    sectionRequest.EndX = request.X / request.Depth;
                }
                else
                {
                    sectionRequest.StartX = request.X - request.X / request.Depth;
                    sectionRequest.EndX = request.X;
                }
                for (var count = 0; count < request.SideLedCount - 1; count++)
                {
                    sectionRequest.StartY = count * blockY;
                    sectionRequest.EndY = sectionRequest.StartY + blockY;
                    colours = GetSectionLED(sectionRequest);
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(request.ColourArray, colours, request.CurrentLedCount++);
                }
                sectionRequest.StartY = blockY * (request.SideLedCount - 1);
                sectionRequest.EndY = request.Y;
                colours = GetSectionLED(sectionRequest);
                request.ColourArray =
                    ArrayService.AdColourToByteArray(request.ColourArray, colours, request.CurrentLedCount);
            }
            return request.ColourArray;
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
