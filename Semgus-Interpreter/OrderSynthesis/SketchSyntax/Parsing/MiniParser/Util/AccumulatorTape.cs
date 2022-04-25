using System.Collections;

namespace Semgus.Util {
    public interface ITape<T> : IEnumerable<T> {
        Result<T, int> this[int index] { get; }

        bool InBounds(int index);
    }

    public class AccumulatorTape<T> : ITape<T> {
        private readonly IEnumerator<T> src;

        private readonly List<T> cache = new();

        public AccumulatorTape(IEnumerable<T> src) {
            this.src = src.GetEnumerator();
        }

        public AccumulatorTape(IEnumerator<T> src) {
            this.src = src;
        }

        public bool InBounds(int index) {
            if (index < 0) return false;

            for (int t = cache.Count; t <= index; t++) {
                if (!src.MoveNext()) return false;
                cache.Add(src.Current);
            }

            return true;
        }

        public IEnumerator<T> GetEnumerator() => new TapeEnumerator<T>(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public Result<T, int> this[int index] => InBounds(index) ? Result.Ok<T,int>(cache[index]) : Result.Err<T, int>(cache.Count);
    }

    public class TapeEnumerator<T> : IEnumerator<T> {
        public int Cursor { get; set; } = -1;
        public ITape<T> Tape { get; }

        public TapeEnumerator(ITape<T> tape) {
            this.Tape = tape;
        }

        public Result<T, int> Peek(int offset = 0) => Tape[Cursor + offset];

        public T Current => Tape[Cursor].Unwrap();

        object? IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext() => Tape.InBounds(++Cursor);

        public void Reset() => throw new NotSupportedException();
    }
}
