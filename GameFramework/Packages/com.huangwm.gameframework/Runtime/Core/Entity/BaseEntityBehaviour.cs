using GF.Common.Debug;
using GF.Core.Behaviour;

namespace GF.Core.Entity
{
    public class BaseEntityBehaviour : BaseBehaviour
    {
        private BaseEntity m_Owner;

        public BaseEntityBehaviour(string name
                , int priority
                , string groupName)
            : base(name, priority, groupName)
        {

        }

        public override void OnRelease()
        {
            m_Owner = null;
        }

        public BaseEntity GetOwner()
        {
            return m_Owner;
        }

        internal void SetOwner(BaseEntity owner)
        {
            m_Owner = owner;
        }

        internal new bool CanUpdate()
        {
            return m_Owner.IsEnable()
                && base.CanUpdate();
        }
    }
}