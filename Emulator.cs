using System.Diagnostics;
using System.Linq;

namespace LiveSplit.OcarinaOfTime
{
    public class Emulator
    {
        public Process Process { get; protected set; }
        public int Offset { get; protected set; }

        protected Emulator(Process process, int offset)
        {
            Process = process;
            Offset = offset;
        }

        public static Emulator TryConnect()
        {
            var process = Process.GetProcessesByName("Project64").FirstOrDefault();
            if (process != null)
            {
                return BuildProject64(process);
            }

            process = Process.GetProcessesByName("mupen64").FirstOrDefault();
            if (process != null)
            {
                return BuildMupen(process);
            }

            process = Process.GetProcessesByName("1964").FirstOrDefault();
            if (process != null)
            {
                return Build1964(process);
            }

            process = Process.GetProcessesByName("EmuHawk").FirstOrDefault();
            if (process != null)
            {
                return BuildBizHawk(process);
            }

            return null;
        }

        private static Emulator Build(Process process, int _base)
        {
            var offset = ~new DeepPointer<int>(process, _base);

            return new Emulator(process, offset);
        }

        private static Emulator BuildBizHawk(Process process)
        {
            var offset = ~new DeepPointer<int>(process, "mupen64plus.dll", (int)EmulatorBase.BizHawk);
            return new Emulator(process, offset);
        }

        private static Emulator Build1964(Process process)
        {
            ProcessModule module = process.MainModule;

            var _base = (int)EmulatorBase.Emu1964;
            return Build(process, _base);
        }

        private static Emulator BuildMupen(Process process)
        {
            ProcessModule module = process.MainModule;

            var _base = (int)EmulatorBase.Mupen64;
            return Build(process, _base - ((int)module.BaseAddress));
        }

        private static Emulator BuildProject64(Process process)
        {
            var version = process.MainWindowTitle;
            var _base = 0;

            if (version.EndsWith("1.6"))
                _base = (int)EmulatorBase.Project64_16;
            else
                _base = (int)EmulatorBase.Project64_17;

            return Build(process, _base);
        }

        public DeepPointer<T> CreatePointer<T>(int address)
        {
            return CreatePointer<T>(1, address);
        }

        public DeepPointer<T> CreatePointer<T>(int length, int address)
        {
            return new DeepPointer<T>(length, Process, Offset - (int)Process.MainModule.BaseAddress + address);
        }
    }
}
