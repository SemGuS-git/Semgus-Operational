#define DO_SK


namespace Semgus.OrderSynthesis {
    internal static class Result {
        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
        public static Result<T> Err<T>(string msg) => Result<T>.Err(msg);
    }
    internal class Result<T> {
        private readonly T _value;
        private readonly bool _ok;

        public string Message { get; }

        private Result(T value) {
            _value = value;
            _ok = true;
            Message = "";
        }

        private Result(string message) {
            _ok = false;
            Message = message;
        }


        public static Result<T> Ok(T value) => new(value);
        public static Result<T> Err(string msg) => new(msg);


        public bool TryUnwrap(out T? value) {
            if (_ok) {
                value = _value;
                return true;
            } else {
                value = default;
                return false;
            }
        }
    }
}