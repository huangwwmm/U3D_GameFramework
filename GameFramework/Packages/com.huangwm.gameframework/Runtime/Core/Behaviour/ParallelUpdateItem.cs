using GF.Common.Data;

namespace GF.Core.Behaviour
{
    internal class ParallelUpdateItem : IObjectPoolItem
    {
        public BaseBehaviour Behaviour;
        public object Input;
        public object Output;
        public float DeltaTime;

        public ParallelUpdateItem SetData(BaseBehaviour behaviour, float deltaTime)
        {
            Behaviour = behaviour;
            DeltaTime = deltaTime;
            return this;
        }

        public void OnAlloc()
        {
        }

        public void OnRelease()
        {
            Behaviour = null;
            Input = null;
            Output = null;
        }
    }
}