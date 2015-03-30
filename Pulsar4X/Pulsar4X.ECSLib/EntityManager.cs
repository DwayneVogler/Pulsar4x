﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Helpers;

namespace Pulsar4X.ECSLib
{
    public class EntityManager
    {
        private List<int> _entities;
        private List<ComparableBitArray> _entityMasks;

        private Dictionary<Type, int> _dataBlobTypes;
        private List<List<BaseDataBlob>> _dataBlobMap;

        private static Dictionary<Guid, EntityManager> _globalGuidDictionary;
        private Dictionary<Guid, int> _localGuidDictionary; 

        private readonly object _lockObj;

        public EntityManager()
        {

            _entities = new List<int>();
            _entityMasks = new List<ComparableBitArray>();

            _dataBlobTypes = new Dictionary<Type, int>();
            _dataBlobMap = new List<List<BaseDataBlob>>();

            if (_globalGuidDictionary == null)
                _globalGuidDictionary = new Dictionary<Guid, EntityManager>();

            _localGuidDictionary = new Dictionary<Guid, int>();

            _lockObj = new object();

            // Use reflection to setup all our dataBlobMap.
            // Find all types that implement BaseDataBlob
            List<Type> dataBlobTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t =>
                    t != typeof(BaseDataBlob) &&
                    t.IsSubclassOf(typeof(BaseDataBlob))
                ).ToList();

            // Create a list in our dataBlobMap for each discovered type.
            int i = 0;
            foreach (Type dataBlobType in dataBlobTypes)
            {
                _dataBlobTypes.Add(dataBlobType, i);
                _dataBlobMap.Add(new List<BaseDataBlob>());
                i++;
            }

            Clear();
        }

        /// <summary>
        /// Verifies that the supplied entity is valid in this manager.
        /// </summary>
        /// <returns>True is the entity is considered valid.</returns>
        public bool IsValidEntity(int entity)
        {
            if (entity < 0 || entity >= _entities.Count)
            {
                return false;
            }
            return _entities[entity] == entity;
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than GetDataBlob(entity, typeIndex)
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid entity is passed.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public T GetDataBlob<T>(int entity) where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();
            return GetDataBlob<T>(entity, typeIndex);
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Fastest direct lookup available.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entity is passed.</exception>
        /// <exception cref="InvalidCastException">Thrown when typeIndex does not match m_dataBlobTypes entry for Type T</exception>
        public T GetDataBlob<T>(int entity, int typeIndex) where T : BaseDataBlob
        {
            return (T)_dataBlobMap[typeIndex][entity];
        }

        /// <summary>
        /// Sets the DataBlob for the specified entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlob is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid entity is passed.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public void SetDataBlob<T>(int entity, T dataBlob) where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();
            SetDataBlob(entity, dataBlob, typeIndex);
        }

