using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ExternalLCDIntegration.Services
{
    public static class ScreenService
    {

        public static Bitmap GetScreenBitmap(int screenWidth, int screenHeight, Size size)
        {
            var screenBitmap = new Bitmap(screenWidth, screenHeight);
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

    }
}
