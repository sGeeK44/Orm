using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Orm.Core.Interfaces;

namespace Orm.Core.Replication
{
    public sealed class Replicator
    {
        private IDataStore m_destination;
        private IDataStore m_source;
        private int m_period;
        private int m_batchSize;
        private AutoResetEvent m_dataAvailable;
        private bool m_run = false;
        private object m_syncRoot = new object();

        public const int MinReplicationPeriod = 100;
        public const int DefaultReplicationPeriod = 5000;
        public const int DefaultBatchSize = 50;

        public bool Running { get; private set; }
        public ReplicationBehavior Behavior { get; private set; }
        public bool CreateIdentityFieldInReplicatedTable { get; private set; }
        public Registrations Registrations { get; private set; }

        public Dictionary<Type, int> m_typeCounts;
        public Dictionary<string, int> m_nameCounts;

        public event EventHandler DataReplicated;

        public Replicator(IDataStore destination, ReplicationBehavior behavior)
            : this(destination, behavior, false)
        {
        }

        public Replicator(IDataStore destination, ReplicationBehavior behavior, bool addIdentityToDestination)
        {
            if (behavior != ReplicationBehavior.ReplicateAndDelete)
            {
                throw new NotSupportedException("Only ReplicateAndDelete is currently supported");
            }

            if (destination == null)
            {
                throw new ArgumentNullException();
            }

            Registrations = new Registrations();

            CreateIdentityFieldInReplicatedTable = addIdentityToDestination;

            Behavior = behavior;
            m_destination = destination;

            ReplicationPeriod = DefaultReplicationPeriod;
            MaxReplicationBatchSize = DefaultBatchSize;

            m_dataAvailable = new AutoResetEvent(false);

            m_typeCounts = new Dictionary<Type, int>();
            m_nameCounts = new Dictionary<string, int>();
        }

        public IDataStore Destination
        {
            get { return m_destination; }
        }

        public void ResetCounts()
        {
            lock (m_typeCounts)
            {
                foreach (var t in m_typeCounts)
                {
                    m_typeCounts[t.Key] = 0;
                }
            }
            lock (m_nameCounts)
            {
                foreach (var n in m_nameCounts)
                {
                    m_nameCounts[n.Key] = 0;
                }
            }
        }

        public int GetCount<T>()
        {
            return GetCount(typeof(T));
        }

        public int GetCount(Type entityType)
        {
            lock (m_typeCounts)
            {
                if (!m_typeCounts.ContainsKey(entityType)) return 0;
                return m_typeCounts[entityType];
            }
        }

        public int GetCount(string entityName)
        {
            lock (m_nameCounts)
            {
                if (!m_nameCounts.ContainsKey(entityName)) return 0;
                return m_nameCounts[entityName];
            }
        }

        public int ReplicationPeriod
        {
            get { return m_period; }
            set
            {
                if (value < MinReplicationPeriod) throw new ArgumentOutOfRangeException();

                m_period = value;
            }
        }

        /// <summary>
        /// The maximum number of Entity instances (e.g. data rows) to send during any given ReplicationPeriod
        /// </summary>
        /// <remarks>A MaxReplicationBatchSize of 0 mean "send all data"</remarks>
        public int MaxReplicationBatchSize
        {
            get { return m_batchSize; }
            set
            {
                if (value < 0) value = 0;

                m_batchSize = value;
            }
        }

        internal void SetSource(IDataStore source)
        {
            m_source = source;

            m_source.AfterInsert += m_source_AfterInsert;
        }

        void m_source_AfterInsert(object sender, EntityInsertArgs e)
        {
            // if we have an insert on a replicated entity, don't wait for the full period, let the replication proc know immediately

            // TODO: add preemption?

            lock (Registrations)
            {
                if (Registrations.Contains(e.EntityName))
                {
                    m_dataAvailable.Set();
                }
                else if (Registrations.Contains(e.Item.GetType()))
                {
                    m_dataAvailable.Set();
                }
            }
        }

        public void Stop()
        {
            m_run = false;
        }

        public void RegisterEntity<T>(ReplicationPriority priority)
        {
            RegisterEntity(typeof(T), priority);
        }

        public void RegisterEntity<T>()
        {
            RegisterEntity(typeof(T), ReplicationPriority.Normal);
        }

        public void RegisterEntity(Type entityType)
        {
            RegisterEntity(entityType, ReplicationPriority.Normal);
        }

        public void RegisterEntity(Type entityType, ReplicationPriority priority)
        {
            lock (Registrations)
            {
                Registrations.AddType(entityType, priority);

                lock (m_typeCounts)
                {
                    if (!m_typeCounts.ContainsKey(entityType))
                    {
                        m_typeCounts.Add(entityType, 0);
                    }
                }

                // TODO: look for failure and cache if it does (e.g. not connected scenarios)
                m_destination.AddType(entityType);
            }
        }

        public void RegisterEntity(string entityName)
        {
            RegisterEntity(entityName, ReplicationPriority.Normal);
        }

        public void RegisterEntity(string entityName, ReplicationPriority priority)
        {
            RegisterEntity(entityName, null, priority);
        }

        public void RegisterEntity(string entityName, string replicatedName, ReplicationPriority priority)
        {
            lock (Registrations)
            {
                Registrations.AddName(entityName, replicatedName, priority);

                lock (m_nameCounts)
                {
                    if (!m_nameCounts.ContainsKey(entityName))
                    {
                        m_nameCounts.Add(entityName, 0);
                    }
                }
            }
        }

        private void OnReplicationError(Exception ex)
        {
            // TODO: handle this or pass it upstream
            Debug.WriteLine("Replication Error: " + ex.Message);
        }

        private void RaiseDataReplicated()
        {
            var handler = DataReplicated;
            if (handler == null) return;

            handler(this, EventArgs.Empty);
        }
    }
}
