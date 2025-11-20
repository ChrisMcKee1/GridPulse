using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal static class JsonColumnHelpers
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    internal static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    internal static ICollection<T> DeserializeList<T>(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new List<T>()
            : JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? new List<T>();
    }

    internal static IDictionary<string, TValue> DeserializeDictionary<TValue>(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, TValue>()
            : JsonSerializer.Deserialize<Dictionary<string, TValue>>(json, SerializerOptions) ?? new Dictionary<string, TValue>();
    }

    internal static ValueComparer<ICollection<T>> CreateCollectionComparer<T>()
    {
        return new ValueComparer<ICollection<T>>(
            (left, right) => CollectionsEqual(left, right),
            collection => GetCollectionHashCode(collection),
            collection => collection.ToList());
    }

    internal static ValueComparer<IDictionary<string, TValue>> CreateDictionaryComparer<TValue>()
    {
        return new ValueComparer<IDictionary<string, TValue>>(
            (left, right) => DictionariesEqual(left, right),
            dictionary => GetDictionaryHashCode(dictionary),
            dictionary => new Dictionary<string, TValue>(dictionary, StringComparer.OrdinalIgnoreCase));
    }

    private static bool CollectionsEqual<T>(ICollection<T> left, ICollection<T> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        using var leftEnumerator = left.GetEnumerator();
        using var rightEnumerator = right.GetEnumerator();

        while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
        {
            if (!EqualityComparer<T>.Default.Equals(leftEnumerator.Current, rightEnumerator.Current))
            {
                return false;
            }
        }

        return true;
    }

    private static int GetCollectionHashCode<T>(IEnumerable<T> collection)
    {
        var hash = new HashCode();
        foreach (var item in collection)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    private static bool DictionariesEqual<TValue>(IDictionary<string, TValue> left, IDictionary<string, TValue> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value))
            {
                return false;
            }

            if (!EqualityComparer<TValue>.Default.Equals(value, pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static int GetDictionaryHashCode<TValue>(IDictionary<string, TValue> dictionary)
    {
        var hash = new HashCode();
        foreach (var pair in dictionary.OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(pair.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(pair.Value);
        }

        return hash.ToHashCode();
    }
}
