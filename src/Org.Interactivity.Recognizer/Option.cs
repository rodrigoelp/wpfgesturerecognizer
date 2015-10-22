using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Org.Interactivity.Recognizer
{
    /// <summary>
    /// Option type, represents encapsulation of an optional value (so we don't need to use a nasty nullable... I really wish c# had this type)
    /// Thanks to <see href="https://github.com/dtchepak">David Tchepak</see> for implementing the <see href="https://github.com/dtchepak/optiontype/blob/master/Option.cs">Option</see> type for me.
    /// </summary>
    /// <typeparam name="T">Instance or reference to the wrapped by the option type.</typeparam>
    internal struct Option<T> : IEnumerable<T>, IEquatable<Option<T>>
    {
        private readonly bool _hasValue;
        private readonly T _value;

        public static Option<T> Empty() { return new Option<T>(false, default(T)); }
        public static Option<T> Full(T value) { return new Option<T>(true, value); }

        private Option(bool hasValue, T value)
        {
            _hasValue = hasValue;
            _value = value;
        }

        public bool HasValue() { return _hasValue; }
        public bool IsEmpty() { return !_hasValue; }
        public T ValueOr(T other) { return Fold(x => x, other); }
        public T ValueOrDefault() { return ValueOr(default(T)); }

        public TResult Fold<TResult>(Func<T, TResult> ifValue, TResult elseValue)
        {
            return FoldLazy(ifValue, () => elseValue);
        }

        public TResult FoldLazy<TResult>(Func<T, TResult> ifValue, Func<TResult> elseValue)
        {
            return _hasValue ? ifValue(_value) : elseValue();
        }

        public Option<TResult> Select<TResult>(Func<T, TResult> f)
        {
            return Fold(x => Option.Full(f(x)), Option.Empty());
        }

        public Option<TResult> Map<TResult>(Func<T, TResult> f)
        {
            return Select(f);
        }

        public Option<T> Where(Func<T, bool> pred)
        {
            return _hasValue && pred(_value) ? this : Option.Empty();
        }

        public Option<TResult> SelectMany<TResult>(Func<T, Option<TResult>> f)
        {
            return SelectMany(f, (x, y) => y);
        }

        public Option<TResult> SelectMany<TK, TResult>(Func<T, Option<TK>> f, Func<T, TK, TResult> selector)
        {
            return Fold(val => f(val).Fold(next => Option.Full(selector(val, next)), Option.Empty()), Option.Empty());
        }

        public void Do(Action<T> ifValue) { DoElse(ifValue, () => { }); }
        public void DoElse(Action<T> ifValue, Action elseValue)
        {
            if (_hasValue) { ifValue(_value); }
            else { elseValue(); }
        }

        public Option<T> OrElse(Option<T> other)
        {
            return _hasValue ? this : other;
        }

        public static implicit operator Option<T>(Option option) { return Empty(); }

        public IEnumerator<T> GetEnumerator() { if (_hasValue) yield return _value; }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public bool Equals(Option<T> other)
        {
            return _hasValue.Equals(other._hasValue) && EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj is Option && !_hasValue) return true;
            return obj is Option<T> && Equals((Option<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_hasValue.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(_value);
            }
        }

        public override string ToString()
        {
            return IsEmpty() ? "Option.Empty" : _value.ToString();
        }

        public static bool operator ==(Option<T> left, Option<T> right) { return left.Equals(right); }
        public static bool operator !=(Option<T> left, Option<T> right) { return !left.Equals(right); }
    }

    internal class Option : IEquatable<Option>
    {
        private static readonly Option empty = new Option();
        private Option() { }
        public static Option<T> Full<T>(T value) { return Option<T>.Full(value); }
        public static Option Empty() { return empty; }

        public bool Equals(Option other) { return true; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is Option) return true;
            if (obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition() == typeof(Option<>))
            {
                return obj.Equals(this);
            }
            return false;
        }

        public override int GetHashCode() { return 1234; }
        public static bool operator ==(Option left, Option right) { return Equals(left, right); }
        public static bool operator !=(Option left, Option right) { return !Equals(left, right); }
    }

    internal static class OptionExtensions
    {
        public static Option<T> ToOption<T>(this T instance)
        {
            return ReferenceEquals(null, instance) ? Option.Empty() : Option.Full(instance);
        }

        public static Option<T> FirstOrEmpty<T>(this IEnumerable<T> items)
        {
            return FirstOrEmpty(items, x => true);
        }

        public static Option<T> FirstOrEmpty<T>(this IEnumerable<T> items, Func<T, bool> pred)
        {
            var filtered = items.Where(pred);
            return filtered.Any() ? Option.Full(filtered.First()) : Option.Empty();
        }

        public static Option<T> Flatten<T>(this Option<Option<T>> option)
        {
            return option.SelectMany(x => x);
        }

        public static Option<T> ToOption<T>(this T? instance) where T : struct
        {
            return instance?.ToOption() ?? Option.Empty();
        }
    }
}