using GF.Common.Debug;
using GF.Core.Event;

namespace GF.Core.Entity
{
    public class BaseEntity
    {
        protected EventCenter m_EventCenter;

        private int m_InstanceID;
        private string m_Name;
        private bool m_Enable;

        public virtual void OnInitialize(string name, object initializeData)
        {
            m_InstanceID = AutoIncrementID.AutoID();
            m_Name = name;
            SetEnable(true);
        }

        public virtual void OnRelease()
        {

        }

        public int GetInstanceID()
        {
            return m_InstanceID;
        }

        public void SetEnable(bool enable)
        {
            m_Enable = enable;
        }

        public bool IsEnable()
        {
            return m_Enable;
        }

        public void AddBehaviour(BaseEntityBehaviour behaviour)
        {
            MDebug.Assert(behaviour.GetOwner() == null
                , "Entity"
                , "component.GetOwner() == null");

            behaviour.SetOwner(this);
            behaviour.SetEnable(true);
        }

        public void RemoveBehaivour(BaseEntityBehaviour behaviour)
        {
            MDebug.Assert(behaviour.GetOwner() == this
                , "Entity"
                , "component.GetOwner() == this");

            behaviour.SetOwner(null);
            behaviour.DestorySelf();
        }

        public EventCenter GetEventCenter()
        {
            return m_EventCenter;
        }
    }
}