using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ExternalLCDIntegration.Extensions;
using ExternalLCDIntegration.Models;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ExternalLCDIntegration.Services
{
    public static class ScreenService
    {
        public static Bitmap CreateBitmap(int screenWidth, int screenHeight)
        {
            return new Bitmap(screenWidth, screenHeight);
        }

        public static Bitmap CopyFromTheScreen(Bitmap screenBitmap, Size size)
        {
            using var g = Graphics.FromImage(screenBitmap);
            g.CopyFromScreen(Point.Empty, Point.Empty, size);
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

        private static AverageColour GetSectionLED(ScreenSectionReadingRequest request)
        {
            var totals = new long[] { 0, 0, 0 };
            var heightLimit = request.EndY * request.Stride;
            var widthLimit = request.EndX * request.BPPModifier;
            var pixelCount = 0;
            unsafe
            {
                var p = (byte*) (void*)request.ScreenPointer;
                for (var y = request.StartY * request.Stride; y < heightLimit; y+=request.Stride)
                {
                    for (var x = request.StartX * request.BPPModifier; x < widthLimit; x+=request.BPPModifier)
                    {
                        for (var color = 0; color < 3; color++)
                        {
                            var idx = y + x + color;
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

        /// <summary>
        /// Create a base request model for reading screen section from a general base Led Request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static ScreenSectionReadingRequest CreateSectionReadingRequest(BaseLedRequest request)
        {
            return new ScreenSectionReadingRequest
            {
                ScreenPointer = request.ScreenPointer,
                Stride = request.Stride,
                BPPModifier = request.BPPModifier
            };
        }

        /// <summary>
        /// Get average led colour, for Horizontal sides of the screen.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
            }
            else
            {
                sectionRequest.StartX = blockX * request.SideLedCount - 1;
                sectionRequest.EndX = request.X;
                request.ColourArray =
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
                for (var count = request.SideLedCount - 2; count >= 0; count--)
                {
                    sectionRequest.StartX = count * blockX;
                    sectionRequest.EndX = sectionRequest.StartX + blockX;
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
                }
            }
            return request.ColourArray;
        }

        /// <summary>
        /// Get average led colour, for vertical sides of the screen.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
            }
            else
            {
                sectionRequest.StartY = blockY * (request.SideLedCount - 1);
                sectionRequest.EndY = request.Y;
                request.ColourArray =
                    ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
                for (var count = request.SideLedCount - 2; count >= 0; count--)
                {
                    sectionRequest.StartY = count * blockY;
                    sectionRequest.EndY = sectionRequest.StartY + blockY;
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(request.ColourArray, GetSectionLED(sectionRequest), request.CurrentLedCount++);
                }
            }

            return request.ColourArray;
        }

        /// <summary>
        /// Get average led colour, for Horizontal sides of the screen, asynchronous.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static byte[] GetHorizontalSideAsync(SideLedReadingRequest request)
        {
            var blockX = request.X / request.SideLedCount;
            var sectionRequest = CreateSectionReadingRequest(request);
            GetStartAndEndSecondarySide(request.StartFromZero, request.Depth, request.Y, out var start, out var end);
            sectionRequest.StartY = start;
            sectionRequest.EndY = end;
            var samplingArray = new byte[request.SideLedCount * 3];
            var ledCount = 0;
            if (request.IsIncremental)
            {
                for (var count = 0; count < request.SideLedCount - 1; count++)
                {
                    sectionRequest.StartX = count * blockX;
                    sectionRequest.EndX = sectionRequest.StartX + blockX;
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(samplingArray, GetSectionLED(sectionRequest), ledCount++);
                }
                sectionRequest.StartX = blockX * (request.SideLedCount - 1);
                sectionRequest.EndX = request.X;
                request.ColourArray =
                    ArrayService.AdColourToByteArray(samplingArray, GetSectionLED(sectionRequest), ledCount);
            }
            else
            {
                sectionRequest.StartX = blockX * request.SideLedCount - 1;
                sectionRequest.EndX = request.X;
                request.ColourArray =
                    ArrayService.AdColourToByteArray(samplingArray, GetSectionLED(sectionRequest), ledCount++);
                for (var count = request.SideLedCount - 2; count >= 0; count--)
                {
                    sectionRequest.StartX = count * blockX;
                    sectionRequest.EndX = sectionRequest.StartX + blockX;
                    request.ColourArray =
                        ArrayService.AdColourToByteArray(samplingArray, GetSectionLED(sectionRequest), ledCount++);
                }
            }
            return samplingArray;
        }

        private static ScreenSectionReadingRequest CreateSectionReadingRequest(IntPtr screenPointer, int stride, int bppModifier,int startX, int endX, int startY, int endY)
        {
            return new ScreenSectionReadingRequest
            {
                ScreenPointer = screenPointer,
                Stride = stride,
                BPPModifier = bppModifier,
                StartX = startX,
                EndX = endX,
                StartY = startY,
                EndY = endY
            };
        }

        /// <summary>
        /// Get average led colour, for Vertical sides of the screen, asynchronous.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static byte[] GetVerticalSideAsync(SideLedReadingRequest request)
        {
            var blockY = request.Y / request.SideLedCount;
            GetStartAndEndSecondarySide(request.StartFromZero, request.Depth, request.X, out var startX, out var endX);
            var ledCount = 0;
            var readings = ArrayService.CreateTaskAverageColourArray(request.SideLedCount);
            if (request.IsIncremental)
            {
                var startY = 0;
                var endY = 0;
                for (var count = 0; count < request.SideLedCount - 1; count++)
                {
                    startY = count * blockY;
                    endY = startY + blockY;
                    readings[ledCount++] = GetSectionReading(CreateSectionReadingRequest(request.ScreenPointer, request.Stride,request.BPPModifier, startX, endX, startY, endY));
                }
                startY = blockY * (request.SideLedCount - 1);
                endY = request.Y;
                readings[ledCount] = GetSectionReading(CreateSectionReadingRequest(request.ScreenPointer, request.Stride, request.BPPModifier, startX, endX, startY, endY));
            }
            else
            {
                var startY = blockY * (request.SideLedCount - 1);
                var endY = request.Y;
                readings[ledCount++] = GetSectionReading(CreateSectionReadingRequest(request.ScreenPointer, request.Stride, request.BPPModifier, startX, endX, startY, endY));
                for (var count = request.SideLedCount - 2; count >= 0; count--)
                {
                    startY = count * blockY;
                    endY = startY + blockY;
                    readings[ledCount++] = GetSectionReading(CreateSectionReadingRequest(request.ScreenPointer, request.Stride, request.BPPModifier, startX, endX, startY, endY));
                }
            }
            var averageArray = ArrayService.AwaitTaskAverageColourArray(readings);
            var samplingArray = new byte[request.SideLedCount * 3];
            for (var readingCount = 0; readingCount < averageArray.Length; readingCount++)
            {
                samplingArray = ArrayService.AdColourToByteArray(samplingArray, averageArray[readingCount], readingCount);
            }
            return samplingArray;
        }

        private static Task<AverageColour> GetSectionReading(ScreenSectionReadingRequest sectionRequest)
        {
            return Task.Run(() => GetSectionLED(sectionRequest));
        }

        public static byte[] GetSideLED(SideLedReadingRequest request)
        {
            return request.IsHorizontal ? GetHorizontalSide(request) : GetVerticalSide(request);
        }

        public static Task<byte[]> GetSideLEDAsync(SideLedReadingRequest request)
        {
            return Task.Run(() => request.IsHorizontal ? GetHorizontalSideAsync(request) : GetVerticalSideAsync(request));
        }
    }
}
