using GF.Common.Debug;
using System.Collections.Generic;

namespace GF.Core.Entity
{
    public class EntityManager
    {
        private Dictionary<int, BaseEntity> m_InstanceIDToEntities;

        public EntityManager(KernelInitializeData initializeData)
        {
            m_InstanceIDToEntities = new Dictionary<int, BaseEntity>();
        }

        public T CreateEntity<T>(string name, object initializeData = null)
            where T : BaseEntity, new()
        {
            T entity = new T();

            entity.OnInitialize(name, initializeData);

            m_InstanceIDToEntities.Add(entity.GetInstanceID(), entity);
            return entity;
        }

        public void DestroyEntity(BaseEntity entity)
        {
            MDebug.Assert(m_InstanceIDToEntities[entity.GetInstanceID()] == entity
                , "Entity"
                , "m_InstanceIDToEntities[entity.GetInstanceID()] == entity");
            m_InstanceIDToEntities.Remove(entity.GetInstanceID());

            entity.OnRelease();
        }

        public bool TryGetEntity(int instanceID, out BaseEntity entity)
        {
            return m_InstanceIDToEntities.TryGetValue(instanceID, out entity);
        }
    }
}