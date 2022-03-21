//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Semgus.Interpreter {
//    public class BoolMat {
//        private readonly int _n;
//        private readonly bool[] _array;

//        private BoolMat(int n) {
//            _n = n;
//            _array = new bool[((n) * (n + 1)) / 2];
//        }

//        public static BoolMat Zero(string s) => new BoolMat(s.Length + 1);

//        public static BoolMat Identity(string s) {
//            var m = new BoolMat(s.Length + 1);
//            for (int i = 0; i < m._n; i++) m[i, i] = true;
//            return m;
//        }

//        public static BoolMat Any(string s) {
//            var m = new BoolMat(s.Length + 1);
//            for (int i = 0; i < m._n-1; i++) {
//                m[i, i + 1] = true;
//            }
//            return m;
//        }

//        public static BoolMat Char(string s, string cs) {
//            if (cs.Length != 1) throw new ArgumentException();
//            var c = cs[0];
//            var m = new BoolMat(s.Length + 1);
//            for (int i = 0; i < m._n - 1; i++) {
//                m[i, i + 1] = s[i] == c;
//            }
//            return m;
//        }

//        public static BoolMat Add(BoolMat m0, BoolMat m1) {
//            var n = m0._n;
//            if (n != m1._n) throw new ArgumentException();
//            var m2 = new BoolMat(n);
//            for (int ii = 0; ii < m0._array.Length; ii++) {
//                m2[ii] = m0[ii] || m1[ii];
//            }
//            return m2;
//        }

//        public static BoolMat MatMul(BoolMat m0, BoolMat m1) {
//            var n = m0._n;
//            if (n != m1._n) throw new ArgumentException();
//            var m2 = new BoolMat(n);

//            for (int i = 0; i < n; i++) {
//                for (int j = i; j < n; j++) {
//                    var u = false;
//                    for(int k = i; k <= j; k++) {
//                        u |= m0[i, k] && m1[k, j];
//                    }
//                    m2[i, j] = u;
//                }
//            }
//            return m2;
//        }

//        public bool this[int ii] {
//            get => _array[ii];
//            private set => _array[ii] = value;
//        }

//        public bool this[int i, int j] {
//            get => _array[i * (_n - 1) - (i * (i - 1)) / 2 + j];
//            private set => _array[i * (_n - 1) - (i * (i - 1)) / 2 + j] = value;
//        }

//        public override string ToString() {
//            var sb = new StringBuilder();
//            sb.Append("[");
//            for(int i = 0; i < _n;i++) {
//                sb.Append("[ ");
//                for (int j = 0; j < _n; j++) {
//                    if (i <= j) {
//                        sb.Append(this[i, j] ? "1 " : "0 ");
//                    } else {
//                        sb.Append("  ");
//                    }
//                }
//                sb.AppendLine("]");
//            }
//            sb.Append("]");
//            return sb.ToString();
//        }
//    }
//}