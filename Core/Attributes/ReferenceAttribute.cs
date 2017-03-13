using System;
using System.Reflection;
using Orm.Core.Interfaces;

// ReSharper disable MergeConditionalExpression

namespace Orm.Core.Attributes
{
    public class ReferenceAttribute : EntityFieldAttribute, IEquatable<ReferenceAttribute>
    {
        /// <summary>
        /// The type of the referenced Entity
        /// </summary>
        public Type ReferenceEntityType { get; set; }
        /// <summary>
        /// The name of the key Field in the referenced Entity (typically the Primary Key)
        /// </summary>
        public string ForeignReferenceField { get; set; }
        public string LocalReferenceField { get; set; }
        public bool Autofill { get; set; }
        public PropertyInfo PropertyInfo { get; internal set; }
        public bool CascadeDelete { get; set; }

        private ReferenceType _type;

        public ReferenceType ReferenceType 
        {
            get { return _type; }
            set
            {
                if (value == ReferenceType.ManyToMany)
                {
                    throw new NotImplementedException();
                }

                _type = value;
            }
        }

        /// <summary>
        /// The type of the Joining entity for many-to-many relationships
        /// </summary>
        public Type JoinEntitytype { get; set; }

        /// <summary>
        /// Get Entity linked
        /// </summary>
        internal IEntityInfo Entity { get; set; }

        /// <summary>
        /// Get Entity reference
        /// </summary>
        internal IEntityInfo EntityReference { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="referenceEntityType">The type of the referenced Entity (the other Entity, not this one)</param>
        /// <param name="foreignReferenceField">The name of the key Field in the referenced Entity (typically the Primary Key)</param>
        public ReferenceAttribute(Type referenceEntityType, string foreignReferenceField)
        {
            ReferenceEntityType = referenceEntityType;
            LocalReferenceField = ForeignReferenceField = foreignReferenceField;
            Autofill = false;
            ReferenceType = ReferenceType.OneToMany;
        }

        public ReferenceAttribute(Type referenceEntityType, string foreignReferenceField, string localReferenceField)
        {
            ReferenceEntityType = referenceEntityType;
            LocalReferenceField = localReferenceField;
            ForeignReferenceField = foreignReferenceField;
            Autofill = false;
            ReferenceType = ReferenceType.OneToMany;
        }

        public bool Equals(ReferenceAttribute other)
        {
            if (other == null
             || ReferenceEntityType != other.ReferenceEntityType)
                return false;

            return string.Compare(ForeignReferenceField, other.ForeignReferenceField, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public void SetValue(object item, object refenceValue)
        {
            PropertyInfo.SetValue(item, refenceValue, null);
        }

        public IEntitySerializer GetReferenceSerializer()
        {
            return EntityReference == null
                 ? null
                 : EntityReference.GetSerializer();
        }
    }
}
