using System.Diagnostics;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal record MuxInterval(MuxTuple Left, MuxTuple Right) {

        public bool IsSingle { get; } = false;

        public MuxInterval(MuxTuple value) : this(value, value) {
            IsSingle = true;
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
