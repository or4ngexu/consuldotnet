﻿using System;
using System.Threading;



namespace Consul
{
    public interface IConsulClient : IDisposable
    {
        IACLEndpoint ACL { get; }
        IDisposableLock AcquireLock(LockOptions opts);
        IDisposableLock AcquireLock(LockOptions opts, CancellationToken ct);
        IDisposableLock AcquireLock(string key);
        IDisposableLock AcquireLock(string key, CancellationToken ct);
        IDisposableSemaphore AcquireSemaphore(SemaphoreOptions opts);
        IDisposableSemaphore AcquireSemaphore(string prefix, int limit);
        IAgentEndpoint Agent { get; }
        ICatalogEndpoint Catalog { get; }
        IDistributedLock CreateLock(LockOptions opts);
        IDistributedLock CreateLock(string key);
        IEventEndpoint Event { get; }
        [Obsolete("This method is not compatible with DNXCore and is slated for removal in 0.7.0+. Please file a github issue if you use it so we can explore alternatives.", false)]
        void ExecuteAbortableLocked(LockOptions opts, Action action);
        [Obsolete("This method is not compatible with DNXCore and is slated for removal in 0.7.0+. Please file a github issue if you use it so we can explore alternatives.", false)]
        void ExecuteAbortableLocked(LockOptions opts, CancellationToken ct, Action action);
        [Obsolete("This method is not compatible with DNXCore and is slated for removal in 0.7.0+. Please file a github issue if you use it so we can explore alternatives.", false)]
        void ExecuteAbortableLocked(string key, Action action);
        [Obsolete("This method is not compatible with DNXCore and is slated for removal in 0.7.0+. Please file a github issue if you use it so we can explore alternatives.", false)]
        void ExecuteAbortableLocked(string key, CancellationToken ct, Action action);
        void ExecuteInSemaphore(SemaphoreOptions opts, Action a);
        void ExecuteInSemaphore(string prefix, int limit, Action a);
        void ExecuteLocked(LockOptions opts, Action action);
        void ExecuteLocked(LockOptions opts, CancellationToken ct, Action action);
        void ExecuteLocked(string key, Action action);
        void ExecuteLocked(string key, CancellationToken ct, Action action);
        IHealthEndpoint Health { get; }
        IKVEndpoint KV { get; }
        IRawEndpoint Raw { get; }
        IDistributedSemaphore Semaphore(SemaphoreOptions opts);
        IDistributedSemaphore Semaphore(string prefix, int limit);
        ISessionEndpoint Session { get; }
        IStatusEndpoint Status { get; }
    }
}
