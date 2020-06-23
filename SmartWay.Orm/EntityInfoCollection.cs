using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SmartWay.Orm.Interfaces;

// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace SmartWay.Orm
{
    public class EntityInfoCollection : IEnumerable<IEntityInfo>
    {
        private readonly Dictionary<string, IEntityInfo> _entities =
            new Dictionary<string, IEntityInfo>(StringComparer.InvariantCultureIgnoreCase);

        private readonly object _sycRoot = new object();
        private readonly Dictionary<Type, string> _typeToNameMap = new Dictionary<Type, string>();

        public object SyncRoot => _sycRoot;

        public IEntityInfo this[string entityName]
        {
            get
            {
                lock (_sycRoot)
                {
                    if (string.IsNullOrEmpty(entityName))
                        return null;

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

        public IEntityInfo this[Type entityType] => this[GetNameForType(entityType)];

        public IEnumerator<IEntityInfo> GetEnumerator()
        {
            lock (_sycRoot)
            {
                return _entities.Values.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_sycRoot)
            {
                return _entities.Values.GetEnumerator();
            }
        }

        public void Add(IEntityInfo entityInfo)
        {
            var entityName = entityInfo.GetNameInStore();
            var key = entityName.ToLower();

            lock (_sycRoot)
            {
                // check for dupes
                if (!_entities.ContainsKey(key)) _entities.Add(key, entityInfo);

                if (!_typeToNameMap.ContainsKey(entityInfo.EntityType))
                    _typeToNameMap.Add(entityInfo.EntityType, entityName);
            }
        }

        public void Remove(string entityName)
        {
            lock (_sycRoot)
            {
                if (_entities.ContainsKey(entityName)) _entities.Remove(entityName);
                foreach (var t in _typeToNameMap.ToArray())
                    if (t.Value == entityName)
                        _typeToNameMap.Remove(t.Key);
            }
        }

        public string GetNameForType(Type type)
        {
            lock (_sycRoot)
            {
                if (type == typeof(object))
                    return null;

                if (_typeToNameMap.ContainsKey(type))
                    return _typeToNameMap[type];

                return GetNameForType(type.BaseType);
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