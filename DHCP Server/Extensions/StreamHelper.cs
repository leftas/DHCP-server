/*
Copyright (c) 2010 Jean-Paul Mikkers
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace DNS_Server.Classes
{
    public static class StreamHelper
    {
        public static IPAddress ReadIPAddress(this Stream s)
        {
            byte[] bytes = new byte[4];
            s.Read(bytes, 0, bytes.Length);
            return new IPAddress(bytes);
        }

        public static void WriteIPAddress(this Stream s, IPAddress v)
        {
            byte[] bytes = v.GetAddressBytes();
            s.Write(bytes, 0, bytes.Length);
        }
        public static ushort ReadUInt16(this Stream s)
        {
            var br = new BinaryReader(s);
            return (ushort)IPAddress.NetworkToHostOrder((short)br.ReadUInt16());
        }

        public static void WriteUInt16(this Stream s, ushort v)
        {
            var bw = new BinaryWriter(s);
            bw.Write((ushort)IPAddress.HostToNetworkOrder((short)v));
        }

        public static uint ReadUInt32(this Stream s)
        {
            var br = new BinaryReader(s);
            return (uint)IPAddress.NetworkToHostOrder((int)br.ReadUInt32());
        }

        public static void WriteUInt32(this Stream s, uint v)
        {
            var bw = new BinaryWriter(s);
            bw.Write((uint)IPAddress.HostToNetworkOrder((int)v));
        }

        public static string ReadZString(this Stream s)
        {
            var sb = new StringBuilder();
            int c = s.ReadByte();
            while (c > 0)
            {
                sb.Append((char)c);
                c = s.ReadByte();
            }
            return sb.ToString();
        }

        public static void WriteZString(this Stream s, string msg)
        {
            TextWriter tw = new StreamWriter(s, Encoding.ASCII);
            tw.Write(msg);
            tw.Flush();
            s.WriteByte(0);
        }

        public static void WriteZString(this Stream s, string msg, int length)
        {
            if (msg.Length >= length)
            {
                msg = msg.Substring(0, length - 1);
            }

            TextWriter tw = new StreamWriter(s, Encoding.ASCII);
            tw.Write(msg);
            tw.Flush();

            for (int t = msg.Length; t < length; t++)
            {
                s.WriteByte(0);
            }
        }

        public static string ReadString(this Stream s)
        {
            return ReadString(s, 16 * 1024);
        }

        public static void WriteString(this Stream s, string msg)
        {
            WriteString(s, false, msg);
        }

        public static void WriteString(this Stream s, bool zeroTerminated, string msg)
        {
            TextWriter tw = new StreamWriter(s, Encoding.ASCII);
            tw.Write(msg);
            tw.Flush();
            if (zeroTerminated)
            {
                s.WriteByte(0);
            }
        }

        public static string ReadString(this Stream s, int maxLength)
        {
            var sb = new StringBuilder();
            int c = s.ReadByte();
            while (c > 0 && sb.Length < maxLength)
            {
                sb.Append((char)c);
                c = s.ReadByte();
            }
            return sb.ToString();
        }

        public static void WriteToOtherStream(this Stream stream, Stream otherStream, int length)
        {
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            otherStream.Write(buffer, 0, length);
        }
    }
}
