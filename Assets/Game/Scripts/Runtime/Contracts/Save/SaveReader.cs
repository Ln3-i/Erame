using System;
using System.IO;
using System.Text;

namespace Game.Runtime
{
    public sealed class SaveReader : IDisposable{
        private readonly BinaryReader _reader;
        private readonly bool _ownsStream;

        public SaveReader(Stream stream, bool ownsStream = false){
            _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: !ownsStream);
            _ownsStream = ownsStream;
        }

        public int ReadInt() => _reader.ReadInt32();
        public float ReadFloat() => _reader.ReadSingle();
        public bool ReadBool() => _reader.ReadBoolean();

        public string ReadString(){
            int length = _reader.ReadInt32();
            if (length < 0) throw new InvalidDataException($"存档字符串长度非法: {length}");
            if (length == 0) return string.Empty;

            var bytes = _reader.ReadBytes(length);
            if (bytes.Length != length){
                throw new EndOfStreamException($"存档字符串被截断：期望 {length} 字节，实际 {bytes.Length} 字节。");
            }
            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] ReadRawBytes(int count){
            if (count < 0) throw new InvalidDataException($"存档字节长度非法: {count}");

            var bytes = _reader.ReadBytes(count);
            if (bytes.Length != count){
                throw new EndOfStreamException($"存档数据被截断：期望 {count} 字节，实际 {bytes.Length} 字节。");
            }
            return bytes;
        }

        public void Dispose(){
            if (_ownsStream) _reader.Dispose();
        }
    }
}
