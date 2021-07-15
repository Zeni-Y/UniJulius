using System;
using System.Collections.Generic;
using System.Threading;

namespace UniJulius.Runtime
{
    class UnitySynchronizationContext : SynchronizationContext
    {
        readonly Queue<Action> queue = new Queue<Action>();

        object syncRoot = new object();

        public static UnitySynchronizationContext Create()
        {
            var context = new UnitySynchronizationContext();
            SetSynchronizationContext(context);
            UnitySynchronizationContextRunner.Begin(context);
            return context;
        }

        UnitySynchronizationContext()
        {
            SetSynchronizationContext(this);
        }

        public void Update()
        {
            lock (syncRoot)
            {
                while (queue.Count > 0)
                {
                    var action = queue.Dequeue();
                    action();
                }
            }
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            lock (syncRoot)
            {
                queue.Enqueue(() => d(state));
            }
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            var completed = false;
            lock (syncRoot)
            {
                queue.Enqueue(() =>
                {
                    completed = true;
                    d(state);
                });
            }
            while (!completed) { };
        }

        public override SynchronizationContext CreateCopy()
        {
            return new UnitySynchronizationContext();
        }
    }
}
