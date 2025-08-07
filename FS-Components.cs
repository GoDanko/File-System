using FileSystem.MemoryManager;
using Debugging;

namespace FileSystem.FSComponents {
        class VirtualFile {
        internal string Name;
        internal (byte, byte) Address;


        public VirtualFile(string name) {
            Name = name;
            
        }
    }

    class VirtualDirectory : VirtualFile {
        Dictionary<byte, VirtualDirectory> ChildDirs = new Dictionary<byte, VirtualDirectory>();

        VirtualDirectory(string name) : base(name) {}
    }

    class TextFormat {
        internal static Dictionary<char, byte> WriteFormatTable = new Dictionary<char, byte> ();

        public static char UnpackChar(byte input) {
            if (ReadFormatTable.ContainsKey(input)) {
                return ReadFormatTable[input];
            } 
            return '\0';
        }

        public static byte PackChar(char input) {
            if (WriteFormatTable.ContainsKey(input)) {
                return WriteFormatTable[input];
            } 
            return 0;
        }

        internal static void CreateWriteTable() {
            if (WriteFormatTable.Count == 0) {
                WriteFormatTable = new Dictionary<char, byte> ();
                foreach (KeyValuePair<byte, char> entry in ReadFormatTable) {
                    WriteFormatTable.Add(entry.Value, entry.Key);
                }
            }
        }

        public static readonly Dictionary<byte, char> ReadFormatTable = new Dictionary<byte, char> () {
            {0x00, '\0'},
            {0x05, ' '},
            {0x09, 'q'},
            {0x0A, 'w'},
            {0x0B, 'e'},
            {0x0C, 'r'},
            {0x0D, 't'},
            {0x0E, 'y'},
            {0x0F, 'u'},
            {0x10, 'i'},
            {0x11, 'o'},
            {0x12, 'p'},
            {0x13, 'a'},
            {0x14, 's'},
            {0x15, 'd'},
            {0x16, 'f'},
            {0x17, 'g'},
            {0x18, 'h'},
            {0x19, 'j'},
            {0x1A, 'k'},
            {0x1B, 'l'},
            {0x1E, 'z'},
            {0x1F, 'x'},
            {0x20, 'c'},
            {0x21, 'v'},
            {0x22, 'b'},
            {0x23, 'n'},
            {0x24, 'm'},
            {0x25, '('},
            {0x26, ')'},
            {0x27, '['},
            {0x28, ']'},
            {0x29, '{'},
            {0x2A, '}'},
            {0x2B, '<'},
            {0x2C, '>'},
            {0x2D, '.'},
            {0x2E, ','},
            {0x2F, ':'},
            {0x30, ';'},
            {0x31, '_'},
            {0x32, '-'},
            {0x33, '+'},
            {0x34, '*'},
            {0x35, '/'},
            {0x36, '\\'},
            {0x37, '|'},
            {0x38, '?'},
            {0x39, '!'},
            {0x3A, '~'},
            {0x3B, '@'},
            {0x3C, '#'},
            {0x3D, '$'},
            {0x3E, '%'},
            {0x3F, '^'},
            {0x40, '&'},
            {0x41, '`'},
            {0x42, '\''},
            {0x43, '\"'},
            {0x55, '1'},
            {0x56, '2'},
            {0x57, '3'},
            {0x58, '4'},
            {0x59, '5'},
            {0x5A, '6'},
            {0x5B, '7'},
            {0x5C, '8'},
            {0x5D, '9'},
            {0x5E, '0'},
            {0x5F, 'Q'},
            {0x60, 'W'},
            {0x61, 'E'},
            {0x62, 'R'},
            {0x63, 'T'},
            {0x64, 'Y'},
            {0x65, 'U'},
            {0x66, 'I'},
            {0x67, 'O'},
            {0x68, 'P'},
            {0x69, 'A'},
            {0x6A, 'S'},
            {0x6B, 'D'},
            {0x6C, 'F'},
            {0x6E, 'G'},
            {0x6F, 'H'},
            {0x70, 'J'},
            {0x71, 'K'},
            {0x72, 'L'},
            {0x73, 'Z'},
            {0x74, 'X'},
            {0x75, 'C'},
            {0x76, 'V'},
            {0x77, 'B'},
            {0x78, 'N'},
            {0x79, 'M'}
        };
    }
}