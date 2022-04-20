namespace Semgus.Util { 
    public class StreamAccumulator<T> {
        private readonly IEnumerator<T> src;

        private readonly List<T> cache = new();

        public StreamAccumulator(IEnumerator<T> src) {
            this.src = src;
        }

        public bool TryGet(int index, out T value) {
            if (Ok(index)) {
                value = cache[index];
                return true;
            } else {
                value = default;
                return false;
            }
        }

        public bool Ok(int index) {
            if (index < 0) return false;

            for (int t = cache.Count; t < index; t++) {
                if (!src.MoveNext()) return false;
                cache.Add(src.Current);
            }

            return true;
        }

        public T this[int index] => Ok(index) ? cache[index] : throw new IndexOutOfRangeException();
    }
}
