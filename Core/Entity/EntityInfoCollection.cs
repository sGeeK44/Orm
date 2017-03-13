﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orm.Core.Interfaces;

// ReSharper disable ConvertPropertyToExpressionBody
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Orm.Core
{
    public class EntityInfoCollection : IEnumerable<IEntityInfo>
    {
        private readonly Dictionary<string, IEntityInfo> _entities = new Dictionary<string, IEntityInfo>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<Type, string> _typeToNameMap = new Dictionary<Type, string>();
        private readonly object _sycRoot = new object();

        public object SyncRoot
        {
            get { return _sycRoot; }
        }

        public void Add(IEntityInfo entityInfo)
        {
            var key = entityInfo.EntityName.ToLower();

            lock (_sycRoot)
            {
                // check for dupes
                if (!_entities.ContainsKey(key))
                {
                    _entities.Add(key, entityInfo);
                }
                
                if (!_typeToNameMap.ContainsKey(entityInfo.EntityType))
                {
                    _typeToNameMap.Add(entityInfo.EntityType, entityInfo.EntityName);
                }
            }
        }

        public void Remove(string entityName)
        {
            lock (_sycRoot)
            {
                if (_entities.ContainsKey(entityName))
                {
                    _entities.Remove(entityName);
                }
                foreach(var t in _typeToNameMap.ToArray())
                {
                    if(t.Value == entityName)
                    {
                        _typeToNameMap.Remove(t.Key);
                    }
                }
            }
        }

        public string GetNameForType(Type type)
        {
            lock (_sycRoot)
            {
                if (!_typeToNameMap.ContainsKey(type)) return null;

                return _typeToNameMap[type];
            }
        }

        public IEnumerator<IEntityInfo> GetEnumerator()
        {
            lock (_sycRoot)
            {
                return _entities.Values.GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (_sycRoot)
            {
                return _entities.Values.GetEnumerator();
            }
        }

        public IEntityInfo this[string entityName]
        {
            get 
            {
                lock (_sycRoot)
                {
                    if (!_entities.ContainsKey(entityName)) return null;

                    return _entities[entityName.ToLower()];
                }
            }
            internal set
            {
                lock (_sycRoot)
                {
                    _entities[entityName.ToLower()] = value;
                }
            }
        }

        public bool Contains(string entityName)
        {
            lock (_sycRoot)
            {
                return _entities.ContainsKey(entityName);
            }
        }

        public IEntityInfo[] ToArray()
        {
            lock (_sycRoot)
            {
                return _entities.Values.ToArray();
            }
        }
    }
}
