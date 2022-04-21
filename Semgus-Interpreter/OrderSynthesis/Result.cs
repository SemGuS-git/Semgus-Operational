#define DO_SK


namespace Semgus.Util {
    public static class Result {
        public static Result<T, E> Ok<T, E>(T value) => new OkResult<T, E>(value);
        public static Result<T, None> Ok<T>(T value) => new OkResult<T>(value);

        public static Result<T, E> Err<T, E>(E err) => new ErrResult<T, E>(err);
        public static Result<T, Exception> Err<T>(string msg) => new ErrResult<T, Exception>(new(msg));
        public static Result<T, None> Err<T>() => new ErrResult<T>();

    }

    public static class ResultExtensions {
        public delegate T1 Selector<T0, T1>(T0 value);

        public static Result<T1, E> Select<T0, T1, E>(this Result<T0, E> head, Selector<T0, T1> select) => head switch {
            OkResult<T0, E> ok => new OkResult<T1, E>(select(ok.Value)),
            ErrResult<T0, E> err => new ErrResult<T1, E>(err.Error),
            _ => throw new NotSupportedException(),
        };

        public static Result<IEnumerable<T>, E> Collect<T, E>(this IEnumerable<Result<T, E>> series) {
            List<T> values = new();
            foreach (var result in series) {
                switch (result) {
                    case OkResult<T, E> ok:
                        values.Add(ok.Value);
                        continue;
                    case ErrResult<T, E> err:
                        return Result.Err<IEnumerable<T>, E>(err.Error);
                    default: throw new NotSupportedException();
                }
            }
            return Result.Ok<IEnumerable<T>, E>(values);
        }

        public static Result<T, IEnumerable<E>> FirstOne<T, E>(this IEnumerable<Result<T, E>> series) {
            List<E> errors = new();
            foreach (var result in series) {
                switch (result) {
                    case OkResult<T, E> ok:
                        return Result.Ok<T, IEnumerable<E>>(ok.Value);
                        continue;
                    case ErrResult<T, E> err:
                        errors.Add(err.Error);
                        continue;
                    default: throw new NotSupportedException();
                }
            }
            return Result.Err<T, IEnumerable<E>>(errors);
        }

        public static T? OrDefault<T, E>(this Result<T, E> result, Func<T> fallback) => result switch {
            OkResult<T, E> ok => ok.Value,
            ErrResult<T, E> err => fallback(),
            _ => throw new NotSupportedException(),
        };

        public static T? OrDefault<T, E>(this Result<T, E> result, Func<E, T> fallback) => result switch {
            OkResult<T, E> ok => ok.Value,
            ErrResult<T, E> err => fallback(err.Error),
            _ => throw new NotSupportedException(),
        };

        public static T? OrDefault<T, E>(this Result<T, E> result) => result.OrDefault(default(T));

        public static T? OrDefault<T, E>(this Result<T, E> result, T? fallback) => result switch {
            OkResult<T, E> ok => ok.Value,
            ErrResult<T, E> err => fallback,
            _ => throw new NotSupportedException(),
        };

        public static bool TryGetValue<T, E>(this Result<T, E> result, out T value) {
            switch (result) {
                case OkResult<T, E> ok:
                    value = ok.Value;
                    return true;
                case ErrResult<T, E> err:
                    value = default;
                    return false;
                default: throw new NotSupportedException();
            }
        }
    }

    public abstract class Result<T, E> {
        public abstract bool IsSuccess { get; }
        public abstract T Unwrap();
        public abstract E UnwrapError();
    }


    public struct None { }

    public abstract class Result<T> : Result<T, None> { }

    public class OkResult<T, E> : Result<T, E> {
        public override bool IsSuccess => true;
        public readonly T Value;

        public override T Unwrap() => Value;
        public override E UnwrapError() => throw new InvalidOperationException();

        public OkResult(T value) {
            this.Value = value;
        }
    }

    public class ErrResult<T, E> : Result<T, E> {
        public override bool IsSuccess => false;
        public readonly E Error;

        public override T Unwrap() => throw (Error is Exception e ? e : new Exception(Error?.ToString()));
        public override E UnwrapError() => Error;

        public ErrResult(E error) {
            this.Error = error;
        }
    }

    public class OkResult<T> : OkResult<T, None> {
        public OkResult(T value) : base(value) { }
    }
    public class ErrResult<T> : ErrResult<T, None> {
        public ErrResult() : base(default) { }
    }


    //internal class Result<T> {
    //    private readonly T _value;
    //    private readonly bool _ok;

    //    public string Message { get; }

    //    private Result(T value) {
    //        _value = value;
    //        _ok = true;
    //        Message = "";
    //    }

    //    private Result(string message) {
    //        _ok = false;
    //        Message = message;
    //    }


    //    public static Result<T> Ok(T value) => new(value);
    //    public static Result<T> Err(string msg) => new(msg);


    //    public bool TryUnwrap(out T? value) {
    //        if (_ok) {
    //            value = _value;
    //            return true;
    //        } else {
    //            value = default;
    //            return false;
    //        }
    //    }
    //}
}