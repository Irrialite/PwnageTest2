using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkBase.Events
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class EventBase<T> where T : struct
    {
        [JsonProperty]
        private T m_ID;
        [JsonProperty]
        private string m_Data;

#if !UNITY_5
        private object m_Target;
#endif

        public T ID
        {
            get
            {
                return m_ID;
            }
        }

#if !UNITY_5
        public X GetTarget<X>() where X : class => m_Target as X;
#endif

        public Y GetData<Y>()
        {
            if (string.IsNullOrEmpty(m_Data))
            {
                return default(Y);
            }
            try
            {
                return JsonConvert.DeserializeObject<Y>(m_Data);
            }
            catch (Exception)
            {
                return default(Y);
            }
        }

        protected EventBase()
        {
        }

        public EventBase(T id, object data
#if !UNITY_5
            , object target = null
#endif
            )
        {
            m_ID = id;
            if (data != null)
            {
                m_Data = JsonConvert.SerializeObject(data);
            }
            else
            {
                m_Data = null;
            }

#if !UNITY_5
            m_Target = target;
#endif
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

#if !UNITY_5
        /// <summary>
        /// This will replace 'Target' if 'Target' is null.
        /// </summary>
        /// <param name="context"></param>
        public void SetCustomContext(object context)
        {
            if (m_Target == null)
            {
                m_Target = context;
            }
        }
#endif
    }
}
