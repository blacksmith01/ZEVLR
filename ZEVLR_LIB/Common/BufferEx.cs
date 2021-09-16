using System;
using System.Collections.Generic;
using System.Text;

namespace ZEVLR_LIB.Common
{
    public class ResizableBuffer
    {
        public byte[] Bytes { get; private set; }
        public Int64 Length { get; private set; }

        public ResizableBuffer(Int64 size) { Set(size); }

        public byte[] Set(Int64 size)
        {
            if (Bytes == null || Bytes.LongLength < size)
            {
                Bytes = new byte[size];
            }

            Length = size;
            return Bytes;
        }
    }
}
