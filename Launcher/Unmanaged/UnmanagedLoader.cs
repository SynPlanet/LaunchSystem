using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;
using System.Management;
using System.ServiceModel.Channels;
using System.Xml;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace Launcher.Unmanaged
{
    static class UnmanagedLoader
    {
        delegate bool DllEntryProc(IntPtr hinstDLL, uint fdwReason, IntPtr lpReserved);

        const int IMAGE_DOS_SIGNATURE = 0x5A4D;
        const uint IMAGE_NT_SIGNATURE = 0x00004550;
        const uint MEM_RELEASE = 0x8000;
        const uint MEM_DECOMMIT = 0x4000;
        const uint IMAGE_SCN_MEM_EXECUTE = 0x20000000;
        const uint IMAGE_SCN_MEM_READ = 0x40000000;
        const uint IMAGE_SCN_MEM_WRITE = 0x80000000;
        const uint IMAGE_SCN_MEM_DISCARDABLE = 0x02000000;
        const uint IMAGE_SCN_MEM_NOT_CACHED = 0x04000000;

        const int IMAGE_SCN_CNT_INITIALIZED_DATA = 0x00000040;
        const int IMAGE_SCN_CNT_UNINITIALIZED_DATA = 0x00000080;

        const int PAGE_NOACCESS = 0x01;
        const int PAGE_READONLY = 0x02;
        const int PAGE_READWRITE = 0x04;
        const int PAGE_WRITECOPY = 0x08;
        const int PAGE_EXECUTE = 0x10;
        const int PAGE_EXECUTE_READ = 0x20;
        const int PAGE_EXECUTE_READWRITE = 0x40;
        const int PAGE_EXECUTE_WRITECOPY = 0x80;
        const int PAGE_NOCACHE = 0x200;

        const uint DLL_PROCESS_ATTACH = 1;

        const int IMAGE_DIRECTORY_ENTRY_EXPORT = 0;  // Export Directory

        static int[][][] ProtectionFlags =
            new int[][][] {
	            new int[][] {
		            // not executable
		            new int[]{PAGE_NOACCESS, PAGE_WRITECOPY},
		            new int[]{PAGE_READONLY, PAGE_READWRITE},
	            },
                new int[][] {
		            // executable
		            new int[]{PAGE_EXECUTE, PAGE_EXECUTE_WRITECOPY},
		            new int[]{PAGE_EXECUTE_READ, PAGE_EXECUTE_READWRITE},
	            }
            };

        public static IntPtr MemoryLoadLibrary(string filename)
        {
            try
            {
                byte[] fileContent = null;
                IntPtr returnPTR = IntPtr.Zero;
                try
                {
                    try { fileContent = LoadFile(filename); }
                    catch
                    {
                        var paths = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var path in paths)
                            try { fileContent = LoadFile(string.Format("{0}\\{1}", path, filename)); }
                            catch { }
                        if (fileContent == null) throw;
                    }
                }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(LogLevel.Basic, ex, string.Format(Local.Message.LoaderMessage0_P2, filename)); return IntPtr.Zero;  }

                try { return MemoryLoadLibrary(fileContent); }
                catch { }
                Information info = GetInformation(fileContent);
                string key = GetKey();
                if (key == null) return IntPtr.Zero;
                string serial = GetSerial();
                ILicence contract = GetContract(key);
                fileContent = GetAssembly(GetContent(fileContent), contract.GetHeader(Environment.MachineName, key, serial, info.ModuleName, info.Version));
                return MemoryLoadLibrary(fileContent);
            }
            catch { LogManager.Instance.Launcher.Exception(LogLevel.Basic, string.Format(Local.Message.LoaderMessage0_P1, filename)); return IntPtr.Zero; }
        }

        private static byte[] LoadFile(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private static Information GetInformation(byte[] fileContent)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(Information))];
            Buffer.BlockCopy(fileContent, 0, buffer, 0, buffer.Length);
            return BytesToStructure<Information>(buffer);
        }

        private static string GetKey()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                foreach (var item in store.Certificates)
                {
                    if (item.FriendlyName.IndexOf("Keeper|") == 0) return item.SubjectName.Name.Substring(3);
                }
            }
            finally { store.Close(); }
            return string.Empty;
        }

        private static string GetSerial()
        {
            ManagementObject obj = new ManagementObject(string.Format("win32_logicaldisk.deviceid=\"{0}:\"", Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1)));
            obj.Get();
            return obj["VolumeSerialNumber"].ToString();
        }

        private static ILicence GetContract(string key)
        {
            string address = string.Empty;
            X509Certificate2 certificate = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                foreach (var item in store.Certificates) if (item.SubjectName.Name.Substring(3) == key) certificate = item;
                address = certificate.FriendlyName.Replace("Keeper|", "");
            }
            finally { store.Close(); }
            ChannelFactory<ILicence> channel = new ChannelFactory<ILicence>(GetNetTcpBinding(),
                   new EndpointAddress(string.Format("net.tcp://{0}:9875/02F6DB4C-ED1F-4972-A308-C3F0B7457032", address)));
            channel.Credentials.ClientCertificate.Certificate = certificate;
            ILicence contract = channel.CreateChannel();
            ((IChannel)contract).Open();
            return contract;
        }

        private static NetTcpBinding GetNetTcpBinding()
        {
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.Transport)
            {
                OpenTimeout = TimeSpan.MaxValue,
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxBufferSize = 47483647,
                MaxReceivedMessageSize = 47483647,
                MaxConnections = 5
            };
            binding.ReaderQuotas = new XmlDictionaryReaderQuotas()
            {
                MaxArrayLength = 47483647,
                MaxStringContentLength = 4096,
                MaxBytesPerRead = 4096,
                MaxNameTableCharCount = 4096
            };
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            return binding;
        }

        private static T BytesToStructure<T>(byte[] buffer) where T : struct
        {
            if (Marshal.SizeOf(typeof(T)) != buffer.Length) throw new Exception();
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(buffer.Length);
                Marshal.Copy(buffer, 0, ptr, buffer.Length);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally { if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr); }
        }

        private static byte[] Decrypt(Block[] header, byte[] data)
        {
            byte[] content = new byte[GetSize(header)];
            byte[] iv = new byte[8];
            byte[] key = new byte[16];
            int offset = 0;
            for (int i = 0; i < header.Length - 1; i++)
            {
                Buffer.BlockCopy(data, header[i].Offset, content, offset, header[i].Size);
                offset += header[i].Size;
            }
            Buffer.BlockCopy(data, header[header.Length - 1].Offset, content, offset, header[header.Length - 1].Size);
            Buffer.BlockCopy(content, content.Length - 24, iv, 0, 8);
            Buffer.BlockCopy(content, content.Length - 16, key, 0, 16);
            using (RC2CryptoServiceProvider rc2 = new RC2CryptoServiceProvider() { KeySize = key.Length * 8, IV = iv, Key = key })
            using (ICryptoTransform decryptor = rc2.CreateDecryptor())
            using (MemoryStream memory = new MemoryStream(content, 0, content.Length - 24))
            using (CryptoStream csDecryptor = new CryptoStream(memory, decryptor, CryptoStreamMode.Read))
            {
                int length = csDecryptor.Read(content, 0, content.Length);
                byte[] buffer = new byte[length];
                Buffer.BlockCopy(content, 0, buffer, 0, length);
                return buffer;
            }
        }

        private static int GetSize(Block[] header)
        {
            int size = 0;
            foreach (var item in header) size += item.Size;
            return size;
        }

        private static byte[] GetAssembly(byte[] content, byte[] binHeader)
        {
            List<Block> header = new List<Block>(GetHeader(binHeader));
            header.Sort(delegate(Block x, Block y)
            {
                if (x.Number > y.Number) return 1;
                else if (x.Number < y.Number) return -1;
                return 0;
            });
            return Decrypt(header.ToArray(), content);
        }

        private static Block[] GetHeader(byte[] buffer)
        {
            int sizeItem = Marshal.SizeOf(typeof(Block));
            int countItems = buffer.Length / sizeItem;
            Block[] header = new Block[countItems];
            for (int i = 0; i < countItems; i++)
            {
                IntPtr ptr = Marshal.AllocHGlobal(sizeItem);
                Marshal.Copy(buffer, i * sizeItem, ptr, sizeItem);
                header[i] = (Block)Marshal.PtrToStructure(ptr, typeof(Block));
                Marshal.FreeHGlobal(ptr);
            }
            return header;
        }

        private static byte[] GetContent(byte[] fileContent)
        {
            int size = Marshal.SizeOf(typeof(Information));
            byte[] content = new byte[fileContent.Length - size];
            Buffer.BlockCopy(fileContent, size, content, 0, content.Length);
            return content;
        }

        private static IntPtr MemoryLoadLibrary(byte[] data)
        {
            IntPtr result = IntPtr.Zero;
            IMAGE_DOS_HEADER dos_header = data.GetStruct<IMAGE_DOS_HEADER>(0);
            if (dos_header.e_magic != IMAGE_DOS_SIGNATURE) throw new Exception("Not a valid executable file.");

            dynamic headerNT;
            if (Environment.Is64BitProcess) headerNT = data.GetStruct<IMAGE_NT_HEADERS64>(dos_header.e_lfanew);
            else headerNT = data.GetStruct<IMAGE_NT_HEADERS32>(dos_header.e_lfanew);

            if (headerNT.Signature != IMAGE_NT_SIGNATURE) throw new Exception("No PE header found.");
            UIntPtr code = Kernel32.VirtualAlloc(new UIntPtr(headerNT.OptionalHeader.ImageBase), headerNT.OptionalHeader.SizeOfImage, AllocationType.RESERVE, MemoryProtection.READWRITE);
            if (code == UIntPtr.Zero) code = Kernel32.VirtualAlloc(UIntPtr.Zero, headerNT.OptionalHeader.SizeOfImage, AllocationType.RESERVE, MemoryProtection.READWRITE);
            if (code == UIntPtr.Zero) throw new Exception("Can't reserve memory.");

            result = Kernel32.HeapAlloc(Kernel32.GetProcessHeap(), 0, Marshal.SizeOf(typeof(MEMORYMODULE)));
            MEMORYMODULE memoryModule = (MEMORYMODULE)Marshal.PtrToStructure(result, typeof(MEMORYMODULE));
            memoryModule.codeBase = code;
            memoryModule.numModules = 0;
            memoryModule.modules = IntPtr.Zero;
            memoryModule.initialized = 0;

            UIntPtr headers = Kernel32.VirtualAlloc(code, headerNT.OptionalHeader.SizeOfImage, AllocationType.COMMIT, MemoryProtection.READWRITE);
            Memcpy(headers, data, dos_header.e_lfanew + (int)headerNT.OptionalHeader.SizeOfHeaders);

            if (Environment.Is64BitProcess) memoryModule.headers = new IntPtr((long)headers.ToUInt64() + dos_header.e_lfanew);
            else memoryModule.headers = new IntPtr((int)headers.ToUInt32() + dos_header.e_lfanew);

            if (Environment.Is64BitProcess)
            {
                IMAGE_NT_HEADERS64 temp = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>();
                temp.OptionalHeader.ImageBase = code.ToUInt64();
                memoryModule.headers.SetStruct<IMAGE_NT_HEADERS64>(temp);

            }
            else
            {
                IMAGE_NT_HEADERS32 temp = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>();
                temp.OptionalHeader.ImageBase = code.ToUInt32();
                memoryModule.headers.SetStruct<IMAGE_NT_HEADERS32>(temp);
            }
            result.SetStruct<MEMORYMODULE>(memoryModule);
            CopySections(data, headerNT, result);
            long locationDelta = (long)(code.ToUInt64() - headerNT.OptionalHeader.ImageBase);
            if (locationDelta != 0) PerformBaseRelocation(result, locationDelta);
            if (!BuildImportTable(result))
            {
                MemoryFreeLibrary(result);
                return IntPtr.Zero;
            }

            if (Environment.Is64BitProcess) FinalizeSections64(result);
            else FinalizeSections32(result);
            memoryModule = result.GetStruct<MEMORYMODULE>();
            if (Environment.Is64BitProcess) headerNT = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>();
            else headerNT = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>();
            DllEntryProc DllEntry;
            if (headerNT.OptionalHeader.AddressOfEntryPoint != 0)
            {
                DllEntry = (DllEntryProc)Marshal.GetDelegateForFunctionPointer(new IntPtr((long)memoryModule.codeBase.ToUInt64() + headerNT.OptionalHeader.AddressOfEntryPoint), typeof(DllEntryProc));
                if (DllEntry == null)
                {
                    MemoryFreeLibrary(result);
                    return IntPtr.Zero;
                }
                if (!DllEntry(new IntPtr((long)memoryModule.codeBase.ToUInt64()), DLL_PROCESS_ATTACH, IntPtr.Zero))
                {
                    MemoryFreeLibrary(result);
                    return IntPtr.Zero;
                }
                memoryModule.initialized = 1;
                result.SetStruct<MEMORYMODULE>(memoryModule);
            }

            return result;
        }

        public static T GetProcMethod<T>(IntPtr memoryModulePTR, string name) where T : class
        {
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_DATA_DIRECTORY directory;
            if (Environment.Is64BitProcess) directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>().OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
            else directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>().OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
            if (directory.Size == 0) return null;
            IntPtr exportsPtr = new IntPtr((long)memoryModule.codeBase.ToUInt64() + directory.VirtualAddress);
            IMAGE_EXPORT_DIRECTORY exports = exportsPtr.GetStruct<IMAGE_EXPORT_DIRECTORY>();
            if (exports.NumberOfNames == 0 || exports.NumberOfFunctions == 0) return null;
            // IntPtr nameRefPtr = new IntPtr((int)memoryModule.codeBase.ToUInt64() + exports.AddressOfNames);
            IntPtr nameRefPtr = memoryModule.codeBase.ToIntPtr().Offset(exports.AddressOfNames);
            // IntPtr ordinalPtr = new IntPtr((int)memoryModule.codeBase.ToUInt64() + exports.AddressOfNameOrdinals);
            IntPtr ordinalPtr = memoryModule.codeBase.ToIntPtr().Offset(exports.AddressOfNameOrdinals);
            int idx = -1;
            for (int i = 0; i < exports.NumberOfNames; i++, nameRefPtr = nameRefPtr.Offset(4), ordinalPtr = ordinalPtr.Offset(2))
            {
                int nameRef = nameRefPtr.GetStruct<int>();
                short ordinal = ordinalPtr.GetStruct<short>();
                string methodName = Marshal.PtrToStringAnsi(memoryModule.codeBase.ToIntPtr().Offset(nameRef));
                if (methodName == name)
                {
                    idx = ordinal;
                    break;
                }
            }
            if (idx == -1) return null;
            if ((uint)idx > exports.NumberOfFunctions) return null;
            IntPtr methodPtr = memoryModule.codeBase.ToIntPtr().Offset(memoryModule.codeBase.ToIntPtr().Offset(exports.AddressOfFunctions + idx * 4).GetStruct<int>());
            if (methodPtr == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer(methodPtr, typeof(T)) as T;
        }

        public static IntPtr GetProcMethod(IntPtr memoryModulePTR, string name) 
        {
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_DATA_DIRECTORY directory;
            if (Environment.Is64BitProcess) directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>().OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
            else directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>().OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
            if (directory.Size == 0) return IntPtr.Zero;
            IntPtr exportsPtr = new IntPtr((long)memoryModule.codeBase.ToUInt64() + directory.VirtualAddress);
            IMAGE_EXPORT_DIRECTORY exports = exportsPtr.GetStruct<IMAGE_EXPORT_DIRECTORY>();
            if (exports.NumberOfNames == 0 || exports.NumberOfFunctions == 0) return IntPtr.Zero;
            // IntPtr nameRefPtr = new IntPtr((int)memoryModule.codeBase.ToUInt64() + exports.AddressOfNames);
            IntPtr nameRefPtr = memoryModule.codeBase.ToIntPtr().Offset(exports.AddressOfNames);
            // IntPtr ordinalPtr = new IntPtr((int)memoryModule.codeBase.ToUInt64() + exports.AddressOfNameOrdinals);
            IntPtr ordinalPtr = memoryModule.codeBase.ToIntPtr().Offset(exports.AddressOfNameOrdinals);
            int idx = -1;
            for (int i = 0; i < exports.NumberOfNames; i++, nameRefPtr = nameRefPtr.Offset(4), ordinalPtr = ordinalPtr.Offset(2))
            {
                int nameRef = nameRefPtr.GetStruct<int>();
                short ordinal = ordinalPtr.GetStruct<short>();
                string methodName = Marshal.PtrToStringAnsi(memoryModule.codeBase.ToIntPtr().Offset(nameRef));
                if (methodName == name)
                {
                    idx = ordinal;
                    break;
                }
            }
            if (idx == -1) return IntPtr.Zero;
            if ((uint)idx > exports.NumberOfFunctions) return IntPtr.Zero;
            IntPtr ptr = memoryModule.codeBase.ToIntPtr().Offset(exports.AddressOfFunctions + idx * 4);
            if (IntPtr.Size == 8) ptr = memoryModule.codeBase.ToIntPtr().Offset(ptr.GetStruct<long>());
            else ptr = memoryModule.codeBase.ToIntPtr().Offset(ptr.GetStruct<int>());
            return ptr;
        }

        private static void FinalizeSections64(IntPtr memoryModulePTR)
        {
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_NT_HEADERS64 header = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>();
            IntPtr sectionPtr = new IntPtr(memoryModule.headers.ToInt64() + Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader").ToInt64() + header.FileHeader.SizeOfOptionalHeader);
            IMAGE_SECTION_HEADER section = sectionPtr.GetStruct<IMAGE_SECTION_HEADER>();
            ulong imageOffset = (header.OptionalHeader.ImageBase & 0xffffffff00000000);
            for (int i = 0; i < header.FileHeader.NumberOfSections; i++, sectionPtr = new IntPtr(sectionPtr.ToInt64() + Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER))), section = sectionPtr.GetStruct<IMAGE_SECTION_HEADER>())
            {
                int protect, oldProtect, size;
                int executable = ((uint)section.Characteristics & IMAGE_SCN_MEM_EXECUTE) != 0 ? 1 : 0;
                int readable = ((uint)section.Characteristics & IMAGE_SCN_MEM_READ) != 0 ? 1 : 0;
                int writeable = ((uint)section.Characteristics & IMAGE_SCN_MEM_WRITE) != 0 ? 1 : 0;

                if (((uint)section.Characteristics & IMAGE_SCN_MEM_DISCARDABLE) != 0)
                {
                    // section is not needed any more and can safely be freed
                    Kernel32.VirtualFree(new UIntPtr((ulong)section.PhysicalAddress | imageOffset), new UIntPtr(section.SizeOfRawData), MEM_DECOMMIT);
                    continue;
                }
                protect = ProtectionFlags[executable][readable][writeable];
                if (((uint)section.Characteristics & IMAGE_SCN_MEM_NOT_CACHED) != 0) protect |= PAGE_NOCACHE;
                size = (int)section.SizeOfRawData;
                if (size == 0)
                {
                    if (((int)section.Characteristics & IMAGE_SCN_CNT_INITIALIZED_DATA) != 0) size = (int)header.OptionalHeader.SizeOfInitializedData;
                    else if (((int)section.Characteristics & IMAGE_SCN_CNT_UNINITIALIZED_DATA) != 0) size = (int)header.OptionalHeader.SizeOfUninitializedData;
                }
                if (size > 0)
                    if (!Kernel32.VirtualProtect(new UIntPtr((ulong)section.PhysicalAddress | imageOffset), (uint)size, (uint)protect, out oldProtect)) throw new Exception("Error protecting memory page.");
            }
            memoryModule.headers.SetStruct<IMAGE_NT_HEADERS64>(header);
            memoryModulePTR.SetStruct<MEMORYMODULE>(memoryModule);
        }

        private static void FinalizeSections32(IntPtr memoryModulePTR)
        {
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_NT_HEADERS32 header = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>();
            IntPtr sectionPtr = new IntPtr(memoryModule.headers.ToInt64() + Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS32), "OptionalHeader").ToInt64() + header.FileHeader.SizeOfOptionalHeader);
            IMAGE_SECTION_HEADER section = sectionPtr.GetStruct<IMAGE_SECTION_HEADER>();
            uint imageOffset = 0;
            for (int i = 0; i < header.FileHeader.NumberOfSections; i++, sectionPtr = new IntPtr(sectionPtr.ToInt64() + +Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER))), section = sectionPtr.GetStruct<IMAGE_SECTION_HEADER>())
            {
                int protect, oldProtect, size;
                int executable = ((uint)section.Characteristics & IMAGE_SCN_MEM_EXECUTE) != 0 ? 1 : 0;
                int readable = ((uint)section.Characteristics & IMAGE_SCN_MEM_READ) != 0 ? 1 : 0;
                int writeable = ((uint)section.Characteristics & IMAGE_SCN_MEM_WRITE) != 0 ? 1 : 0;

                if (((uint)section.Characteristics & IMAGE_SCN_MEM_DISCARDABLE) != 0)
                {
                    // section is not needed any more and can safely be freed
                    Kernel32.VirtualFree(new UIntPtr(section.PhysicalAddress | imageOffset), new UIntPtr(section.SizeOfRawData), MEM_DECOMMIT);
                    continue;
                }
                protect = ProtectionFlags[executable][readable][writeable];
                if (((uint)section.Characteristics & IMAGE_SCN_MEM_NOT_CACHED) != 0) protect |= PAGE_NOCACHE;
                size = (int)section.SizeOfRawData;
                if (size == 0)
                {
                    if (((int)section.Characteristics & IMAGE_SCN_CNT_INITIALIZED_DATA) != 0) size = (int)header.OptionalHeader.SizeOfInitializedData;
                    else if (((int)section.Characteristics & IMAGE_SCN_CNT_UNINITIALIZED_DATA) != 0) size = (int)header.OptionalHeader.SizeOfUninitializedData;
                }
                if (size > 0)
                    if (!Kernel32.VirtualProtect(new UIntPtr((ulong)section.PhysicalAddress | imageOffset), (uint)size, (uint)protect, out oldProtect)) throw new Exception("Error protecting memory page.");
            }
        }

        public static void MemoryFreeLibrary(IntPtr memoryModulePTR)
        {
            if (memoryModulePTR != IntPtr.Zero)
            {
                MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
                if (memoryModule.initialized != 0)
                {
                    DllEntryProc DllEntry;
                    if (Environment.Is64BitProcess) DllEntry = (DllEntryProc)Marshal.GetDelegateForFunctionPointer(new IntPtr((long)memoryModule.codeBase.ToUInt64() + memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>().OptionalHeader.AddressOfEntryPoint), typeof(DllEntryProc));
                    else DllEntry = (DllEntryProc)Marshal.GetDelegateForFunctionPointer(new IntPtr((long)memoryModule.codeBase.ToUInt64() + memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>().OptionalHeader.AddressOfEntryPoint), typeof(DllEntryProc));
                    if (DllEntry != null) DllEntry(new IntPtr((long)memoryModule.codeBase.ToUInt64()), 0, IntPtr.Zero);
                    memoryModule.initialized = 0;
                    memoryModulePTR.SetStruct<MEMORYMODULE>(memoryModule);
                }
                if (memoryModule.modules != IntPtr.Zero)
                {
                    IntPtr ptr = memoryModule.modules;
                    for (int i = 0; i < memoryModule.numModules; i++)
                    {
                        if (ptr != new IntPtr(-1)) Kernel32.FreeLibrary(ptr);
                        ptr = ptr.Offset(IntPtr.Size);

                    }
                    Marshal.FreeHGlobal(memoryModule.modules);
                    memoryModulePTR.SetStruct<MEMORYMODULE>(memoryModule);
                }
                if (memoryModule.codeBase != UIntPtr.Zero) Kernel32.VirtualFree(memoryModule.codeBase, UIntPtr.Zero, MEM_RELEASE);
                Kernel32.HeapFree(Kernel32.GetProcessHeap(), 0, memoryModulePTR);
            }
        }

        private static bool BuildImportTable(IntPtr memoryModulePTR)
        {
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_DATA_DIRECTORY directory;
            if (Environment.Is64BitProcess) directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>().OptionalHeader.DataDirectory[1];
            else directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>().OptionalHeader.DataDirectory[1];
            if (directory.Size > 0)
            {
                IntPtr importDescPtr = new IntPtr((long)memoryModule.codeBase.ToUInt64() + directory.VirtualAddress);
                IMAGE_IMPORT_DESCRIPTOR importDesc = importDescPtr.GetStruct<IMAGE_IMPORT_DESCRIPTOR>();
                for (; !Kernel32.IsBadReadPtr(importDescPtr, (uint)Marshal.SizeOf(typeof(IMAGE_IMPORT_DESCRIPTOR))) && importDesc.Name != 0; importDescPtr = new IntPtr(importDescPtr.ToInt64() + Marshal.SizeOf(typeof(IMAGE_IMPORT_DESCRIPTOR))), importDesc = importDescPtr.GetStruct<IMAGE_IMPORT_DESCRIPTOR>())
                {
                    IntPtr thunkRef, funcRef;
                    IntPtr tempPtr = new IntPtr((long)memoryModule.codeBase.ToUInt64() + importDesc.Name);
                    string dllname = Marshal.PtrToStringAnsi(tempPtr);
                    IntPtr handle = Kernel32.LoadLibrary(dllname);
                    if (handle == IntPtr.Zero) return false;
                    if (memoryModule.modules == IntPtr.Zero) memoryModule.modules = Marshal.AllocHGlobal(new IntPtr((memoryModule.numModules + 1) * IntPtr.Size));
                    else memoryModule.modules = Marshal.ReAllocHGlobal(memoryModule.modules, new IntPtr((memoryModule.numModules + 1) * IntPtr.Size));
                    if (memoryModule.modules == IntPtr.Zero) return false;
                    IntPtr modulePtr = new IntPtr(memoryModule.modules.ToInt64() + IntPtr.Size * memoryModule.numModules);
                    memoryModule.numModules++;
                    modulePtr.SetStruct<IntPtr>(handle);
                    if (importDesc.OriginalFirstThunk != 0)
                    {
                        thunkRef = new IntPtr((long)memoryModule.codeBase.ToUInt64() + importDesc.OriginalFirstThunk);
                        funcRef = new IntPtr((long)memoryModule.codeBase.ToUInt64() + importDesc.FirstThunk);
                    }
                    else
                    {
                        thunkRef = new IntPtr((long)memoryModule.codeBase.ToUInt64() + importDesc.FirstThunk);
                        funcRef = new IntPtr((long)memoryModule.codeBase.ToUInt64() + importDesc.FirstThunk);
                    }
                    for (; thunkRef.GetStruct<IntPtr>() != IntPtr.Zero; thunkRef = new IntPtr(thunkRef.ToInt64() + IntPtr.Size), funcRef = new IntPtr(funcRef.ToInt64() + IntPtr.Size))
                    {
                        long IMAGE_ORDINAL_FLAG = ((long)1) << (IntPtr.Size * 8);
                        if ((thunkRef.GetStruct<IntPtr>().ToInt64() & IMAGE_ORDINAL_FLAG) != 0)
                            funcRef.SetStruct<IntPtr>(Kernel32.GetProcAddress(handle, Marshal.PtrToStringAnsi(thunkRef.GetStruct<IntPtr>())));
                        else
                        {
                            IntPtr thunkDataPtr = new IntPtr((long)memoryModule.codeBase.ToUInt64() + thunkRef.GetStruct<IntPtr>().ToInt64());
                            IMAGE_IMPORT_BY_NAME thunkData = thunkDataPtr.GetStruct<IMAGE_IMPORT_BY_NAME>();
                            IntPtr procName = new IntPtr((long)thunkDataPtr.ToInt64() + (long)Marshal.OffsetOf(typeof(IMAGE_IMPORT_BY_NAME), "Name"));
                            tempPtr = Kernel32.GetProcAddress(handle, procName);
                            funcRef.SetStruct<IntPtr>(tempPtr);
                        }
                        if (funcRef.GetStruct<IntPtr>() == IntPtr.Zero) return false;
                    }
                }
            }
            memoryModulePTR.SetStruct<MEMORYMODULE>(memoryModule);
            return true;
        }

        private static void PerformBaseRelocation(IntPtr memoryModulePTR, long delta)
        {
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_DATA_DIRECTORY directory;
            if (Environment.Is64BitProcess) directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>().OptionalHeader.DataDirectory[5];
            else directory = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>().OptionalHeader.DataDirectory[5];
            if (directory.Size > 0)
            {
                IntPtr relocationPtr = memoryModule.codeBase.ToIntPtr().Offset(directory.VirtualAddress);
                IMAGE_BASE_RELOCATION relocation = relocationPtr.GetStruct<IMAGE_BASE_RELOCATION>();
                while (relocation.VirtualAddress > 0)
                {
                    IntPtr dest = memoryModule.codeBase.ToIntPtr().Offset(relocation.VirtualAddress);
                    IntPtr relInfo = relocationPtr.Offset(Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION)));
                    for (int i = 0; i < (relocation.SizeOfBlock - Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION))) / 2; i++, relInfo.Offset(2))
                    {
                        IntPtr patchAddrHL;
                        ushort uvalue = relInfo.GetStruct<ushort>();
                        int type, offset;
                        type = (int)(uvalue >> 12);
                        offset = (int)(uvalue & 0xfff);
                        switch (type)
                        {
                            case 0:
                                break;
                            case 3:
                                patchAddrHL = dest.Offset(offset);
                                if (Environment.Is64BitProcess) patchAddrHL.SetStruct<long>(patchAddrHL.GetStruct<long>() + (long)delta);
                                else patchAddrHL.SetStruct<int>(patchAddrHL.GetStruct<int>() + (int)delta);
                                break;
                            default:
                                break;
                        }
                    }
                    relocationPtr = relocationPtr.Offset(relocation.SizeOfBlock);
                    relocation = relocationPtr.GetStruct<IMAGE_BASE_RELOCATION>();
                }
            }
        }

        private static void CopySections(byte[] data, IMAGE_NT_HEADERS64 headerNT, IntPtr memoryModulePTR)
        {
            UIntPtr dest;
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_NT_HEADERS64 header = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS64>();
            long sectionPointer = memoryModule.headers.ToInt64() + Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader").ToInt64() + header.FileHeader.SizeOfOptionalHeader;
            for (int i = 0; i < header.FileHeader.NumberOfSections; i++, sectionPointer += Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)))
            {
                IMAGE_SECTION_HEADER section = new IntPtr(sectionPointer).GetStruct<IMAGE_SECTION_HEADER>();
                if (section.SizeOfRawData == 0)
                {
                    uint size = headerNT.OptionalHeader.SectionAlignment;
                    if (size > 0)
                    {
                        dest = Kernel32.VirtualAlloc(new UIntPtr(memoryModule.codeBase.ToUInt64() + section.VirtualAddress), size, AllocationType.COMMIT, MemoryProtection.READWRITE);
                        section.PhysicalAddress = dest.ToUInt32();
                        new IntPtr(sectionPointer).SetStruct<IMAGE_SECTION_HEADER>(section);
                        Memset(dest, (byte)0, (int)size);
                    }
                    continue;
                }
                dest = Kernel32.VirtualAlloc(new UIntPtr(memoryModule.codeBase.ToUInt64() + section.VirtualAddress), section.SizeOfRawData, AllocationType.COMMIT, MemoryProtection.READWRITE);
                Memcpy(dest, data, (int)section.PointerToRawData, (int)section.SizeOfRawData);
                section.PhysicalAddress = (uint)dest.ToUInt64();
                new IntPtr(sectionPointer).SetStruct<IMAGE_SECTION_HEADER>(section);
            }
        }

        private static void CopySections(byte[] data, IMAGE_NT_HEADERS32 headerNT, IntPtr memoryModulePTR)
        {
            UIntPtr dest;
            MEMORYMODULE memoryModule = memoryModulePTR.GetStruct<MEMORYMODULE>();
            IMAGE_NT_HEADERS32 header = memoryModule.headers.GetStruct<IMAGE_NT_HEADERS32>();
            int sectionPointer = memoryModule.headers.ToInt32() + Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS32), "OptionalHeader").ToInt32() + header.FileHeader.SizeOfOptionalHeader;
            for (int i = 0; i < header.FileHeader.NumberOfSections; i++, sectionPointer += Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)))
            {
                IMAGE_SECTION_HEADER section = new IntPtr(sectionPointer).GetStruct<IMAGE_SECTION_HEADER>();
                if (section.SizeOfRawData == 0)
                {
                    uint size = headerNT.OptionalHeader.SectionAlignment;
                    if (size > 0)
                    {
                        dest = Kernel32.VirtualAlloc(new UIntPtr(memoryModule.codeBase.ToUInt32() + section.VirtualAddress), size, AllocationType.COMMIT, MemoryProtection.READWRITE);
                        section.PhysicalAddress = dest.ToUInt32();
                        new IntPtr(sectionPointer).SetStruct<IMAGE_SECTION_HEADER>(section);
                        Memset(dest, (byte)0, (int)size);
                    }
                    continue;
                }
                dest = Kernel32.VirtualAlloc(new UIntPtr(memoryModule.codeBase.ToUInt32() + section.VirtualAddress), section.SizeOfRawData, AllocationType.COMMIT, MemoryProtection.READWRITE);
                Memcpy(dest, data, (int)section.PointerToRawData, (int)section.SizeOfRawData);
                section.PhysicalAddress = dest.ToUInt32();
                new IntPtr(sectionPointer).SetStruct<IMAGE_SECTION_HEADER>(section);
            }
        }

        private static void Memcpy(UIntPtr dist, byte[] data, int offset, int size)
        {
            if (Environment.Is64BitProcess) Marshal.Copy(data, offset, new IntPtr((long)dist.ToUInt64()), (int)size);
            else Marshal.Copy(data, offset, new IntPtr((int)dist.ToUInt32()), (int)size);
        }

        private static void Memcpy(UIntPtr dist, byte[] data, int size)
        {
            if (Environment.Is64BitProcess) Marshal.Copy(data, 0, new IntPtr((long)dist.ToUInt64()), (int)size);
            else Marshal.Copy(data, 0, new IntPtr((int)dist.ToUInt32()), (int)size);
        }

        private static void Memset(UIntPtr dist, byte value, int size)
        {
            Memcpy(dist, new byte[size].Select(x => x = value).ToArray(), size);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct Information
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ModuleName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string Version;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct Block
        {
            public int Number;
            public int Offset;
            public int Size;
        }

        [ServiceContract(Namespace = "{BAD4C45F-AD7C-4330-BE8B-5D5ADF694794}")]
        interface ILicence
        {
            [OperationContract(IsOneWay = false)]
            byte[] GetHeader(string computerName, string key, string id, string moduleName, string version);
        }

    }
}

