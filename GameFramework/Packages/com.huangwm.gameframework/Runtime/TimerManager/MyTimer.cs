using System;
using System.Collections;
using System.Collections.Generic;
using GF.Common.Data;
using GF.Timer;
using UnityEngine;

namespace GF.Timer
{
    public class MyTimer : IObjectPoolItem
    {
        public int tag;
        public float tm;
        public float life;
        public int count;
        public Action<float> m_scheduler;
        public Action<float> m_frame;
        public void OnAlloc()
        {
            m_frame = null;
            m_scheduler = null;
        }

        public void OnRelease()
        {
            m_frame = null;
            m_scheduler = null;
        }
    }
}
