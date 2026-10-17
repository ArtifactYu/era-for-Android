/* Copyright 2020 Mark Heath
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
 * IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Loopable WaveStream
    /// </summary>
    public class LoopStream : WaveStream
    {
        readonly WaveStream sourceStream;

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        public LoopStream(WaveStream source)
        {
            sourceStream = source;
        }

        /// <summary>
        /// The WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// Length in bytes of this stream (effectively infinite)
        /// </summary>
        public override long Length
        {
            get { return long.MaxValue / 32; }
        }

        /// <summary>
        /// Position within this stream in bytes
        /// </summary>
        public override long Position
        {
            get
            {
                return sourceStream.Position;
            }
            set
            {
                sourceStream.Position = value;
            }
        }

        /// <summary>
        /// Always has data available
        /// </summary>
        public override bool HasData(int count)
        {
            // infinite loop
            return true;
        }

        /// <summary>
        /// Read data from this stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int required = count - read;
                int readThisTime = sourceStream.Read(buffer, offset + read, required);
                if (readThisTime < required)
                {
                    sourceStream.Position = 0;
                }

                if (sourceStream.Position >= sourceStream.Length)
                {
                    sourceStream.Position = 0;
                }
                read += readThisTime;
            }
            return read;
        }

        /// <summary>
        /// Dispose this WaveStream (disposes the source)
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            sourceStream.Dispose();
            base.Dispose(disposing);
        }
    }
}