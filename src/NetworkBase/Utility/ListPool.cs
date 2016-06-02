using NetworkBase.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkBase.Utility
{
    public static class ListPool<T>
    {
        private static readonly ConcurrentStack<List<T>> m_ListPool = new ConcurrentStack<List<T>>();

        public static List<T> AcquireGameEventList()
        {
            List<T> list;
            if (m_ListPool.Count == 0)
            {
                for (int i = 0; i < 10; ++i)
                {
                    m_ListPool.Push(new List<T>());
                }
            }

            if (!m_ListPool.TryPop(out list))
            {
                list = new List<T>();
            }

            return list;
        }

        public static void ReleaseGameEventList(List<T> list)
        {
            list.Clear();
            m_ListPool.Push(list);
        }
    }
}
