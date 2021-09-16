using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace ZEVLR_LIB.Common
{
    public static class BitmapEx
    {
        public static byte[] ReadGreyPixelData(string filePath)
        {
            var bitmap = new Bitmap(filePath);
            byte[] bytes = new byte[bitmap.Width * bitmap.Height];
            {
                var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var pp = bmpData.Stride / bitmap.Width;
                var tempBuffer = ArrayPool<byte>.Shared.Rent(Math.Abs(bmpData.Stride) * bitmap.Height);
                var ptr = bmpData.Scan0;

                Marshal.Copy(ptr, tempBuffer, 0, tempBuffer.Length);
                bitmap.UnlockBits(bmpData);

                for (int i = 0; i * pp < tempBuffer.Length; i++)
                    bytes[i] = tempBuffer[i * pp];

                ArrayPool<byte>.Shared.Return(tempBuffer);
            }

            return bytes;
        }
    }
}
