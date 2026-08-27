using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BedrockInventoryEditor.Core.Nbt;

public abstract class NbtTag
{
    public string Name { get; set; } = string.Empty;
    public abstract NbtTagType TagType { get; }
    public abstract NbtTag Clone();

    public override string ToString() => $"{TagType}('{Name}')";
}

public class NbtByte : NbtTag
{
    public override NbtTagType TagType => NbtTagType.Byte;
    public byte Value { get; set; }

    public NbtByte() { }
    public NbtByte(byte value) => Value = value;
    public NbtByte(string name, byte value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtByte(Name, Value);
    public override string ToString() => $"{TagType}('{Name}'): {Value}b";
}

public class NbtShort : NbtTag
{
    public override NbtTagType TagType => NbtTagType.Short;
    public short Value { get; set; }

    public NbtShort() { }
    public NbtShort(short value) => Value = value;
    public NbtShort(string name, short value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtShort(Name, Value);
    public override string ToString() => $"{TagType}('{Name}'): {Value}s";
}

public class NbtInt : NbtTag
{
    public override NbtTagType TagType => NbtTagType.Int;
    public int Value { get; set; }

    public NbtInt() { }
    public NbtInt(int value) => Value = value;
    public NbtInt(string name, int value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtInt(Name, Value);
    public override string ToString() => $"{TagType}('{Name}'): {Value}";
}

public class NbtLong : NbtTag
{
    public override NbtTagType TagType => NbtTagType.Long;
    public long Value { get; set; }

    public NbtLong() { }
    public NbtLong(long value) => Value = value;
    public NbtLong(string name, long value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtLong(Name, Value);
    public override string ToString() => $"{TagType}('{Name}'): {Value}L";
}

public class NbtFloat : NbtTag
{
    public override NbtTagType TagType => NbtTagType.Float;
    public float Value { get; set; }

    public NbtFloat() { }
    public NbtFloat(float value) => Value = value;
    public NbtFloat(string name, float value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtFloat(Name, Value);
    public override string ToString() => $"{TagType}('{Name}'): {Value}f";
}

public class NbtDouble : NbtTag
{
    public override NbtTagType TagType => NbtTagType.Double;
    public double Value { get; set; }

    public NbtDouble() { }
    public NbtDouble(double value) => Value = value;
    public NbtDouble(string name, double value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtDouble(Name, Value);
    public override string ToString() => $"{TagType}('{Name}'): {Value}d";
}

public class NbtByteArray : NbtTag
{
    public override NbtTagType TagType => NbtTagType.ByteArray;
    public byte[] Value { get; set; } = [];

    public NbtByteArray() { }
    public NbtByteArray(byte[] value) => Value = value;
    public NbtByteArray(string name, byte[] value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtByteArray(Name, (byte[])Value.Clone());
    public override string ToString() => $"{TagType}('{Name}'): [{Value.Length} bytes]";
}

public class NbtString : NbtTag
{
    public override NbtTagType TagType => NbtTagType.String;
    public string Value { get; set; } = string.Empty;

    public NbtString() { }
    public NbtString(string value) => Value = value;
    public NbtString(string name, string value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtString(Name, Value);
    public override string ToString() => $"{TagType}('{Name}'): \"{Value}\"";
}

public class NbtList : NbtTag, IEnumerable<NbtTag>
{
    public override NbtTagType TagType => NbtTagType.List;
    public NbtTagType ListType { get; set; } = NbtTagType.End;
    public List<NbtTag> Value { get; } = [];

    public NbtList() { }
    public NbtList(NbtTagType listType) => ListType = listType;
    public NbtList(string name, NbtTagType listType) { Name = name; ListType = listType; }

    public int Count => Value.Count;

    public void Add(NbtTag tag)
    {
        if (ListType == NbtTagType.End && Value.Count == 0)
        {
            ListType = tag.TagType;
        }
        Value.Add(tag);
    }

    public void Clear() => Value.Clear();

    public NbtTag this[int index]
    {
        get => Value[index];
        set => Value[index] = value;
    }

    public IEnumerator<NbtTag> GetEnumerator() => Value.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();

    public override NbtTag Clone()
    {
        var clone = new NbtList(Name, ListType);
        foreach (var item in Value)
        {
            clone.Add(item.Clone());
        }
        return clone;
    }

