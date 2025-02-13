using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Management;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using System.Security.Cryptography;

namespace Launcher
{
    /*
    class Loader
    {
        static Loader instance = new Loader();

        public static Loader Instance { get { return instance; } }

        private readonly object locker = new object();

        private Loader()
        {

        }

        public Assembly GetAssembly(string filename)
        {
            Monitor.Enter(locker);
            try
            {
                byte[] fileContent=null;
                try
                {
                    fileContent = LoadFile(filename);
                }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(LogLevel.Basic, ex, string.Format(Local.Message.LoaderMessage0_P2, filename)); return null; }
                Information info = GetInformation(fileContent);
                string key = GetKey();
                if (key == null) return null;
                string serial = GetSerial();
                ILicence contract = GetContract(key);
                return GetAssembly(GetContent(fileContent), contract.GetHeader(Environment.MachineName, key, serial, info.ModuleName, info.Version));
            }
            catch { LogManager.Instance.Launcher.Exception(LogLevel.Basic, string.Format(Local.Message.LoaderMessage0_P1, filename)); return null; }
            finally { Monitor.Exit(locker); }
        }

        private Assembly GetAssembly(byte[] content, byte[] binHeader)
        {
            List<Block> header = new List<Block>(GetHeader(binHeader));
            header.Sort(delegate(Block x, Block y)
            {
                if (x.Number > y.Number) return 1;
                else if (x.Number < y.Number) return -1;
                return 0;
            });
            byte[] rawAssembly = Decrypt(header.ToArray(), content);
            return AppDomain.CurrentDomain.Load(rawAssembly);
        }

        private byte[] LoadFile(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private Information GetInformation(byte[] fileContent)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(Information))];
            Buffer.BlockCopy(fileContent, 0, buffer, 0, buffer.Length);
            return BytesToStructure<Information>(buffer);
        }

        private string GetKey()
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

        private string GetSerial()
        {
            ManagementObject obj = new ManagementObject(string.Format("win32_logicaldisk.deviceid=\"{0}:\"", Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1)));
            obj.Get();
            return obj["VolumeSerialNumber"].ToString();
        }

        private ILicence GetContract(string key)
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

        byte[] GetContent(byte[] fileContent)
        {
            int size = Marshal.SizeOf(typeof(Information));
            byte[] content = new byte[fileContent.Length - size];
            Buffer.BlockCopy(fileContent, size, content, 0, content.Length);
            return content;
        }

        private Block[] GetHeader(byte[] buffer)
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

        private byte[] Decrypt(Block[] header, byte[] data)
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

        private T BytesToStructure<T>(byte[] buffer) where T : struct
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
    
    }*/
}
