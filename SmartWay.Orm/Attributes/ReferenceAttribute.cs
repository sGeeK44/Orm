using System;

namespace SmartWay.Orm.Attributes
{
    public class ReferenceAttribute : EntityFieldAttribute, IEquatable<ReferenceAttribute>, IDistinctable
    {
        public ReferenceAttribute(Type referenceEntityType, string foreignReferenceField, string localReferenceField)
        {
            ReferenceEntityType = referenceEntityType;
            LocalReferenceField = localReferenceField;
            ForeignReferenceField = foreignReferenceField;
        }

        /// <summary>
        ///     The type of the referenced Entity
        /// </summary>
        public Type ReferenceEntityType { get; }

        /// <summary>
        ///     The name of the key Field in the referenced Entity (typically the Primary Key for ManyToOne relation)
        /// </summary>
        public string ForeignReferenceField { get; }

        /// <summary>
        ///     The name of the key Field in current Entity (typically the Foreign Key for ManyToOne relation)
        /// </summary>
        public string LocalReferenceField { get; }

        public string Key => $"{LocalReferenceField}{ReferenceEntityType.Name}{ForeignReferenceField}";

        public bool Equals(ReferenceAttribute other)
        {
            if (other == null
                || ReferenceEntityType != other.ReferenceEntityType)
                return false;

            return string.Compare(ForeignReferenceField, other.ForeignReferenceField,
                       StringComparison.InvariantCultureIgnoreCase) == 0;
        }
    }
}