using System.Diagnostics;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal record MuxInterval(MuxTuple Left, MuxTuple Right) {

        private bool _checkedSingle = false;
        private bool _isSingle = false;
        public bool IsSingle => _checkedSingle ? _isSingle : (_checkedSingle = true) && (_isSingle = Left.Values.SequenceEqual(Right.Values));

        public MuxInterval(MuxTuple value) : this(value,value) {
            _checkedSingle = true;
            _isSingle = true;
        }

        public MuxTupleType TupleType => Left.TupleType;

        public static MuxInterval Widest(MuxTupleType type) => new(type.Bot, type.Top);

        public static MuxInterval IntervalJoin(MuxInterval a, MuxInterval b) {
            Debug.Assert(a.TupleType == b.TupleType);

            return new MuxInterval(a.TupleType.Meet(a.Left, b.Left), a.TupleType.Join(a.Right, b.Right));
        }

        public bool DoesNotContain(MuxTuple value) {
            return TupleType.StrictCompare(value, Left) || TupleType.StrictCompare(Right, value);
        }

        public long DiscreteValueCount() {
            long n = 1;
            var ttype = TupleType;
            for (int i = 0; i < ttype.Size; i++) {
                if (n < 0) return int.MaxValue; // overflow
                if (ttype.ElementTypes[i] == typeof(bool)) {
                    if (Left[i] != Right[i]) n *= 2;
                    continue;
                }
                if (ttype.ElementTypes[i] == Operational.SmtIntsTheoryImpl.IntegerType) {
                    n *= (1 + Math.Abs(Right[i] - Left[i]));
                    continue;
                }
            }
            return n;
        }

        public override string ToString() => $"[{Left}, {Right}]";

        // Break this interval into a set of single-element intervals.
        internal MuxInterval[] Split() {
            var ttype = TupleType;
            dynamic[][] choices_per_element = new dynamic[ttype.Size][];

            long cartesian_product_size = 1;

            for (int i = 0; i < ttype.Size; i++) {
                if (ttype.ElementTypes[i] == typeof(bool)) {

                    if (Left[i] == Right[i]) {
                        choices_per_element[i] = (new dynamic[] { Left[i] });
                    } else {
                        choices_per_element[i] = (new dynamic[] { false, true });
                        cartesian_product_size *= 2;
                    }
                    continue;
                }
                if (ttype.ElementTypes[i] == Operational.SmtIntsTheoryImpl.IntegerType) {
                    long first, last;

                    if (Right[i] < Left[i]) {
                        first = Right[i];
                        last = Left[i];
                    } else {
                        first = Left[i];
                        last = Right[i];
                    }

                    var span_length = last - first + 1;

                    var split = new dynamic[span_length];
                    for (long j = 0; j < span_length; j++) split[j] = first + j;
                    choices_per_element[i] = split;

                    cartesian_product_size *= span_length;
                    continue;
                }
                throw new NotSupportedException();
            }

            MuxInterval[] results = new MuxInterval[cartesian_product_size];

            // If we have only a single primitive element, we can skip the cartesian product
            if (ttype.Size == 1) {
                for (int i = 0; i < cartesian_product_size; i++) {
                    results[i] = new(new MuxTuple(ttype, new[] { choices_per_element[0][i] }));
                }
            } else {
                int k = 0;
                foreach (var hot_array in Util.IterationUtil.CartesianProduct(choices_per_element)) {
                    var clone = new dynamic[ttype.Size];
                    hot_array.CopyTo(clone, 0);
                    results[k++] = new(new MuxTuple(ttype, clone));
                }
            }
            return results;
        }
    }



    //internal class Agglom {
    //    SemgusTermType TermType { get; }
    //    SynthComparisonFunction CompareIn { get; }
    //    SynthComparisonFunction CompareOut { get; }
    //}

    //internal class Interval {

    //}
    //internal class SemRelArgTupleType {

    //}

    //internal class SynthComparisonFunction {
    //    public bool Leq() {

    //    }
    //}
}
