using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable ConvertPropertyToExpressionBody
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Orm.Core.Attributes
{
    public class FieldAttributeCollection : IEnumerable<FieldAttribute>, ICloneable
    {
        private readonly Dictionary<string, FieldAttribute> _fields = new Dictionary<string, FieldAttribute>(StringComparer.InvariantCultureIgnoreCase);
        private readonly object _syncRoot = new object();

        public FieldAttribute KeyField { get; private set; }

        internal FieldAttributeCollection()
        {
            KeyField = null;
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        public object Clone()
        {
            return new FieldAttributeCollection(_fields.ToDictionary(e=>e.Key, e=>(FieldAttribute)e.Value.Clone()).Values);
        }

        internal FieldAttributeCollection(IEnumerable<FieldAttribute> fields)
        {
            KeyField = null;

            AddRange(fields);
        }

        internal void AddRange(IEnumerable<FieldAttribute> fields)
        {
            lock (_syncRoot)
            {
                foreach (var f in fields)
                {
                    Add(f, true);
                }
            }
        }

        internal void Add(FieldAttribute attribute)
        {
            Add(attribute, false);
        }

        internal void Add(FieldAttribute attribute, bool replaceKeyField)
        {
            lock (_syncRoot)
            {
                if (attribute.IsPrimaryKey)
                {
                    if ((KeyField == null) || (replaceKeyField))
                    {
                        KeyField = attribute;
                    }
                    else
                    {
                        throw new MutiplePrimaryKeyException(KeyField.FieldName);
                    }
                }

                _fields.Add(attribute.FieldName, attribute);
            }
        }

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _fields.Count;
                }
            }
        }

        public FieldAttribute this[string fieldName]
        {
            get
            {
                lock (_syncRoot)
                {
                    return _fields[fieldName.ToLower()];
                }
            }
        }

        public FieldAttribute this[int index]
        {
            get
            {
                lock (_syncRoot)
                {
                    return _fields.ElementAt(index).Value;
                }
            }
        }

        public bool ContainsField(string fieldName)
        {
            lock (_syncRoot)
            {
                return _fields.ContainsKey(fieldName);
            }
        }

        public IEnumerator<FieldAttribute> GetEnumerator()
        {
            lock (_syncRoot)
            {
                return _fields.Values.GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }
    }
}
