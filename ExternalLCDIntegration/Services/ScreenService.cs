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

        public static void PrintRGB(ref string output,int avrB, int avrG, int avrR)
        {
            output = $"R: {avrR.ToString()} G: {avrG.ToString()} B: {avrB.ToString()}";
        }


        
        public static byte[] GetSideLEDUpdated(SideLedReadingRequest request)
        {
            return null;
        }

        public static byte[] GetSideLEDs(SideLedReadingRequest requestModel)
        {
            var totals = new long[] { 0, 0, 0 };
            var primaryDimensionBlock = requestModel.X / requestModel.SideLedCount;
            var secondaryDimensionBlock = requestModel.Y / 5;
            var startPixel = 0;
            var EndPixel = secondaryDimensionBlock;
            if (!requestModel.StartFromZero)
            {
                startPixel = requestModel.Y - secondaryDimensionBlock;
                EndPixel = requestModel.Y;
            }
                
            unsafe
            {
                var p = (byte*)(void*)requestModel.ScreenPointer;
                var pixelCount = 0;
                for (var primaryCount = 0; primaryCount < requestModel.SideLedCount - 1; primaryCount++)
                {
                    for (var secondaryCount = startPixel; secondaryCount < EndPixel; secondaryCount++)
                    {
                        var startX = primaryDimensionBlock * primaryCount;
                        var endX = startX + primaryDimensionBlock;
                        for (var blockX = startX; blockX < endX; blockX++)
                        {
                            for (var color = 0; color < 3; color++)
                            {
                                var idx = secondaryCount * requestModel.Stride + blockX * requestModel.BPPModifier + color;
                                totals[color] += p[idx];
                            }
                            pixelCount++;
                        }
                    }
                    requestModel.ColourArray = AddLedColourToArray(totals, requestModel.ColourArray, pixelCount, requestModel.CurrentLedCount++);
                    totals.CleanArray();
                }
                pixelCount = 0;
                for (var secondaryCount = startPixel; secondaryCount < EndPixel; secondaryCount++)
                {
                    var startX = primaryDimensionBlock * requestModel.SideLedCount - 1;
                    for (var blockX = startX; blockX < requestModel.X; blockX++)
                    {
                        for (var color = 0; color < 3; color++)
                        {
                            var idx = secondaryCount * requestModel.Stride + blockX * requestModel.BPPModifier + color;
                            totals[color] += p[idx];
                        }

                        pixelCount++;
                    }
                }
                requestModel.ColourArray = AddLedColourToArray(totals, requestModel.ColourArray, pixelCount, requestModel.CurrentLedCount++);
                return requestModel.ColourArray;
            }
        }

        private static byte[] AddLedColourToArray(long[] colourArray, byte[] outputArray, int pixelCount, int currentLedCount)
        {
            var avgB = ScreenService.GetAverageColour(colourArray[0], pixelCount);
            var avgG = ScreenService.GetAverageColour(colourArray[1], pixelCount);
            var avgR = ScreenService.GetAverageColour(colourArray[2], pixelCount);
            return ArrayService.AddToByteArray(outputArray, avgR, avgG, avgB, currentLedCount);
        }
    }
}
