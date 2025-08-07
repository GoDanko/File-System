using System.IO;
using System.IO.MemoryMappedFiles;
using Debugging;

namespace FileSystem.HostFileAPI
{
    public class ManageFile
    {
        static internal string LocalAppPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        static internal string RuntimePath = AppDomain.CurrentDomain.BaseDirectory;

        private string FileName { get; set; }
        private string SystemManagedPath { get; set; }
        internal string GlobalFilePath { get; set; }
        internal string FileContent { get { return ReadFrom(); } }

        public ManageFile(string path, string targetName)
        {
            FileName = targetName;
            GlobalFilePath = Path.Combine(path, targetName);
            CreateFileAndPath(GlobalFilePath);
        }

        void CreateFileAndPath(string path)
        {
            string compositePath = "";
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    if (!Directory.Exists(compositePath)) Directory.CreateDirectory(compositePath);
                }
                compositePath += path[i];
            }
            if (!File.Exists(path)) {
                using (FileStream fs = File.Create(path))
                { }
            }
        }

        internal string LogTo(string content, bool removeExistingContent = false)
        {
            if (!removeExistingContent)
            {
                using (StreamWriter text = File.AppendText(this.GlobalFilePath))
                {
                    text.Write(content);
                }
            }
            else
            {
                File.WriteAllText(this.GlobalFilePath, content);
            }
            return content;
        }

        internal string ReadFrom()
        {
            using (StreamReader reader = new StreamReader(this.GlobalFilePath))
            {
                return reader.ReadToEnd();
            }
        }
    }

    class ManageBinary : ManageFile {
        internal int BinaryByteMaxSize = 0;

        // These methods were mostly AI assisted: I need to learn somehow how to IO files

        public ManageBinary(string path, string targetName) : base  (path, targetName) {

        }

        internal void OverwriteBinary(byte[] content)
        {
            using (FileStream writeAccess = new FileStream(GlobalFilePath, FileMode.Create))
            {
                writeAccess.Write(content);
            }
        }

        internal void AppendToBinary(byte[] content) {
            using (FileStream writeAccess = new FileStream(GlobalFilePath, FileMode.Append, FileAccess.Write))
            {
                writeAccess.Write(content);
            }
        }
        
        internal void AppendToBinary(Span<byte> content)
        {
            using (FileStream writeAccess = new FileStream(GlobalFilePath, FileMode.Append, FileAccess.Write))
            {
                writeAccess.Write(content);
            }
        }

        internal void WriteToBinary(byte[] content, int offset) {
            using (FileStream writeAccess = new FileStream(GlobalFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                writeAccess.Seek(offset, SeekOrigin.Begin);
                writeAccess.Write(content);
            }
        }

        internal void WriteToBinary(Span<byte> content, int offset) {
            using (FileStream writeAccess = new FileStream(GlobalFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                writeAccess.Seek(offset, SeekOrigin.Begin);
                writeAccess.Write(content);
            }
        }

        internal byte[] ReadBinaryChunk(int offset, int readSpan) {
            if (offset + readSpan > BinaryByteMaxSize) {
                if (offset < BinaryByteMaxSize) readSpan = BinaryByteMaxSize - offset;
                else return new byte[0];
            }
            byte[] readData = new byte[readSpan];
            using (MemoryMappedFile readAccess = MemoryMappedFile.CreateFromFile(GlobalFilePath, FileMode.Open))
            using (var entryPoint = readAccess.CreateViewAccessor(offset, readSpan)) {
                entryPoint.ReadArray(0, readData, 0, readSpan);
            }
            return readData;
        }
    }
}