using NetworkBase.Events;
using NetworkBase.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkBase.Extensions
{
    public static class TCPExtensions
    {
        public const int c_IntSize = sizeof(int);

        public static async Task<int> SendEventAsync<T>(this Socket socket, EventBase<T> e) where T : struct
        {
            if (e == null)
            {
                return await Task.FromResult(0);
            }

            var bytes = Encoding.UTF8.GetBytes(e.Serialize());
            var sendBytes = new byte[bytes.Length + c_IntSize];

            sendBytes.WriteInt(bytes.Length, 0);
            Array.Copy(bytes, 0, sendBytes, c_IntSize, bytes.Length);
            return await socket.SendAsync(new ArraySegment<byte>(sendBytes), SocketFlags.None);
        }

        /// <summary>
        /// Returns bytes read from buffer.
        /// </summary>
        public static int ParseGameEvents(this byte[] buf, out GameEvent[] ges)
        {
            return buf.ParseGameEvents(buf.Length, out ges);
        }

        /// <summary>
        /// Returns bytes read from buffer. Count = number of bytes to read from buffer.
        /// </summary>
        public static int ParseGameEvents(this byte[] buf, int count, out GameEvent[] ges)
        {
            int length;
            int offset = 0;

            var list = ListPool<GameEvent>.AcquireGameEventList();
            if (count > 0 && count <= buf.Length)
            {
                while (offset + c_IntSize <= count
                    && (length = buf.ReadInt(offset)) > 0 && length + offset + c_IntSize <= count)
                {
                    list.Add(GameEvent.Deserialize(Encoding.UTF8.GetString(buf, offset + c_IntSize, length)));
                    offset += length + c_IntSize;
                }
            }
            ges = list.ToArray();
            ListPool<GameEvent>.ReleaseGameEventList(list);

            return offset;
        }

        public static void WriteInt(this byte[] buf, int value, int offset)
        {
            if (buf.Length - offset < c_IntSize)
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < c_IntSize; ++i)
            {
                buf[offset + i] = (byte)(value >> (i * 8));
            }
        }

        public static int ReadInt(this byte[] buf, int offset)
        {
            if (buf.Length - offset < c_IntSize)
            {
                throw new ArgumentException();
            }

            int val = 0;
            for (int i = 0; i < c_IntSize; ++i)
            {
                val |= buf[i + offset] << (i * 8);
            }
            return val;
        }
    }
}
