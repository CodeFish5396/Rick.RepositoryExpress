using BarcodeLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Rick.RepositoryExpress.Utils
{
    public static class BarCodeHelper
    {
        public static byte[] GetBarCode(string Code, TYPE type = TYPE.CODE128, int Length = 1000, int Height = 200, int FontSize = 40)
        {
            try
            {
                using (Barcode barcode = new Barcode())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        barcode.IncludeLabel = true;
                        barcode.Alignment = AlignmentPositions.CENTER;
                        barcode.LabelFont = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, FontSize, System.Drawing.FontStyle.Regular);
                        var barcodeImage = barcode.Encode(type, Code, System.Drawing.Color.Black, System.Drawing.Color.White, Length, Height);
                        barcodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        ms.Position = 0;
                        using (BinaryReader reader = new BinaryReader(ms))
                        {
                            byte[] bytes = (byte[])reader.ReadBytes((int)ms.Length).Clone();
                            reader.Dispose();
                            ms.Dispose();
                            return bytes;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SaveBarCode(string Code, string path, TYPE type = TYPE.CODE128, int Length = 1000, int Height = 200, int FontSize = 40)
        {
            try
            {
                using (Barcode barcode = new Barcode())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        barcode.IncludeLabel = true;
                        barcode.Alignment = AlignmentPositions.CENTER;
                        barcode.LabelFont = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, FontSize, System.Drawing.FontStyle.Regular);
                        var barcodeImage = barcode.Encode(type, Code, System.Drawing.Color.Black, System.Drawing.Color.White, Length, Height);
                        barcodeImage.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
