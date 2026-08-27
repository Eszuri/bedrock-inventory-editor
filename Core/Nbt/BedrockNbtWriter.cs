using System;
using System.IO;
using System.Text;

namespace BedrockInventoryEditor.Core.Nbt;

public class BedrockNbtWriter
{
    private readonly BinaryWriter _writer;

    public BedrockNbtWriter(Stream stream)
    {
        _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
    }

    public static byte[] WriteToBytes(NbtCompound root, bool includeRootHeader = true)
    {
        using var ms = new MemoryStream();
        var writer = new BedrockNbtWriter(ms);
        if (includeRootHeader)
        {
            writer.WriteRootCompound(root);
        }
        else
        {
            writer.WriteCompoundPayload(root);
        }
        return ms.ToArray();
    }

    public void WriteRootCompound(NbtCompound compound)
    {
        _writer.Write((byte)NbtTagType.Compound);
        WriteStringValue(compound.Name);
        WriteCompoundPayload(compound);
    }

    public void WriteTag(NbtTag tag)
    {
        _writer.Write((byte)tag.TagType);
        WriteStringValue(tag.Name);
        WriteTagPayload(tag);
    }

    private void WriteTagPayload(NbtTag tag)
    {
        switch (tag)
        {
            case NbtByte b:
                _writer.Write(b.Value);
                break;
            case NbtShort s:
                _writer.Write(s.Value);
                break;
            case NbtInt i:
                _writer.Write(i.Value);
                break;
            case NbtLong l:
                _writer.Write(l.Value);
                break;
            case NbtFloat f:
                _writer.Write(f.Value);
                break;
            case NbtDouble d:
                _writer.Write(d.Value);
                break;
            case NbtByteArray ba:
                _writer.Write(ba.Value.Length);
                _writer.Write(ba.Value);
                break;
            case NbtString str:
                WriteStringValue(str.Value);
                break;
            case NbtList list:
                WriteList(list);
                break;
            case NbtCompound comp:
                WriteCompoundPayload(comp);
                break;
            case NbtIntArray ia:
                _writer.Write(ia.Value.Length);
                foreach (var val in ia.Value) _writer.Write(val);
                break;
            case NbtLongArray la:
                _writer.Write(la.Value.Length);
                foreach (var val in la.Value) _writer.Write(val);
                break;
            default:
                throw new InvalidOperationException($"Unsupported tag type: {tag.TagType}");
        }
    }

    private void WriteStringValue(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        _writer.Write((ushort)bytes.Length);
        if (bytes.Length > 0)
        {
            _writer.Write(bytes);
        }
    }

    private void WriteList(NbtList list)
    {
        _writer.Write((byte)list.ListType);
        _writer.Write(list.Count);
        foreach (var item in list)
        {
            WriteTagPayloadWithoutName(item);
        }
    }

    private void WriteTagPayloadWithoutName(NbtTag tag)
    {
        switch (tag)
        {
            case NbtByte b:
                _writer.Write(b.Value);
                break;
            case NbtShort s:
                _writer.Write(s.Value);
                break;
            case NbtInt i:
                _writer.Write(i.Value);
                break;
            case NbtLong l:
                _writer.Write(l.Value);
                break;
            case NbtFloat f:
                _writer.Write(f.Value);
                break;
            case NbtDouble d:
                _writer.Write(d.Value);
                break;
            case NbtByteArray ba:
                _writer.Write(ba.Value.Length);
                _writer.Write(ba.Value);
                break;
            case NbtString str:
                WriteStringValue(str.Value);
                break;
            case NbtList list:
                WriteList(list);
                break;
            case NbtCompound comp:
                WriteCompoundPayload(comp);
                break;
            case NbtIntArray ia:
                _writer.Write(ia.Value.Length);
                foreach (var val in ia.Value) _writer.Write(val);
                break;
            case NbtLongArray la:
                _writer.Write(la.Value.Length);
                foreach (var val in la.Value) _writer.Write(val);
                break;
            default:
                throw new InvalidOperationException($"Unsupported tag in list: {tag.TagType}");
        }
    }

    private void WriteCompoundPayload(NbtCompound compound)
    {
        foreach (var tag in compound)
        {
            WriteTag(tag);
        }
        _writer.Write((byte)NbtTagType.End);
    }
}
