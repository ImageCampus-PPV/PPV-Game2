using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Entities
{
    public class EntityRegistry : IService
    {
        public bool IsPersistance => false;

        private uint _currentEntityID = 0;

        private Dictionary<uint, Unit> _entities;
        private Dictionary<Type, List<uint>> _entityIdsPerType;

        public EntityRegistry()
        {
            _entities = new Dictionary<uint, Unit>();
            _entityIdsPerType = new Dictionary<Type, List<uint>>();
        }

        public void Init()
        {
            Unit[] entities = UnityEngine.Object.FindObjectsOfType<Unit>();

            Type currentEntityType = null;

            foreach (Unit entity in entities)
            {
                entity.SetID(++_currentEntityID);
                _entities.Add(entity.ID, entity);

                do
                {
                    currentEntityType = currentEntityType == null ? entity.GetType() : currentEntityType.BaseType;

                    if (!_entityIdsPerType.ContainsKey(currentEntityType))
                        _entityIdsPerType.Add(currentEntityType, new List<uint>());

                    _entityIdsPerType[currentEntityType].Add(entity.ID);

                } while (currentEntityType != typeof(Unit));

                currentEntityType = null;
            }
        }

        public EntityType GetAs<EntityType>(uint ID) where EntityType : Unit
        {
            if (ID == Unit.NULL_UNIT)
            {
                throw new NullReferenceException("Unit id 0 represents a null entity");
            }

            if (!_entities.ContainsKey(ID))
            {
                throw new KeyNotFoundException(ID.ToString());
            }

            if (_entities[ID] is not EntityType)
            {
                throw new InvalidCastException($"An attempt was made to obtain a type {_entities[ID].GetType().Name}"
                                             + $"entity as type {typeof(EntityType).Name} from the EntityRegistry");
            }

            return _entities[ID] as EntityType;
        }

        public IEnumerable<Enemy> Enemies => FilterEntities<Enemy>();
        public IEnumerable<Player> Players => FilterEntities<Player>();
        public IEnumerable<Unit> Units => FilterEntities<Unit>();

        public IEnumerable<EntityType> FilterEntities<EntityType>() where EntityType : Unit
        {
            if (_entityIdsPerType.ContainsKey(typeof(EntityType)))
            {
                foreach (uint ID in _entityIdsPerType[typeof(EntityType)])
                {
                    yield return _entities[ID] as EntityType;
                }
            }
        }
    }
}
