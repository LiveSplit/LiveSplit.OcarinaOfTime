using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LiveSplit
{
    public class DeepPointer<T>
    {
        private Process _process;
        private List<int> _offsets;
        private int _base;
        private int _length;
        private string _module;

        public DeepPointer(Process process, string module, int base_, params int[] offsets)
        {
            _process = process;
            _module = module.ToLower();
            _base = base_;
            _offsets = new List<int>();
            _offsets.Add(0); // deref base first
            _offsets.AddRange(offsets);
        }

        public DeepPointer(int length, Process process, int base_, params int[] offsets)
            : this(process, base_, offsets)
        {
            _length = length;
        }

        public DeepPointer(int length, Process process, String module, int base_, params int[] offsets)
            : this(process, module, base_, offsets)
        {
            _length = length;
        }

        public DeepPointer(Process process, int base_, params int[] offsets)
        {
            _process = process;
            _base = base_;
            _offsets = new List<int>();
            _offsets.Add(0); // deref base first
            _offsets.AddRange(offsets);
        }

        public static T operator ~(DeepPointer<T> p)
        {
            if (typeof(T) == typeof(String))
            {
                String x = null;
                p.Deref(p._process, out x, p._length);
                return (T)((object)x);
            }
            else if (p._length <= 1)
            {
                T x = default(T);
                p.Deref<T>(p._process, out x);
                return x;
            }
            else if (typeof(T) == typeof(byte[]))
            {
                byte[] x = null;
                p.Deref(p._process, out x, p._length);
                return (T)((object)x);
            }
            throw new NotSupportedException("Only enums, structs, strings and byte arrays supported");
        }

        public static DeepPointer<T> operator +(DeepPointer<T> p, T x)
        {
            int offset = p._offsets[p._offsets.Count - 1];

            IntPtr ptr;
            p.DerefOffsets(p._process, out ptr);

            var type = typeof(T);
            IntPtr written;

            var buffer = ToBytes(x, type);
            var size = buffer.Length;

            var result = SafeNativeMethods.WriteProcessMemory(p._process.Handle, ptr + offset, buffer, size, out written);

            return p;
        }

        private bool Deref<T>(Process process, out T value)
        {
            int offset = _offsets[_offsets.Count - 1];
            IntPtr ptr;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessValue(process, ptr + offset, out value))
            {
                value = default(T);
                return false;
            }

            return true;
        }

        private bool Deref(Process process, Type type, out object value)
        {
            int offset = _offsets[_offsets.Count - 1];
            IntPtr ptr;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessValue(process, ptr + offset, type, out value))
            {
                value = default(object);
                return false;
            }

            return true;
        }

        private bool Deref(Process process, out byte[] value, int elementCount)
        {
            int offset = _offsets[_offsets.Count - 1];
            IntPtr ptr;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessBytes(process, ptr + offset, elementCount, out value))
            {
                value = null;
                return false;
            }

            return true;
        }

        private bool Deref(Process process, out Vector3f value)
        {
            int offset = _offsets[_offsets.Count - 1];
            IntPtr ptr;
            float x, y, z;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessValue(process, ptr + offset + 0, out x)
                || !ReadProcessValue(process, ptr + offset + 4, out y)
                || !ReadProcessValue(process, ptr + offset + 8, out z))
            {
                value = new Vector3f();
                return false;
            }

            value = new Vector3f(x, y, z);
            return true;
        }

        private bool Deref(Process process, out string str, int max)
        {
            var sb = new StringBuilder(max);

            int offset = _offsets[_offsets.Count - 1];
            IntPtr ptr;
            if (!this.DerefOffsets(process, out ptr)
                || !ReadProcessString(process, ptr + offset, sb))
            {
                str = String.Empty;
                return false;
            }

            str = sb.ToString();
            return true;
        }

        bool DerefOffsets(Process process, out IntPtr ptr)
        {
            if (!String.IsNullOrEmpty(_module))
            {
                ProcessModule module = process.Modules.Cast<ProcessModule>()
                    .FirstOrDefault(m => Path.GetFileName(m.FileName).ToLower() == _module);
                if (module == null)
                {
                    ptr = IntPtr.Zero;
                    return false;
                }

                ptr = module.BaseAddress + _base;
            }
            else
            {
                ptr = process.MainModule.BaseAddress + _base;
            }


            for (int i = 0; i < _offsets.Count - 1; i++)
            {
                if (!ReadProcessPtr32(process, ptr + _offsets[i], out ptr)
                    || ptr == IntPtr.Zero)
                {
                    return false;
                }
            }

            return true;
        }

        static bool ReadProcessValue<T>(Process process, IntPtr addr, out T val)
        {
            Type type = typeof(T);

            object val2;
            var result = ReadProcessValue(process, addr, type, out val2);
            val = (T)val2;

            return result;
        }

        static bool ReadProcessValue(Process process, IntPtr addr, Type type, out object val)
        {
            byte[] bytes;

            var result = ReadProcessBytes(process, addr, Marshal.SizeOf(type.IsEnum ? Enum.GetUnderlyingType(type) : type), out bytes);

            val = ResolveToType(bytes, type);

            return true;
        }

        static bool ReadProcessBytes(Process process, IntPtr addr, int elementCount, out byte[] val)
        {
            var bytes = new byte[elementCount];

            int read;
            val = null;
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read) || read != bytes.Length)
                return false;

            val = bytes;

            return true;
        }

        static byte[] ToBytes(object o, Type type)
        {
            var size = Marshal.SizeOf(type.IsEnum ? Enum.GetUnderlyingType(type) : type);

            if (typeof(byte[]) == type)
            {
                return (byte[])o;
            }
            else if (type.IsEnum)
            {
                return ToBytes(o, Enum.GetUnderlyingType(type));
            }

            if (type == typeof(int))
            {
                return BitConverter.GetBytes((int)o);
            }
            else if (type == typeof(uint))
            {
                return BitConverter.GetBytes((uint)o);
            }
            else if (type == typeof(float))
            {
                return BitConverter.GetBytes((float)o);
            }
            else if (type == typeof(byte))
            {
                return new byte[1] { (byte)o };
            }
            else if (type == typeof(bool))
            {
                return BitConverter.GetBytes((bool)o);
            }
            else if (type == typeof(short))
            {
                return BitConverter.GetBytes((short)o);
            }

            var buffer = new byte[size];

            IntPtr box = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(o, box, true);
                Marshal.Copy(box, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(box);
            }

            return buffer;
        }

        static object ResolveToType(byte[] bytes, Type type)
        {
            object val = default(object);

            if (type == typeof(int))
            {
                val = (object)BitConverter.ToInt32(bytes, 0);
            }
            else if (type == typeof(uint))
            {
                val = (object)BitConverter.ToUInt32(bytes, 0);
            }
            else if (type == typeof(float))
            {
                val = (object)BitConverter.ToSingle(bytes, 0);
            }
            else if (type == typeof(byte))
            {
                val = (object)bytes[0];
            }
            else if (type == typeof(bool))
            {
                val = (object)BitConverter.ToBoolean(bytes, 0);
            }
            else if (type == typeof(short))
            {
                val = (object)BitConverter.ToInt16(bytes, 0);
            }
            else if (type.IsEnum)
            {
                val = ResolveToType(bytes, Enum.GetUnderlyingType(type));
            }
            else
            {  
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    val = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
                }
                finally
                {
                    handle.Free();
                }
            }

            return val;
        }

        static bool ReadProcessPtr32(Process process, IntPtr addr, out IntPtr val)
        {
            byte[] bytes = new byte[4];
            int read;
            val = IntPtr.Zero;
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read) || read != bytes.Length)
                return false;
            val = (IntPtr)BitConverter.ToInt32(bytes, 0);
            return true;
        }

        static bool ReadProcessString(Process process, IntPtr addr, StringBuilder sb)
        {
            byte[] bytes = new byte[sb.Capacity];
            int read;
            if (!SafeNativeMethods.ReadProcessMemory(process.Handle, addr, bytes, bytes.Length, out read) || read != bytes.Length)
                return false;

            if (read >= 2 && bytes[1] == '\x0') // hack to detect utf-16
                sb.Append(Encoding.Unicode.GetString(bytes));
            else
                sb.Append(Encoding.ASCII.GetString(bytes));


            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '\0')
                {
                    sb.Remove(i, sb.Length - i);
                    break;
                }
            }

            return true;
        }
    }

    public class Vector3f
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public int IX { get { return (int)this.X; } }
        public int IY { get { return (int)this.Y; } }
        public int IZ { get { return (int)this.Z; } }

        public Vector3f() { }

        public Vector3f(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public float Distance(Vector3f other)
        {
            float result = (this.X - other.X) * (this.X - other.X) +
                (this.Y - other.Y) * (this.Y - other.Y) +
                (this.Z - other.Z) * (this.Z - other.Z);
            return (float)Math.Sqrt(result);
        }

        public float DistanceXY(Vector3f other)
        {
            float result = (this.X - other.X) * (this.X - other.X) +
                (this.Y - other.Y) * (this.Y - other.Y);
            return (float)Math.Sqrt(result);
        }

        public override string ToString()
        {
            return this.X + " " + this.Y + " " + this.Z;
        }
    }

    static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize, // should be IntPtr if we ever need to read a size bigger than 32 bit address space
            out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);
    }
}
