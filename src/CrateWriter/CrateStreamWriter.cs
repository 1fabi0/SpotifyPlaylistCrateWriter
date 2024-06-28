using System.Buffers.Binary;
using System.Text;

namespace CrateWriter
{
    public class CrateStreamWriter
    {
        private static readonly byte[] _otrkTag = Encoding.ASCII.GetBytes("otrk");
        private static readonly byte[] _ptrkTag = Encoding.ASCII.GetBytes("ptrk");
        private static readonly byte[] _versionTag = Encoding.ASCII.GetBytes("vrsn");
        private static readonly byte[] _version = Encoding.BigEndianUnicode.GetBytes("1.0/Serato ScratchLive Crate");
        private static readonly byte[] _versionLength = GetBigEndianLength(_version.Length);

        private readonly Stream _stream;
        internal CrateStreamWriter(Stream stream)
        {
            _stream = stream;
        }

        private void WriteVersion()
        {
            _stream.Write(_versionTag);
            _stream.Write(_versionLength);
            _stream.Write(_version);
        }

        private static byte[] GetBigEndianLength(int length)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, length);
            return bytes;
        }

        public void WriteTrack(string trackPath)
        {
            byte[] ptrk = Encoding.BigEndianUnicode.GetBytes(trackPath);
            byte[] ptrkLength = GetBigEndianLength(ptrk.Length);
            byte[] otrkLength = GetBigEndianLength(_ptrkTag.Length + ptrkLength.Length + ptrk.Length);
            _stream.Write(_otrkTag);
            _stream.Write(otrkLength);
            _stream.Write(_ptrkTag);
            _stream.Write(ptrkLength);
            _stream.Write(ptrk);
        }

        public static CrateStreamWriter Create(Stream stream)
        {
            if (stream.Length != 0)
                throw new Exception("Expected empty stream");
            if (stream.CanWrite == false)
                throw new Exception("Expected to write to stream");
            if (stream.CanSeek == false)
                throw new Exception("Expected to seek to start of stream");
            stream.Seek(0, SeekOrigin.Begin);
            var sw = new CrateStreamWriter(stream);
            sw.WriteVersion();
            return sw;
        }


    }
}
