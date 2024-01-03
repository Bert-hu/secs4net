﻿using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Secs4Net;

public abstract partial class Item : IEquatable<Item>, IDisposable
{
    private const nuint ElementOffset0 = 0;
    private const nuint ElementOffset1 = 1;
    private const nuint ElementOffset2 = 2;
    private const nuint ElementOffset3 = 3;
    private static readonly Item EmptyL = new ListItem(SecsFormat.List, []);
    private static readonly Item EmptyA = new StringItem(SecsFormat.ASCII, string.Empty);
    private static readonly Item EmptyJ = new StringItem(SecsFormat.JIS8, string.Empty);
    private static readonly Item EmptyBoolean = new MemoryItem<bool>(SecsFormat.Boolean);
    private static readonly Item EmptyBinary = new MemoryItem<byte>(SecsFormat.Binary);
    private static readonly Item EmptyU1 = new MemoryItem<byte>(SecsFormat.U1);
    private static readonly Item EmptyU2 = new MemoryItem<ushort>(SecsFormat.U2);
    private static readonly Item EmptyU4 = new MemoryItem<uint>(SecsFormat.U4);
    private static readonly Item EmptyU8 = new MemoryItem<ulong>(SecsFormat.U8);
    private static readonly Item EmptyI1 = new MemoryItem<sbyte>(SecsFormat.I1);
    private static readonly Item EmptyI2 = new MemoryItem<short>(SecsFormat.I2);
    private static readonly Item EmptyI4 = new MemoryItem<int>(SecsFormat.I4);
    private static readonly Item EmptyI8 = new MemoryItem<long>(SecsFormat.I8);
    private static readonly Item EmptyF4 = new MemoryItem<float>(SecsFormat.F4);
    private static readonly Item EmptyF8 = new MemoryItem<double>(SecsFormat.F8);
    public static Encoding JIS8Encoding { get; set; } = Encoding.UTF8;

    public SecsFormat Format { get; }

    static Item()
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException("This version is only work on little endian hardware.");
        }
    }

    private protected Item(SecsFormat format)
    {
        Format = format;
    }

    public abstract int Count { get; }

    /// <summary>
    /// Encode the item to SECS binary format
    /// </summary>
    public abstract void EncodeTo(IBufferWriter<byte> buffer);

    /// <summary>
    /// Indexer of List items.
    /// Be careful of setter operation. Since the original slot will be overridden.
    /// So, it has no chance to be Disposed along with the List's Dispose method.
    /// You can invoke <see cref="Dispose"/> method on the original item by yourself or till the GC collects it.
    /// </summary>
    /// <exception cref="NotSupportedException">When the item's <see cref="Format"/> is not <see cref="SecsFormat.List"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public virtual Item this[int index]
    {
        get => throw ThrowNotSupportException(Format);
        set => throw ThrowNotSupportException(Format);
    }

    /// <summary>
    /// Get List's sub-items
    /// </summary>
    /// <exception cref="NotSupportedException">When the item's <see cref="Format"/> is not <see cref="SecsFormat.List"/></exception>
    public virtual Item[] Items
        => throw ThrowNotSupportException(Format);

    /// <summary>
    /// Get the first element of item array value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException">When item is empty or data length less than sizeof(<typeparamref name="T"/>)</exception>
    /// <exception cref="NotSupportedException">when the item's <see cref="Format"/> is <see cref="SecsFormat.List"/> or <see cref="SecsFormat.ASCII"/> or <see cref="SecsFormat.JIS8"/></exception>
    public virtual ref T FirstValue<T>() where T : unmanaged, IEquatable<T>
        => throw ThrowNotSupportException(Format);

    /// <summary>
    /// Get the first element of item array value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">when <see cref="Format"/> is <see cref="SecsFormat.List"/> or <see cref="SecsFormat.ASCII"/> or <see cref="SecsFormat.JIS8"/></exception>
    public virtual T FirstValueOrDefault<T>(T defaultValue = default) where T : unmanaged, IEquatable<T>
        => throw ThrowNotSupportException(Format);

    /// <summary>
    /// Get item array as <see cref="Memory{T}"/>
    /// </summary>
    /// <exception cref="NotSupportedException">when <see cref="Format"/> is <see cref="SecsFormat.List"/> or <see cref="SecsFormat.ASCII"/> or <see cref="SecsFormat.JIS8"/></exception>
    public virtual Memory<T> GetMemory<T>() where T : unmanaged, IEquatable<T>
        => throw ThrowNotSupportException(Format);

    /// <summary>
    /// Get item string value
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">when the <see cref="Format"/> is not <see cref="SecsFormat.ASCII"/> or <see cref="SecsFormat.JIS8"/></exception>
    public virtual string GetString()
        => throw ThrowNotSupportException(Format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static NotSupportedException ThrowNotSupportException(SecsFormat format, [CallerMemberName] string? memberName = null)
        => new($"{memberName} is not supported, since the item's {nameof(Format)} is {format}");

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public static bool operator !=(Item? r1, Item? r2)
        => !(r1 == r2);

    public static bool operator ==(Item? r1, Item? r2)
        => ReferenceEquals(r1, r2) || (r1?.Equals(r2) ?? false);

    public sealed override bool Equals(object? obj)
        => Equals(obj as Item);

    public bool Equals(Item? other)
        => other is not null && IsEquals(other);

    private protected abstract bool IsEquals(Item other);

    public sealed override string ToString() => $"{Format.GetName()} [{Count}]";

    internal byte[] GetEncodedBytes()
    {
        using var buffer = new ArrayPoolBufferWriter<byte>();
        EncodeTo(buffer);
        return buffer.WrittenSpan.ToArray();
    }

    [DebuggerDisplay("Encoded Bytes")]
    private sealed class EncodedByteDebugView(Item item)
    {

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Bytes => item.GetEncodedBytes();
    }
}
