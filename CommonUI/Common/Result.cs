namespace CommonUI.Common
{
    /// <summary>
    /// Result pattern dla jawnego przekazywania sukcesu/błędu zamiast null
    /// </summary>
    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public string Error { get; }

        private Result(bool isSuccess, T? value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value, string.Empty);
        public static Result<T> Failure(string error) => new(false, default, error);

        /// <summary>
        /// Wykonuje akcję jeśli sukces
        /// </summary>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess && Value is not null)
                action(Value);
            return this;
        }

        /// <summary>
        /// Wykonuje akcję jeśli błąd
        /// </summary>
        public Result<T> OnFailure(Action<string> action)
        {
            if (IsFailure)
                action(Error);
            return this;
        }

        /// <summary>
        /// Mapuje wartość na inny typ
        /// </summary>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            return IsSuccess && Value is not null
                ? Result<TNew>.Success(mapper(Value))
                : Result<TNew>.Failure(Error);
        }

        /// <summary>
        /// Zwraca wartość lub domyślną jeśli błąd
        /// </summary>
        public T? GetValueOrDefault(T? defaultValue = default)
        {
            return IsSuccess ? Value : defaultValue;
        }

        /// <summary>
        /// Implicit conversion do bool dla łatwego sprawdzania if (result)
        /// </summary>
        public static implicit operator bool(Result<T> result) => result.IsSuccess;
    }

    /// <summary>
    /// Result bez wartości - tylko sukces/błąd
    /// </summary>
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, string.Empty);
        public static Result Failure(string error) => new(false, error);

        public Result OnSuccess(Action action)
        {
            if (IsSuccess)
                action();
            return this;
        }

        public Result OnFailure(Action<string> action)
        {
            if (IsFailure)
                action(Error);
            return this;
        }

        public static implicit operator bool(Result result) => result.IsSuccess;
    }

    /// <summary>
    /// Predefiniowane błędy dla spójności
    /// </summary>
    public static class ResultErrors
    {
        public const string NotFound = "Nie znaleziono";
        public const string InvalidInput = "Nieprawidłowe dane wejściowe";
        public const string EmptyValue = "Wartość nie może być pusta";
        public const string OperationCancelled = "Operacja została anulowana";
        public const string UnknownError = "Wystąpił nieznany błąd";

        public static string NotFoundFor(string entity) => $"{entity} nie został znaleziony";
        public static string InvalidFor(string field) => $"Nieprawidłowa wartość pola: {field}";
    }
}