    public override string ToString() => $"{TagType}('{Name}') [{Value.Count} items of {ListType}]";
}

public class NbtCompound : NbtTag, IEnumerable<NbtTag>
{
    public override NbtTagType TagType => NbtTagType.Compound;
    private readonly Dictionary<string, NbtTag> _tags = new(System.StringComparer.Ordinal);

    public NbtCompound() { }
    public NbtCompound(string name) { Name = name; }

    public int Count => _tags.Count;
    public IEnumerable<string> Keys => _tags.Keys;
    public IEnumerable<NbtTag> Values => _tags.Values;

    public bool ContainsKey(string key) => _tags.ContainsKey(key);

    public NbtTag? Get(string key) => _tags.TryGetValue(key, out var tag) ? tag : null;

    public T? Get<T>(string key) where T : NbtTag => Get(key) as T;

    public byte GetByte(string key, byte defaultValue = 0) => (byte)GetNumeric(key, defaultValue);
    public short GetShort(string key, short defaultValue = 0) => (short)GetNumeric(key, defaultValue);
    public int GetInt(string key, int defaultValue = 0) => GetNumeric(key, defaultValue);
    public long GetLong(string key, long defaultValue = 0)
    {
        var tag = Get(key);
        return tag switch
        {
            NbtLong l => l.Value,
            NbtInt i => i.Value,
            NbtShort s => s.Value,
            NbtByte b => b.Value,
            _ => defaultValue
        };
    }
    public int GetNumeric(string key, int defaultValue = 0)
    {
        var tag = Get(key);
        return tag switch
        {
            NbtByte b => b.Value,
            NbtShort s => s.Value,
            NbtInt i => i.Value,
            NbtLong l => (int)l.Value,
            _ => defaultValue
        };
    }
    public string GetString(string key, string defaultValue = "") => Get<NbtString>(key)?.Value ?? defaultValue;
    public NbtCompound? GetCompound(string key) => Get<NbtCompound>(key);
    public NbtList? GetList(string key) => Get<NbtList>(key);

    public void Set(NbtTag tag)
    {
        _tags[tag.Name] = tag;
    }

    public void Set(string name, NbtTag tag)
    {
        tag.Name = name;
        _tags[name] = tag;
    }

    public void SetByte(string name, byte value) => Set(new NbtByte(name, value));
    public void SetShort(string name, short value) => Set(new NbtShort(name, value));
    public void SetInt(string name, int value) => Set(new NbtInt(name, value));
    public void SetLong(string name, long value) => Set(new NbtLong(name, value));
    public void SetFloat(string name, float value) => Set(new NbtFloat(name, value));
    public void SetDouble(string name, double value) => Set(new NbtDouble(name, value));
    public void SetString(string name, string value) => Set(new NbtString(name, value));

    public bool Remove(string key) => _tags.Remove(key);
    public void Clear() => _tags.Clear();

    public NbtTag this[string key]
    {
        get => _tags[key];
        set
        {
            value.Name = key;
            _tags[key] = value;
        }
    }

    public IEnumerator<NbtTag> GetEnumerator() => _tags.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _tags.Values.GetEnumerator();

    public override NbtTag Clone()
    {
        var clone = new NbtCompound(Name);
        foreach (var tag in _tags.Values)
        {
            clone.Set(tag.Clone());
        }
        return clone;
    }

    public override string ToString() => $"{TagType}('{Name}') [{_tags.Count} tags]";
}

public class NbtIntArray : NbtTag
{
    public override NbtTagType TagType => NbtTagType.IntArray;
    public int[] Value { get; set; } = [];

    public NbtIntArray() { }
    public NbtIntArray(int[] value) => Value = value;
    public NbtIntArray(string name, int[] value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtIntArray(Name, (int[])Value.Clone());
    public override string ToString() => $"{TagType}('{Name}'): [{Value.Length} ints]";
}

public class NbtLongArray : NbtTag
{
    public override NbtTagType TagType => NbtTagType.LongArray;
    public long[] Value { get; set; } = [];

    public NbtLongArray() { }
    public NbtLongArray(long[] value) => Value = value;
    public NbtLongArray(string name, long[] value) { Name = name; Value = value; }

    public override NbtTag Clone() => new NbtLongArray(Name, (long[])Value.Clone());
    public override string ToString() => $"{TagType}('{Name}'): [{Value.Length} longs]";
}