        /// <summary>
        /// Sets the DataBlob for the specified entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlob is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entity is passed.</exception>
        private void SetDataBlob(int entity, BaseDataBlob dataBlob, int typeIndex)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException("dataBlob", "Do not use SetDataBlob to remove a datablob. Use RemoveDataBlob.");
            }

            dataBlob.Entity = entity;
            _dataBlobMap[typeIndex][entity] = dataBlob;
            _entityMasks[entity][typeIndex] = true;
        }

        /// <summary>
        /// Removes the DataBlob from the specified entity.
        /// Slower than RemoveDataBlob(entity, typeIndex).
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid entity is passed.</exception>
        public void RemoveDataBlob<T>(int entity) where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();
            RemoveDataBlob(entity, typeIndex);
        }

        /// <summary>
        /// Removes the DataBlob from the specified entity.
        /// Fastest DataBlob removal available.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entity is passed.</exception>
        public void RemoveDataBlob(int entity, int typeIndex)
        {
            if (!IsValidEntity(entity))
            {
                throw new ArgumentException("Invalid Entity.");
            }

            _dataBlobMap[typeIndex][entity] = null;
            _entityMasks[entity][typeIndex] = false;
        }

        /// <summary>
        /// Returns a list of all DataBlobs with type T.
        /// <para></para>
        /// Returns a blank list if no DataBlobs of type T found.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public List<T> GetAllDataBlobsOfType<T>() where T: BaseDataBlob
        {
            var dataBlobs = new List<T>();
            foreach (BaseDataBlob dataBlob in _dataBlobMap[GetDataBlobTypeIndex<T>()])
            {
                if (dataBlob != null)
                {
                    dataBlobs.Add((T)dataBlob);
                }
            }

            return dataBlobs;
        }

        /// <summary>
        /// Returns a list of all DataBlobs for a given entity.
        /// <para></para>
        /// Returns a blank list if entity has no DataBlobs.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed an invalid entity.</exception>
        public List<BaseDataBlob> GetAllDataBlobsOfEntity(int entity)
        {
            if (!IsValidEntity(entity))
            {
                throw new ArgumentException("Invalid Entity.");
            }

            var entityDBs = new List<BaseDataBlob>();
            ComparableBitArray entityMask = _entityMasks[entity];

            for (int typeIndex = 0; typeIndex < _dataBlobTypes.Count; typeIndex++)
            {
                if (entityMask[typeIndex])
                {
                    entityDBs.Add(GetDataBlob<BaseDataBlob>(entity, typeIndex));
                }
            }

            return entityDBs;
        }

        /// <summary>
        /// Returns a list of entity id's for entities that have datablob type T.
        /// <para></para>
        /// Returns a blank list if no DataBlobs of type T exist.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public List<int> GetAllEntitiesWithDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();

            ComparableBitArray dataBlobMask = BlankDataBlobMask();
            dataBlobMask[typeIndex] = true;

            return GetAllEntitiesWithDataBlobs(dataBlobMask);
        }

        /// <summary>
        /// Returns a list of entity id's for entities that contain all dataBlobs defined by
        /// the dataBlobMask.
        /// <para></para>
        /// Returns a blank list if no entities have all needed DataBlobs
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed a malformed (incorrect length) dataBlobMask.</exception>
        /// <exception cref="NullReferenceException">Thrown when dataBlobMask is null.</exception>
        public List<int> GetAllEntitiesWithDataBlobs(ComparableBitArray dataBlobMask)
        {
            if (dataBlobMask.Length != _dataBlobTypes.Count)
            {
                throw new ArgumentException("dataBlobMask must contain a bit value for each dataBlobType.");
            }

            var entities = new List<int>();

            for (int entity = 0; entity < _entityMasks.Count; entity++)
            {
                if ((_entityMasks[entity] & dataBlobMask) == dataBlobMask)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        /// <summary>
        /// Optimized convenience function to get entities that contain two types of DataBlobs, along with the associated DataBlobs.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Tuple<T1, T2>> GetEntitiesAndDataBlobs<T1, T2>() where T1 : BaseDataBlob where T2 : BaseDataBlob
        {
            int typeIndexT1 = GetDataBlobTypeIndex<T1>();
            int typeIndexT2 = GetDataBlobTypeIndex<T2>();

            ComparableBitArray dataBlobMask = BlankDataBlobMask();
            dataBlobMask[typeIndexT1] = true;
            dataBlobMask[typeIndexT2] = true;

            List<int> entities = GetAllEntitiesWithDataBlobs(dataBlobMask);

            var entitiesAndDataBlobs = new Dictionary<int, Tuple<T1, T2>>();

            foreach (int entity in entities)
            {
                T1 dataBlobT1 = (T1)_dataBlobMap[typeIndexT1][entity];
                T2 dataBlobT2 = (T2)_dataBlobMap[typeIndexT2][entity];

                var dataBlobs = new Tuple<T1, T2>(dataBlobT1, dataBlobT2);

                entitiesAndDataBlobs.Add(entity, dataBlobs);
            }

            return entitiesAndDataBlobs;
        }

        /// <summary>
        /// Returns the first entity found with the specified DataBlobType.
        /// <para></para>
        /// Returns -1 if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public int GetFirstEntityWithDataBlob<T>() where T : BaseDataBlob
        {
            return GetFirstEntityWithDataBlob(GetDataBlobTypeIndex<T>());
        }

        /// <summary>
        /// Returns the first entity found with the specified DataBlobType.
        /// <para></para>
        /// Returns -1 if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when passed an invalid typeIndex</exception>
        public int GetFirstEntityWithDataBlob(int typeIndex)
        {
            List<BaseDataBlob> dataBlobType = _dataBlobMap[typeIndex];
            for (int i = 0; i < _entities.Count; i++)
            {
                if (dataBlobType[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns a blank DataBlob mask with the correct number of entries.
        /// </summary>
        public ComparableBitArray BlankDataBlobMask()
        {
            return new ComparableBitArray(_dataBlobTypes.Count);
        }

        /// <summary>
        /// Creates an entity with an entity slot.
        /// </summary>
        /// <returns>Entity ID of the new entity.</returns>
        public int CreateEntity()
        {
            return CreateEntity(Guid.NewGuid());
        }

        /// <summary>
        /// Adds an entity with the pre-existing datablobs to this EntityManager.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlobs is null.</exception>
        public int CreateEntity(List<BaseDataBlob> dataBlobs)
        {
            return CreateEntity(dataBlobs, Guid.NewGuid());
        }

        private int CreateEntity(List<BaseDataBlob> dataBlobs, Guid entityGuid)
        {
            if (dataBlobs == null)
            {
                throw new ArgumentNullException("dataBlobs", "dataBlobs cannot be null. To create a blank entity use CreateEntity().");
            }

            int entity = CreateEntity(entityGuid);

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                int typeIndex;
                TryGetDataBlobTypeIndex(dataBlob.GetType(), out typeIndex);
                SetDataBlob(entity, dataBlob, typeIndex);
            }

            return entity;
        }

        private int CreateEntity(Guid entityGuid)
        {
            int entityID;
            for (entityID = 0; entityID < _entities.Count; entityID++)
            {
                if (entityID != _entities[entityID])
                {
                    // Space open.
                    break;
                }
            }

            // Mark space claimed by making the index match the value.
            // Entities[7] == 7; on claimed spot.
            // Entities[7] == -1; on unclaimed spot.
            if (entityID == _entities.Count)
            {
                _entities.Add(entityID);

                _entityMasks.Add(new ComparableBitArray(_dataBlobTypes.Count));
                // Make sure the entityDBMaps have enough space for this entity.
                foreach (List<BaseDataBlob> entityDBMap in _dataBlobMap)
                {
                    entityDBMap.Add(null);
                }
            }
            else
            {
                _entities[entityID] = entityID;
                // Make sure the EntityDBMaps are null for this entity.
                // This should be done by RemoveEntity, but let's just be safe.
                for (int typeIndex = 0; typeIndex < _dataBlobTypes.Count; typeIndex++)
                {
                    _dataBlobMap[typeIndex][entityID] = null;
                }

                _entityMasks[entityID] = new ComparableBitArray(_dataBlobTypes.Count);
            }

            // Add the GUID to the lookup list.
            _globalGuidDictionary.Add(entityGuid, this);
            return entityID;
        }

        /// <summary>
        /// Removes this entity from this entity manager.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed an invalid entity.</exception>
        public void RemoveEntity(int entity)
        {
            // Make sure we only attempt to remove valid entities.
            if (!IsValidEntity(entity))
            {
                throw new ArgumentException("Invalid Entity.");
            }

            // Mark the entity as invalid.
            _entities[entity] = -1;

            // Remove the GUID from all lists.
            Guid entityGuid;
            if (!TryGetGuidByEntity(entity, out entityGuid))
            {
                throw new Exception("Failed to remove entity. Entity had no Guid.");
            }
            _globalGuidDictionary.Remove(entityGuid);
            _localGuidDictionary.Remove(entityGuid);


            foreach (List<BaseDataBlob> dataBlobType in _dataBlobMap)
            {
                dataBlobType[entity] = null;
            }

            _entityMasks[entity] = BlankDataBlobMask();
        }

        /// <summary>
        /// Transfers an entity to the specified manager.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed an invalid entity.</exception>
        /// <exception cref="Exception">Thrown when a entity is found with no Guid. (Should not be possible, possible data corruption)</exception>
        public void TransferEntity(int entity, EntityManager manager)
        {
            List<BaseDataBlob> dataBlobs = GetAllDataBlobsOfEntity(entity);

            Guid entityGuid;
            if (!TryGetGuidByEntity(entity, out entityGuid))
            {
                throw new Exception("Failed to transfer entity. Entity had no Guid.");
            }

            RemoveEntity(entity);
            manager.CreateEntity(dataBlobs, entityGuid);
        }

        /// <summary>
        /// Gets the associated EntityManager and entityID of the specified Guid.
        /// </summary>
        /// <returns>True if entity is found.</returns>
        /// <exception cref="Exception">Thrown when Guid is located in global dictionary, but not in the referenced manager. (Should not be possible, possible data corruption)</exception>
        public bool FindEntityByGuid(Guid entityGuid, out EntityManager manager, out int entityID)
        {
            manager = null;
            entityID = -1;

            if (!_globalGuidDictionary.TryGetValue(entityGuid, out manager))
            {
                return false;
            }

            if (!manager.TryGetEntityByGuid(entityGuid, out entityID))
            {
                throw new Exception("FindEntityByGuid Failed. Guid located in global dictionary, but not in specified manager.");
            }
            return true;
        }

        /// <summary>
        /// Gets the associated entityID of the specified Guid.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        /// <returns>True if entity exists in this manager.</returns>
        public bool TryGetEntityByGuid(Guid entityGuid, out int entityID)
        {
            return _localGuidDictionary.TryGetValue(entityGuid, out entityID);
        }

        /// <summary>
        /// Gets the associated Guid of the specified entityID.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        /// <returns>True if entity exists in this manager.</returns>
        public bool TryGetGuidByEntity(int entityID, out Guid entityGuid)
        {
            entityGuid = Guid.Empty;

            if (!IsValidEntity(entityID))
            {
                return false;
            }

            try
            {
                entityGuid = _localGuidDictionary.First(kvp => kvp.Value == entityID).Key;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Completely clears all entities.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        public void Clear()
        {
            for (int entityID = 0; entityID < _entities.Count; entityID++)
            {
                if (IsValidEntity(entityID))
                    RemoveEntity(entityID);
            }

        }

        /// <summary>
        /// Returns the true if the specified type is a valid DataBlobType.
        /// <para></para>
        /// typeIndex parameter is set to the typeIndex of the dataBlobType if found.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlobType is null.</exception>
        public bool TryGetDataBlobTypeIndex(Type dataBlobType, out int typeIndex)
        {
            return _dataBlobTypes.TryGetValue(dataBlobType, out typeIndex);
        }

        /// <summary>
        /// Faster than TryGetDataBlobTypeIndex and uses generics for type safety.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public int GetDataBlobTypeIndex<T>() where T : BaseDataBlob
        {
            return _dataBlobTypes[typeof(T)];
        }
    }
}