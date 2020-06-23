using System;

namespace SmartWay.Orm
{
    public class StoreAlreadyExistsException : Exception
    {
        public StoreAlreadyExistsException()
            : base("Selected store already exists")
        {
        }
    }

    public class ReservedWordException : Exception
    {
        public ReservedWordException(string word)
            : base(
                $"'{word}' is a reserved word.  It cannot be used for an Entity or Field name. Rename the entity/field or adjust its attributes.")
        {
        }
    }

    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(Type type)
            : base($"Entity Type '{type.Name}' not found. Is your Store up to date?")
        {
        }

        public EntityNotFoundException(string entityName)
            : base($"Entity Type '{entityName}' not found. Is your Store up to date?")
        {
        }
    }

    public class PrimaryKeyRequiredException : Exception
    {
        public PrimaryKeyRequiredException(string message)
            : base(message)
        {
        }
    }

    public class RecordNotFoundException : Exception
    {
        public RecordNotFoundException(string message)
            : base(message)
        {
        }
    }

    public class DefinitionException : Exception
    {
        public DefinitionException(string message)
            : base(message)
        {
        }
    }

    public class EntityDefinitionException : Exception
    {
        public EntityDefinitionException(string message)
            : base(message)
        {
        }
    }
}