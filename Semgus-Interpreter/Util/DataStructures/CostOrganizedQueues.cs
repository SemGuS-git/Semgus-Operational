using System.Collections.Generic;

namespace Semgus.Util {

    public class CostOrganizedQueues<T> {
        private readonly List<Queue<T>> _queues = new();

        public int MaxCost => _queues.Count;

        public Queue<T> Get(int cost) {
            int k = 1 + cost - _queues.Count;
            for (int i = 0; i < k; i++) _queues.Add(new());
            return _queues[cost];
        }

        public void EnqueueAt(int cost, T value) {
            Get(cost).Enqueue(value);
        }
    }
}
