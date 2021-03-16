using GF.Common.Collection;

namespace GF.Core.Behaviour
{
    internal class BehaviourRBTree : RBTree<BaseBehaviour>
    {
        public BehaviourRBTree()
            : base(TreeAccessMethod.KEY_SEARCH_AND_INDEX)
        {
        }

        protected override int CompareNode(BaseBehaviour record1, BaseBehaviour record2)
        {
            return record1.GetPriority() - record2.GetPriority();
        }

        protected override int CompareSateliteTreeNode(BaseBehaviour record1, BaseBehaviour record2)
        {
            return record1.GetInstanceID() - record2.GetInstanceID();
        }
    }
}