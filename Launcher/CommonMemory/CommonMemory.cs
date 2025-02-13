using System;
using DataExchange;
using DataExchange.Field;

namespace Launcher.CommonMemory
{
  public sealed class CommonMemory : MarshalByRefObject, ICommonMemoryFullControl, ICommonMemoryManager
    {
        Client client;

        public CommonMemory()
        {
            client = new Client();
        }

        public unsafe void GetData(int address, byte* data)
        {
            client.GetData(address, data);
        }

        public int GetDataSize(int address)
        {
            return client.GetDataSize(address);
        }

        public int Initialize(string address, int typeField, int sizeField, bool isArray)
        {
            return client.Initialize(address, (TypeField)typeField, sizeField, isArray);
        }

        public void Initialize(object obj)
        {
            client.Initialize(obj);
        }

        public unsafe void SetData(int address, byte* data, int size)
        {
            client.SetData(address, data, size);
        }

        public void Connected()
        {
            client.Connected(); 
        }

        public void Disconnected()
        {
            client.Disconnected();
        }

        public void Load()
        {
            client.Load();
        }

        public void Save()
        {
            client.Save();
        }

        public void SetLog(ILog log)
        {
            client.SetLog(log);
        }

        public byte[] GetData(int address)
        {
            return client.GetData(address);
        }

        public void SetData(int address, byte[] data)
        {
            client.SetData(address, data);
        }

        public void Desynchronize()
        {
            client.Desynchronize();
        }

        public void Synchronize()
        {
            client.Synchronize();
        }

        public bool Lock(int address)
        {
            return client.Lock(address);
        }

        public bool IsSynchronize { get { return client.IsSynchronize; } }
    }
}
