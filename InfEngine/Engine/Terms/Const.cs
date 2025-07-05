using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace InfEngine.Engine.Terms;

/// <summary>
/// Ex.: N, Y, 1, -2, true, ...
/// </summary>
/// <param name="Name">Name of the const. Can be N, Y, 1, -2, true. We don't care about the type of the const. This
/// is validate during the type-checking phase.</param>
public record Const(ConstName Name) : Term
{
    public override bool Any<T>(Func<T, bool> pred)
    {
        if (this is not T)
        {
            return false;
        }

        return pred((this as T)!);
    }

    public override Term Replace<T>(Func<T, Term?> replacement)
    {
        if (this is not T)
        {
            return this;
        }
        
        var r = replacement((this as T)!);
        if (r == null)
            return this;
        
        return r;
    }

    public override IEnumerable<T> Descendants<T>()
    {
        yield break;
    }
}

public struct ConstName : IEquatable<ConstName>
{
    private string? _name;
    private UInt128 _data;
    public ConstType Type { get; }

    private ConstName(UInt128 value, ConstType type)
    {
        this._data = value;
        Type = type;
        this._name = null;
    }
    
    private ConstName(string name)
    {
        this._name = name;
        _data = 0;
        Type = ConstType.Name;
    }

    public static ConstName FromBool(bool value) => new((UInt128)(value ? 1 : 0), ConstType.Bool);
    public static ConstName FromU8(byte value) => new(value, ConstType.U8);
    public static ConstName FromU16(ushort value) => new(value, ConstType.U16);
    public static ConstName FromU32(uint value) => new(value, ConstType.U32);
    public static ConstName FromU64(ulong value) => new(value, ConstType.U64);
    public static ConstName FromI8(sbyte value) => new((UInt128)value, ConstType.I8);
    public static ConstName FromI16(short value) => new((UInt128)value, ConstType.I16);
    public static ConstName FromI32(int value) => new((UInt128)value, ConstType.I32);
    public static ConstName FromI64(long value) => new((UInt128)value, ConstType.I64);
    public static ConstName FromChar(char value) => new(value, ConstType.Char);
    public static ConstName FromChar8(byte value) => new(value, ConstType.Char8);
    public static ConstName FromChar32(uint value) => new(value, ConstType.Char32);
    public static ConstName FromU128(UInt128 value) => new(value, ConstType.U128);
    public static ConstName FromI128(Int128 value) => new((UInt128)value, ConstType.I128);
    public static ConstName FromName(string name) => new(name);
    public static ConstName FromUSize(nuint value) => new(value, ConstType.USize);
    public static ConstName FromISize(nint value) => new((UInt128)value, ConstType.ISize);

    public bool Equals(ConstName other)
    {
        if (Type == ConstType.Bool)
            return other.Type == ConstType.Bool && _data == other._data;
        if (Type == ConstType.Char)
            return other.Type == ConstType.Char && _data == other._data;
        if (Type == ConstType.Char8)
            return other.Type == ConstType.Char8 && _data == other._data;
        if (Type == ConstType.Char32)
            return other.Type == ConstType.Char32 && _data == other._data;
        if (Type == ConstType.Name)
            return other.Type == ConstType.Name && string.Equals(_name, other._name, StringComparison.Ordinal);
        
        if (this.IsSigned == other.IsSigned)
            return _data == other._data;
        if (this.IsSigned)
        {
            var ithis = (Int128)this._data;
            return ithis >= 0 && this._data == other._data;
        }
        
        var iother = (Int128)other._data;
        return iother >= 0 && this._data == other._data;
    }

    public bool IsSigned => Type == ConstType.I8 || Type == ConstType.I16 || Type == ConstType.I32 || 
                            Type == ConstType.I64 || Type == ConstType.I128 || Type == ConstType.ISize;

    public override int GetHashCode()
    {
        if (Type == ConstType.Name)
            return HashCode.Combine(_name);
    
        return HashCode.Combine(_data);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj == null)
            return false;
        if (obj is not ConstName constName)
            return false;
        return Equals(constName);
    }

    public override string ToString() => Type switch
    {
        ConstType.U8 => ((byte)this._data).ToString(),
        ConstType.U16 => ((ushort)this._data).ToString(),
        ConstType.U32 => ((uint)this._data).ToString(),
        ConstType.U64 => ((ulong)this._data).ToString(),
        ConstType.U128 => (this._data).ToString(),
        ConstType.I8 => ((sbyte)this._data).ToString(),
        ConstType.I16 => ((short)this._data).ToString(),
        ConstType.I32 => ((int)this._data).ToString(),
        ConstType.I64 => ((long)this._data).ToString(),
        ConstType.I128 => ((Int128)this._data).ToString(),
        ConstType.USize => ((nuint)this._data).ToString(),
        ConstType.ISize => ((nint)this._data).ToString(),
        ConstType.Bool => this._data != 0 ? "true" : "false",
        ConstType.Char => $"c'{((char)this._data).ToString()}'",
        ConstType.Char8 => $"c8'\\0x{Convert.ToString((byte)this._data, 16)}'",
        ConstType.Char32 =>$"c8'\\0x{Convert.ToString((uint)this._data, 16)}'",
        ConstType.Name => this._name ?? "",
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public static bool operator ==(ConstName left, ConstName right) => left.Equals(right);
    public static bool operator !=(ConstName left, ConstName right) => !left.Equals(right);
}