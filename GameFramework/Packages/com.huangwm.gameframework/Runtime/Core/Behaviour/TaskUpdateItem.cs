using GF.Common.Data;
using System.Threading;

namespace GF.Core.Behaviour
{
    internal class TaskUpdateItem : IObjectPoolItem
    {
        public BaseBehaviour Behaviour;
        public object Input;
        public object Output;
        public ManualResetEvent ManualResetEvent;
        public float DeltaTime; 

        public TaskUpdateItem SetData(BaseBehaviour behaviour, float deltaTime)
        {
            Behaviour = behaviour;
            ManualResetEvent.Reset();
            DeltaTime = deltaTime;
            return this;
        }

        public void OnAlloc()
        {
            ManualResetEvent = new ManualResetEvent(false);
        }

        public void OnRelease()
        {
            Behaviour = null;
            Input = null;
            Output = null;
        }
    }
}