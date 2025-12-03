using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Simplified IconGen for .NET 6/8+ using top-level statements style or standard class if needed.
// Note: System.Drawing.Common is required. The project references it via UseWindowsForms=true implicitly or we need to add it.
// RamCleaner.csproj has UseWindowsForms=true so System.Drawing should be available.

namespace RamCleaner
{
    public class IconGen
    {
        public static void Generate()
        {
            using (Bitmap bmp = new Bitmap(64, 64))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    
                    // Draw a cool simple logo: A circle with 'O'
                    // Gradient brush
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Point(0,0), new Point(64,64), Color.DeepSkyBlue, Color.DodgerBlue))
                    {
                        g.FillEllipse(brush, 2, 2, 60, 60);
                    }
                    
                    // White ring
                    using (var pen = new Pen(Color.White, 4))
                    {
                        g.DrawEllipse(pen, 10, 10, 44, 44);
                    }

                    // "M" for Memory or "O" for Opti
                    using (var font = new Font("Arial", 24, FontStyle.Bold))
                    {
                         var size = g.MeasureString("O", font);
                         g.DrawString("O", font, Brushes.White, (64 - size.Width)/2, (64 - size.Height)/2 + 2);
                    }
                }
                
                string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                bmp.Save(pngPath, ImageFormat.Png);
                
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OptiRam.ico");
                using (FileStream fs = new FileStream(icoPath, FileMode.Create))
                {
                    // ICO Header
                    fs.Write(new byte[] { 0, 0, 1, 0, 1, 0 }, 0, 6);
                    
                    // Entry
                    long pngSize = new FileInfo(pngPath).Length;
                    fs.WriteByte(64); fs.WriteByte(64); fs.WriteByte(0); fs.WriteByte(0);
                    fs.Write(new byte[] { 1, 0, 32, 0 }, 0, 4);
                    fs.Write(BitConverter.GetBytes((int)pngSize), 0, 4);
                    fs.Write(BitConverter.GetBytes(22), 0, 4);
                    
                    // PNG Data
                    byte[] pngData = File.ReadAllBytes(pngPath);
                    fs.Write(pngData, 0, pngData.Length);
                }
            }
        }
    }
}