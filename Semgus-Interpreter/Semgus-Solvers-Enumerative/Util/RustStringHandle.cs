#if USE_RUST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Semgus.Solvers.Util {

    // http://jakegoulding.com/rust-ffi-omnibus/string_return/
    internal class RustStringHandle : SafeHandle {
        [DllImport("semgus_solvers_rust")]
        private unsafe static extern void free_rust_string(IntPtr str);

        public RustStringHandle() : base(IntPtr.Zero, true) { Interlocked.Increment(ref InUseCount); }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public static int InUseCount = 0;

        public string AsString() {
            int len = 0;
            while (Marshal.ReadByte(handle, len) != 0) { ++len; }
            byte[] buffer = new byte[len];
            Marshal.Copy(handle, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
        protected override bool ReleaseHandle() {
            if (!this.IsInvalid) {
                Interlocked.Decrement(ref InUseCount);
                free_rust_string(handle);
            }
            return true;
        }
    }
}
#endif