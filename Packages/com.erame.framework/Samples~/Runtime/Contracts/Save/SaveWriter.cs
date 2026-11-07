using System;
using System.IO;
using System.Text;

namespace __NS__.Runtime{
    // 安全写入器：封装了 BinaryWriter
    public sealed class SaveWriter : IDisposable{
        private readonly BinaryWriter _writer;
        private readonly bool _ownsStream;

        public SaveWriter(Stream stream, bool ownsStream = false){
            _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: !ownsStream);
            _ownsStream = ownsStream;
        }

        public void Write(int value) => _writer.Write(value);
        public void Write(float value) => _writer.Write(value);
        public void Write(bool value) => _writer.Write(value);
        
        public void Write(string value){
            // 如果字符串为空，直接写长度 0
            if (string.IsNullOrEmpty(value)){
                _writer.Write(0);
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(value);
            _writer.Write(bytes.Length);
            _writer.Write(bytes);
        }

        public void WriteRawBytes(byte[] bytes){
            if (bytes != null && bytes.Length > 0){
                _writer.Write(bytes);
            }
        }

        public void Dispose(){
            if (_ownsStream) _writer.Dispose();
            else _writer.Flush();
        }
    }
}