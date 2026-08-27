using System;
using System.IO;
using System.Text;

namespace BedrockInventoryEditor.Core.Nbt;

public class BedrockNbtReader
{
    private readonly BinaryReader _reader;

    public BedrockNbtReader(Stream stream)
    {
        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
    }

    public bool HasRootHeader { get; private set; }

    public static NbtCompound ReadFromBytes(byte[] data)
    {
        return ReadFromBytes(data, out _);
    }

    public static NbtCompound ReadFromBytes(byte[] data, out bool hasRootHeader)
    {
        using var ms = new MemoryStream(data);
        var reader = new BedrockNbtReader(ms);
        var res = reader.ReadRootCompound();
        hasRootHeader = reader.HasRootHeader;
        return res;
    }

    public NbtCompound ReadRootCompound()
    {
        if (_reader.BaseStream.Position >= _reader.BaseStream.Length)
            return new NbtCompound();

        var firstByte = _reader.ReadByte();
        if (firstByte == (byte)NbtTagType.Compound)
        {
            HasRootHeader = true;
            var name = ReadStringValue();
            var compound = new NbtCompound(name);
            ReadCompoundPayload(compound);
            return compound;
        }
        else
        {
            HasRootHeader = false;
            // Reset position if it didn't start with compound header
            _reader.BaseStream.Seek(-1, SeekOrigin.Current);
            var compound = new NbtCompound();
            ReadCompoundPayload(compound);
            return compound;
        }
    }

    public NbtTag ReadTag()
    {
        var tagType = (NbtTagType)_reader.ReadByte();
        if (tagType == NbtTagType.End)
            return null!;

        var name = ReadStringValue();
        return ReadTagPayload(tagType, name);
    }

    private NbtTag ReadTagPayload(NbtTagType type, string name)
    {
        return type switch
        {
            NbtTagType.Byte => new NbtByte(name, _reader.ReadByte()),
            NbtTagType.Short => new NbtShort(name, _reader.ReadInt16()),
            NbtTagType.Int => new NbtInt(name, _reader.ReadInt32()),
            NbtTagType.Long => new NbtLong(name, _reader.ReadInt64()),
            NbtTagType.Float => new NbtFloat(name, _reader.ReadSingle()),
            NbtTagType.Double => new NbtDouble(name, _reader.ReadDouble()),
            NbtTagType.ByteArray => ReadByteArray(name),
            NbtTagType.String => new NbtString(name, ReadStringValue()),
            NbtTagType.List => ReadList(name),
            NbtTagType.Compound => ReadCompound(name),
            NbtTagType.IntArray => ReadIntArray(name),
            NbtTagType.LongArray => ReadLongArray(name),
            _ => throw new FormatException($"Unknown NBT tag type: {(byte)type}")
        };
    }

    private string ReadStringValue()
    {
        var length = _reader.ReadUInt16();
        if (length == 0) return string.Empty;
        var bytes = _reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private NbtByteArray ReadByteArray(string name)
    {
        var length = _reader.ReadInt32();
        var bytes = _reader.ReadBytes(length);
        return new NbtByteArray(name, bytes);
    }

    private NbtList ReadList(string name)
    {
        var listType = (NbtTagType)_reader.ReadByte();
        var count = _reader.ReadInt32();
        var list = new NbtList(name, listType);

        for (var i = 0; i < count; i++)
        {
            var item = ReadTagPayloadWithoutName(listType);
            list.Add(item);
        }

        return list;
    }

    private NbtTag ReadTagPayloadWithoutName(NbtTagType type)
    {
        return type switch
        {
            NbtTagType.Byte => new NbtByte(string.Empty, _reader.ReadByte()),
            NbtTagType.Short => new NbtShort(string.Empty, _reader.ReadInt16()),
            NbtTagType.Int => new NbtInt(string.Empty, _reader.ReadInt32()),
            NbtTagType.Long => new NbtLong(string.Empty, _reader.ReadInt64()),
            NbtTagType.Float => new NbtFloat(string.Empty, _reader.ReadSingle()),
            NbtTagType.Double => new NbtDouble(string.Empty, _reader.ReadDouble()),
            NbtTagType.ByteArray => ReadByteArray(string.Empty),
            NbtTagType.String => new NbtString(string.Empty, ReadStringValue()),
            NbtTagType.List => ReadList(string.Empty),
            NbtTagType.Compound => ReadCompound(string.Empty),
            NbtTagType.IntArray => ReadIntArray(string.Empty),
            NbtTagType.LongArray => ReadLongArray(string.Empty),
            _ => throw new FormatException($"Unknown NBT tag type in list: {(byte)type}")
        };
    }

    private NbtCompound ReadCompound(string name)
    {
        var compound = new NbtCompound(name);
        ReadCompoundPayload(compound);
        return compound;
    }

    private void ReadCompoundPayload(NbtCompound compound)
    {
        while (true)
        {
            if (_reader.BaseStream.Position >= _reader.BaseStream.Length)
                break;

            var tagType = (NbtTagType)_reader.ReadByte();
            if (tagType == NbtTagType.End)
                break;

            var tagName = ReadStringValue();
            var tag = ReadTagPayload(tagType, tagName);
            compound.Set(tag);
        }
    }

    private NbtIntArray ReadIntArray(string name)
    {
        var length = _reader.ReadInt32();
        var array = new int[length];
        for (var i = 0; i < length; i++)
        {
            array[i] = _reader.ReadInt32();
        }
        return new NbtIntArray(name, array);
    }

    private NbtLongArray ReadLongArray(string name)
    {
        var length = _reader.ReadInt32();
        var array = new long[length];
        for (var i = 0; i < length; i++)
        {
            array[i] = _reader.ReadInt64();
        }
        return new NbtLongArray(name, array);
    }
}
