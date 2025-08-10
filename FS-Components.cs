using FileSystem.MemoryManager;
using Debugging;

namespace FileSystem.FSComponents {

    

    class AddressEntry {
        internal int ID;
        internal byte Size;
        internal byte[] Content;

        [Flags] enum DataType : byte {  // keep it 4 bits
            undefined = 0,  // 0b0000 | 0
            chars = 1 << 0, // 0b0001 | 1
            int8 = 1 << 1,  // 0b0010 | 2
            int16 = 1 << 2, // 0b0100 | 4
            int32 = 1 << 3, // 0b1000 | 8
        }

        internal static byte[] OrderBrackets(Span<byte> content, byte dataType = 0, ushort fixedSize = 0, bool keepID = false) {
            fixedSize = fixedSize < 32 ? (ushort)32 : fixedSize;
            ushort contentPerBracket = keepID ? (ushort)(fixedSize - DiscAccess.BracketMetaSizeNoID + 4) : (ushort)(fixedSize - DiscAccess.BracketMetaSizeNoID);
            ushort bracketsNeeded = (ushort)(content.Length / contentPerBracket + 1);

            Span<byte> result = new byte[bracketsNeeded * fixedSize];
            for (int i = 0; i < bracketsNeeded; i++) {
                Span<byte> singlePortion = ((i + 1) * contentPerBracket <= content.Length) ? stackalloc byte[contentPerBracket] : stackalloc byte[content.Length % contentPerBracket];
                content.Slice(i * contentPerBracket, singlePortion.Length).CopyTo(singlePortion);
                BuildBracket(singlePortion.ToArray(), dataType, (byte)(bracketsNeeded - i), fixedSize, keepID).CopyTo(result.Slice(i * fixedSize, fixedSize));
            }
            return result.ToArray();
        }

        internal static byte[] BuildBracket(byte[] content, byte dataType = 0, byte linkedBrackets = 0, ushort fixedSize = 0, bool keepID = false) {
            fixedSize = fixedSize < 32 ? (ushort)32 : fixedSize;
            byte[] bracket = new byte[fixedSize];

            ushort trackByte = 0;
            bracket[trackByte] = keepID ? (byte)0b1011_0000 : (byte)0b1010_0000;
            bracket[trackByte++] |= dataType;

            if (keepID) {
                // Create a lookup that checks if the ID is already occupied,
                // by leveraging the shared firstByte mapping structure within
                // the disc, and if it doesn't then register it as occupied
                Span<byte> ProcessID = stackalloc byte[4];
                ProcessID = BinaryTooling.SplitIntoBytes(new Random().Next(65536, 2147483646));
                bracket[trackByte++] = ProcessID[0];
                bracket[trackByte++] = ProcessID[1];
                bracket[trackByte++] = ProcessID[2];
                bracket[trackByte++] = ProcessID[3];
            }


            Span<byte> intAsByte = BinaryTooling.SplitIntoBytes(fixedSize);
            bracket[trackByte++] = intAsByte[0];
            bracket[trackByte++] = intAsByte[1];

            ushort firstContentByte = trackByte;
            EventLogger.CursorReturnLog($"start at: {firstContentByte}", 15, 1, false);
            while (trackByte < bracket.Length - 1) {
                bracket[trackByte] = (byte)((trackByte < trackByte - firstContentByte) ? content[trackByte - firstContentByte] : 0);
                EventLogger.CursorReturnLog($"fail at: {trackByte}", 16, 1, false);
                trackByte++;
            }
            EventLogger.CursorReturnLog($"correction: it didn't fail", 17, 1, false);

            bracket[bracket.Length - 1] = 0b1110_0000;
            bracket[bracket.Length - 1] |= (byte)((linkedBrackets < 15) ? linkedBrackets : 15);

            return bracket;
        }



        internal static AddressEntry? ParseBracket(byte[] content) {
            AddressEntry bracket = new AddressEntry();

            Span<byte> firstByte = stackalloc byte[2];
            firstByte = BinaryTooling.BitSplitter(content[0], 2);
            if (firstByte[1] != 0b0000_1010) { return null; }

            bracket.ID = BinaryTooling.WeldBytesIntoInt(new byte[] {content[1], content[2], content[3], content[4]});
            bracket.Size = content[3];

            bracket.Content = new byte[bracket.Size];
            for (int i = 0; i < bracket.Size; i++){
                bracket.Content[i] = content[i + 21];
            }

            return bracket;
        }
    }
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
}