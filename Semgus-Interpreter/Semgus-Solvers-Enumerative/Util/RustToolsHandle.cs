#if USE_RUST
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Semgus.Solvers.Util {
    public class RustToolsHandle : SafeHandle {

        [DllImport("semgus_solvers_rust")]
        private static extern RustToolsHandle persistent_data_new([MarshalAs(UnmanagedType.LPUTF8Str)] string str);
        [DllImport("semgus_solvers_rust")]
        private unsafe static extern void persistent_data_free(IntPtr ptr);
        [DllImport("semgus_solvers_rust")]
        private static extern RustStringHandle preduce(RustToolsHandle data, [MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        public RustToolsHandle() : base(IntPtr.Zero, true) { Interlocked.Increment(ref InUseCount); }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public static int InUseCount = 0;

        protected override bool ReleaseHandle() {
            if (!this.IsInvalid) {
                Interlocked.Decrement(ref InUseCount);
                persistent_data_free(handle);
            }
            return true;
        }

        public string DoReduce(string s) {
            using var h = preduce(this, s);
            return h.AsString();
        }

        public static RustToolsHandle Create(string rules) => persistent_data_new(rules);
    }
}
#endif