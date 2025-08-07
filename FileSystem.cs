using System;
using FileSystem.HostFileAPI;
using FileSystem.FSComponents;
using Debugging;

namespace FileSystem.MemoryManager {

    class DiscAccess {
        internal ManageBinary BinaryFile;
        internal const ushort HeaderSize = 18;
        internal const ushort BracketMetadataSize = 22;
        internal const ushort AvailablePartitions = 65535;
        internal ushort ByteSizeOfPartition = 256;
        internal ushort AddressTableOffset;
        internal ushort TotalSizePerAddressEntry = 32;
        int TotalByteMemory;


        public DiscAccess(ushort partitionByteSize)
        {
            ByteSizeOfPartition = partitionByteSize;
        }

        internal static DiscAccess InitialiseStorage(ushort partitionByteSize = 256) {
            DiscAccess storage = new DiscAccess(partitionByteSize);
            if (File.Exists(Path.Combine(ManageBinary.LocalAppPath, "Schefflera", "Sys", "SysMemory.bin"))) {
                storage.BinaryFile = new ManageBinary(Path.Combine(ManageBinary.LocalAppPath, "Schefflera", "Sys"), "SysMemory.bin");
                if (EvaluateDisc(storage)) { return storage; }
                else { return storage.CreateNewDisc(); }
            }
            else { return storage.CreateNewDisc(); }
        }

        internal DiscAccess CreateNewDisc() {
            AddressTableOffset = (ushort)(AvailablePartitions / ((TotalSizePerAddressEntry - BracketMetadataSize) / 2));
            TotalByteMemory = (ByteSizeOfPartition + 2) * (AvailablePartitions + AddressTableOffset);
            BinaryFile = new ManageBinary(Path.Combine(ManageBinary.LocalAppPath, "Schefflera", "Sys"), "SysMemory.bin");
            BinaryFile.BinaryByteMaxSize = TotalByteMemory;

            BinaryFile.OverwriteBinary(new byte[0]);
            BinaryFile = PartitionVirtualDisc();
            BinaryFile.WriteToBinary(CreateDiscHeader(), 2);
            EventLogger.Report("DISC MANAGER: Created new disc");
            return this;
        }

        internal int GetOffset(ushort targetAddress, ushort addPartitionOffset = 0, bool targetMemoryTable = false)
        {
            if (addPartitionOffset >= ByteSizeOfPartition + 2) addPartitionOffset = (ushort)(ByteSizeOfPartition - 1);
            if (!targetMemoryTable) return (targetAddress + AddressTableOffset - 1) * (ByteSizeOfPartition + 2) + addPartitionOffset + 2;
            else return targetAddress * (ByteSizeOfPartition + 2) + addPartitionOffset + 2;
        }

        internal ManageBinary PartitionVirtualDisc()
        {
            int i = 1;
            Span<byte> addressPartition = new byte[ByteSizeOfPartition + 2];
            while (i < AddressTableOffset) {
                Span<byte> byteWiseSplitAddress = BinaryTooling.SplitIntoBytes(i);
                addressPartition[0] = byteWiseSplitAddress[0];
                addressPartition[1] = byteWiseSplitAddress[1];
                BinaryFile.AppendToBinary(addressPartition);
                i++;
            }
            i = 1;
            Span<byte> dataPartition = new byte[ByteSizeOfPartition + 2];
            while (i < AvailablePartitions) {
                Span<byte> byteWiseSplitAddress = BinaryTooling.SplitIntoBytes(i);
                dataPartition[0] = byteWiseSplitAddress[0];
                dataPartition[1] = byteWiseSplitAddress[1];
                BinaryFile.AppendToBinary(dataPartition);
                i++;
            }

            return BinaryFile;
        }

        byte[] CreateDiscHeader() 
        {
            Span<byte> result = new byte[HeaderSize];
            Span<byte> sizeOfPartition = BinaryTooling.SplitIntoBytes(ByteSizeOfPartition);
            result[0] = sizeOfPartition[0];
            result[1] = sizeOfPartition[1];
            Span<byte> amountOfPartitions = BinaryTooling.SplitIntoBytes(AvailablePartitions);
            result[2] = amountOfPartitions[0];
            result[3] = amountOfPartitions[1];
            Span<byte> FirstMassMemoryAddress = BinaryTooling.SplitIntoBytes(AddressTableOffset);
            result[4] = FirstMassMemoryAddress[0];
            result[5] = FirstMassMemoryAddress[1];
            Span<byte> sizePerAddress = BinaryTooling.SplitIntoBytes(TotalSizePerAddressEntry);
            result[6] = sizePerAddress[0];
            result[7] = sizePerAddress[1];
            Span<byte> totalByteMemory = BinaryTooling.SplitIntoBytes(TotalByteMemory);
            result[8] = totalByteMemory[0];
            result[9] = totalByteMemory[1];
            result[10] = totalByteMemory[2];
            result[11] = totalByteMemory[3];
            Span<byte> headerSize = BinaryTooling.SplitIntoBytes(HeaderSize);
            result[12] = headerSize[0];
            result[13] = headerSize[1];
            Span<byte> bracketFixedSize = BinaryTooling.SplitIntoBytes(BracketMetadataSize);
            result[14] = bracketFixedSize[0];
            result[15] = bracketFixedSize[1];
            return result.ToArray();
        }


        internal static byte?[] MakeByteArrayNullable(byte[] input, int[]? nullIndexes = null) {
            byte?[] output = new byte?[input.Length];

            if (nullIndexes != null) {
                for (int i = 0; i < nullIndexes.Length; i++) {
                    if (input.Length > nullIndexes[i]) output[i] = null;
                }
            }

            for (int i = 0; i < input.Length; i++) {
                output[i] = (output[i] != null) ? input[i] : null;
            }

            return output;
        }

        internal static bool EvaluateDisc(DiscAccess evaluatedDisc)
        {
            FileInfo windowsFileMetadata = new FileInfo(evaluatedDisc.BinaryFile.GlobalFilePath);
            
            // 1st stage: header evaluation
            if (windowsFileMetadata.Length < HeaderSize + 2) {
                EventLogger.Report("DISC EVALUATION: Failed. Disc Header is missing");
                return false;
            };
            evaluatedDisc.BinaryFile.BinaryByteMaxSize = HeaderSize + 2;
            byte[] discHeader = evaluatedDisc.BinaryFile.ReadBinaryChunk(0, HeaderSize + 2);
            if (BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[0], discHeader[1]}) != 1) {
                EventLogger.Report("DISC EVALUATION: Failed. Header address's missing");
                return false;
            };

            for (byte j = 3; j < HeaderSize; j += 2) {
                if (j == 11 || j == 13) continue;
                if (BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[j - 1], discHeader[j]}) == 0) {
                    EventLogger.Report("DISC EVALUATION: Failed. Embedded parameters are not set");
                    return false;
                }
            }

            ushort sizeOfPartition = (ushort) BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[2], discHeader[3]});
            ushort amountOfPartitions = (ushort) BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[4], discHeader[5]});
            ushort firstMassMemoryPartition = (ushort) BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[6], discHeader[7]});
            ushort sizePerAddress = (ushort) BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[8], discHeader[9]});
            int totalByteMemory = BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[10], discHeader[11], discHeader[12], discHeader[13]});
            ushort headerSize = (ushort) BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[14], discHeader[15]});
            ushort bracketFixedSize = (ushort) BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[16], discHeader[17]});

            if (totalByteMemory != (sizeOfPartition + 2) * (amountOfPartitions + firstMassMemoryPartition)) {
                EventLogger.Report("DISC EVALUATION: Failed. Embedded parameters format is incorrect");
                return false;
            };
            if (firstMassMemoryPartition != amountOfPartitions / ((sizePerAddress - bracketFixedSize) / 2)) {
                EventLogger.Report("DISC EVALUATION: Failed. Embedded parameters format is incorrect");
                return false;
            };

            evaluatedDisc.ByteSizeOfPartition = sizeOfPartition;
            evaluatedDisc.AddressTableOffset = firstMassMemoryPartition;
            evaluatedDisc.TotalSizePerAddressEntry = sizePerAddress;
            evaluatedDisc.TotalByteMemory = totalByteMemory;

            // 2nd stage: address indexing
            if (windowsFileMetadata.Length + (evaluatedDisc.ByteSizeOfPartition + 2) * 2 < totalByteMemory) {
                EventLogger.Report("DISC EVALUATION: Failed. Disc's size is too small");
                return false;
            };
            evaluatedDisc.BinaryFile.BinaryByteMaxSize = (int)windowsFileMetadata.Length;
            ushort offset = (ushort)(evaluatedDisc.ByteSizeOfPartition + 2);

            int CorruptedAddresses = 0;
            ushort nextExpectedAddress = 2;
            int i = offset;
            while (i < windowsFileMetadata.Length)
            {
                if (nextExpectedAddress != BinaryTooling.WeldBytesIntoInt(evaluatedDisc.BinaryFile.ReadBinaryChunk(i, 2))) CorruptedAddresses++;
                i += offset;
                if (++nextExpectedAddress == firstMassMemoryPartition) {
                    nextExpectedAddress = 1;
                    break;
                }
            }
            while (i < windowsFileMetadata.Length) {
                if (nextExpectedAddress != BinaryTooling.WeldBytesIntoInt(evaluatedDisc.BinaryFile.ReadBinaryChunk(i, 2))) CorruptedAddresses++;
                if (++nextExpectedAddress == amountOfPartitions) break;
                i += offset;
            }
            EventLogger.Report($"DISC EVALUATION: Success. Despite corrupted indexes {CorruptedAddresses}");
            return true;
        }

        internal static bool LogByteArrayToHexString(byte[] input, ushort top = 1, bool limitToLine = false) 
        {
            short printWidth = -2;
            for (short i = 0; i < input.Length; i++) {
                printWidth += 3;
                if (printWidth >= Console.BufferWidth - 1) {;
                    if (limitToLine) {
                        EventLogger.CursorReturnLog($"-", top, Console.BufferWidth - 1);
                        return false;
                    }
                    if (top++ >= Console.BufferHeight) {
                        EventLogger.CursorReturnLog($"-", top, Console.BufferWidth - 1);
                        return false;
                    }
                    EventLogger.CursorReturnLog($"-", top, Console.BufferWidth - 1, false);
                    printWidth = 1;

                }
                EventLogger.CursorReturnLog($"{input[i]:X2} ", top, printWidth, false);
            }
            EventLogger.CursorReturnLog($".", top, Console.BufferWidth - 1);
            return true;
        }
    }

    class FS {
        internal DiscAccess TargetDisc;

        public FS(DiscAccess targetDisc) {
            TargetDisc = targetDisc;
        }

        internal ushort[] WriteToDisc(ushort startingAddress, Span<byte> input, ushort addPartitionOffset = 0, bool overwrite = true, bool targetMemoryTable = false)
        {
            int addressesToOverwrite = input.Length / TargetDisc.ByteSizeOfPartition;
            ushort[] overwrittenAddresses = (input.Length < TargetDisc.ByteSizeOfPartition) ? new ushort[1] : new ushort[addressesToOverwrite];
            Span<byte> inputPointers = input;

            ushort? targetAddress = startingAddress;
            for (ushort i = 0; i < overwrittenAddresses.Length; i++) {
                targetAddress = overwrite ? targetAddress : FindPartiallyFreePartition((ushort)(startingAddress + i), (ushort)(TargetDisc.ByteSizeOfPartition - addPartitionOffset), targetMemoryTable).address;
                if (targetAddress.HasValue) {
                    overwrittenAddresses[i] = targetAddress.Value;
                    int writeLength = (i != overwrittenAddresses.Length - 1) ? TargetDisc.ByteSizeOfPartition : inputPointers.Length % TargetDisc.ByteSizeOfPartition;
                    if (writeLength == 0) writeLength = TargetDisc.ByteSizeOfPartition;
                    TargetDisc.BinaryFile.WriteToBinary(inputPointers.Slice(i * TargetDisc.ByteSizeOfPartition, writeLength), TargetDisc.GetOffset(targetAddress.Value, addPartitionOffset, targetMemoryTable));
                    addPartitionOffset = 0;
                    targetAddress++;
                } else {
                    overwrittenAddresses[i] = 0;
                    return overwrittenAddresses;
                }
            }
            return overwrittenAddresses;
        }

        internal byte[] ReadFromDisc(Span<ushort> addresses, ushort readSpan = 0, bool targetMemoryTable = false)
        {
            if (readSpan == 0) readSpan = TargetDisc.ByteSizeOfPartition;
            byte[] output = new byte[addresses.Length * TargetDisc.ByteSizeOfPartition];
            Span<byte> outputPointers = output;
            for (ushort i = 0; i < addresses.Length; i++) {
                ReadOnlySpan<byte> singlePartition = TargetDisc.BinaryFile.ReadBinaryChunk(TargetDisc.GetOffset(addresses[i], default, targetMemoryTable), readSpan);
                singlePartition.CopyTo(outputPointers.Slice(i * TargetDisc.ByteSizeOfPartition, TargetDisc.ByteSizeOfPartition));
            }
            return output;
        }

        internal byte[] ReadFromDisc(ushort targetAddress, ushort addPartitionOffset = 0, ushort readSpan = 0, bool targetMemoryTable = false)
        {
            if (readSpan == 0) readSpan = TargetDisc.ByteSizeOfPartition;
            if (TargetDisc.ByteSizeOfPartition < addPartitionOffset + readSpan) addPartitionOffset = 0;
            return TargetDisc.BinaryFile.ReadBinaryChunk(TargetDisc.GetOffset(targetAddress, addPartitionOffset, targetMemoryTable), readSpan);
        }

        internal ushort? FindNextFreePartition(ushort startingAddress, bool targetMemoryTable = false)
        {
            ushort freeAddress = startingAddress;
            while (true) {
                if (!CheckIfPartitionIsOccupied(freeAddress, default, targetMemoryTable)) return freeAddress;
                else {
                    if (!targetMemoryTable) {
                        if (freeAddress == startingAddress - 1) return null;
                        if (freeAddress < DiscAccess.AvailablePartitions) freeAddress++;
                        else freeAddress = (ushort)(TargetDisc.AddressTableOffset + 1);
                    } else {
                        if (freeAddress == startingAddress - 1) return null;
                        if (freeAddress < TargetDisc.AddressTableOffset) freeAddress++;
                        else freeAddress = 1;
                    }
                }
            }
        }

        internal bool CheckIfPartitionIsOccupied(ushort targetAddress, ushort addPartitionOffset = 0, bool targetMemoryTable = false)
        {
            byte[] partitionData = ReadFromDisc(targetAddress, default, default, targetMemoryTable);
            Console.Clear();
            int partitionSum = 0;
            for (ushort i = 0; i < partitionData.Length; i++) {
                partitionSum += partitionData[i];
                if (partitionSum > 0) return true;
            }
            return false;
        }

        internal (ushort? address, ushort? remainingSpace) FindPartiallyFreePartition(ushort startingAddress, ushort minimalRequiredSpace, bool targetMemoryTable = false)
        {
            ushort semiFreeAddress = startingAddress;
            do {
                ushort remainingSpaceInPartition = CheckRemainingSpaceInPartition(semiFreeAddress, targetMemoryTable);
                if (remainingSpaceInPartition >= minimalRequiredSpace) return (semiFreeAddress, remainingSpaceInPartition);
                else {
                    if (!targetMemoryTable) {
                        if (semiFreeAddress < DiscAccess.AvailablePartitions) semiFreeAddress++;
                        else semiFreeAddress = (ushort)(TargetDisc.AddressTableOffset + 1);
                    } else {
                        if (semiFreeAddress < TargetDisc.AddressTableOffset) semiFreeAddress++;
                        else semiFreeAddress = 1;
                    }
                }
            } while (semiFreeAddress != startingAddress);
            return (null, null);
        }
        
        internal ushort CheckRemainingSpaceInPartition(ushort targetAddress, bool targetMemoryTable = false)
        {
            Span<byte> partitionData = ReadFromDisc(targetAddress, default, default, targetMemoryTable);
            ushort lastOccupiedByte = 0;
            for (ushort i = 0; i < partitionData.Length; i++) {
                if (partitionData[i] != 0) lastOccupiedByte = (ushort)(i + 1);
            }
            return (ushort)(TargetDisc.ByteSizeOfPartition - lastOccupiedByte);
        }

        internal int[] FindMatchingPatterns(byte?[] targetPattern, bool targetMemoryTable = false) {
            List<int> result = new List<int> ();
            ushort maxIterations = (!targetMemoryTable) ? DiscAccess.AvailablePartitions : TargetDisc.AddressTableOffset;

            for (ushort targetedPartition = 1; targetedPartition < maxIterations; targetedPartition++) {
                int offset = TargetDisc.GetOffset(targetedPartition, default, targetMemoryTable);
                Span<byte> binaryChunk = TargetDisc.BinaryFile.ReadBinaryChunk(offset, TargetDisc.ByteSizeOfPartition);

                ushort matchingLength = 0;
                for (ushort i = 0; i < binaryChunk.Length; i++) {
                    if (binaryChunk[i] != targetPattern[matchingLength] && targetPattern[matchingLength].HasValue) {
                        matchingLength = 0;
                        continue;
                    } else {
                        matchingLength++;
                    }

                    if (matchingLength == targetPattern.Length) {
                        result.Add(offset + (i - targetPattern.Length));
                        matchingLength = 0;
                    }
                }
                Console.Clear();
            }
            return result.ToArray();
        }

        internal (ushort[] adresses, ushort[] addedOffset) RegisterAddresses(Span<ushort> addressesToRegister, string upTo15CharID = "") {

            ushort addressAmountPerBracket = (ushort)((TargetDisc.TotalSizePerAddressEntry - DiscAccess.BracketMetadataSize) / 2);
            ushort bracketsNeeded = (ushort)(addressesToRegister.Length / addressAmountPerBracket + 1);

            // if (upTo15CharID == "") upTo15CharID += AddressEntry.BracketCounter;
            // Check If the ID exists already;

            Span<byte[]> brackets = new byte[bracketsNeeded][];
            ushort ThisManyMoreNeeded = (ushort)(bracketsNeeded - 1);

            for (ushort i = 0; i < brackets.Length; i++) {
                Span<byte> byteNotedAddresses = ((i + 1) * addressAmountPerBracket <= addressesToRegister.Length) ? stackalloc byte[addressAmountPerBracket * 2] : stackalloc byte[(addressesToRegister.Length % addressAmountPerBracket) * 2];
                for (ushort j = 0; j < byteNotedAddresses.Length / 2; j++) {
                    Span<byte> individualAddress = BinaryTooling.SplitIntoBytes(addressesToRegister[i * addressAmountPerBracket + j]);
                    individualAddress.Slice(0, 2).CopyTo(byteNotedAddresses.Slice(j * 2, 2));
                }
                brackets[i] = AddressEntry.BuildBracket(byteNotedAddresses.ToArray(), (byte)(addressAmountPerBracket * 2), upTo15CharID, (byte)ThisManyMoreNeeded);
                ThisManyMoreNeeded--;
            }

            Span<ushort> overwrittenAddresses = new ushort[bracketsNeeded];
            Span<ushort> addressOffset = new ushort[bracketsNeeded];

            ushort bracketsPushed = 0;
            while (bracketsPushed < bracketsNeeded) {
                (ushort? address, ushort? remainingSpace) inputToAddress = FindPartiallyFreePartition(1, TargetDisc.TotalSizePerAddressEntry, true);
                if (!inputToAddress.address.HasValue || !inputToAddress.remainingSpace.HasValue) return (new ushort[0], new ushort[0]);

                int bracketWiseSpace = (inputToAddress.remainingSpace.Value / TargetDisc.TotalSizePerAddressEntry < bracketsNeeded - bracketsPushed) ? inputToAddress.remainingSpace.Value / TargetDisc.TotalSizePerAddressEntry : bracketsNeeded - bracketsPushed;
                Span<byte> inputChunk = new byte[bracketWiseSpace * TargetDisc.TotalSizePerAddressEntry];

                for (byte y = 0; y < bracketWiseSpace; y++)
                {
                    brackets[y + bracketsPushed].CopyTo(inputChunk.Slice(y * TargetDisc.TotalSizePerAddressEntry, TargetDisc.TotalSizePerAddressEntry));
                    overwrittenAddresses[y + bracketsPushed] = inputToAddress.address.Value;
                    addressOffset[y + bracketsPushed] = (ushort)(TargetDisc.ByteSizeOfPartition - inputToAddress.remainingSpace.Value);
                }
                bracketsPushed += (ushort)(inputChunk.Length / TargetDisc.TotalSizePerAddressEntry);
                WriteToDisc(inputToAddress.address.Value, inputChunk, (ushort)(TargetDisc.ByteSizeOfPartition - inputToAddress.remainingSpace.Value), default, true);
            }

            return (overwrittenAddresses.ToArray(), addressOffset.ToArray());
        }
    }

    class AddressEntry {
        internal int ID;
        internal byte Size;
        internal string Tag = "";
        internal byte[] Content;

        [Flags] enum DataType : byte {
            undefined = 0,  // 0b0000
            chars = 1 << 0, // 0b0001
            int8 = 1 << 1,  // 0b0010
            int16 = 1 << 2, // 0b0100
            int32 = 1 << 3, // 0b1000
        }

        internal static byte[] BuildBracket(byte[] content, byte contentSize, string upTo15CharID, byte linkedBrackets = 0) {
            byte tagSize = (byte)upTo15CharID.Length;

            byte[] bracket = new byte[contentSize + DiscAccess.BracketMetadataSize];
            bracket[0] = 0b1010_0000;

            byte[] ProcessID = BinaryTooling.SplitIntoBytes(new Random().Next(65536, 2147483646));
            bracket[1] = ProcessID[0];
            bracket[2] = ProcessID[1];
            bracket[3] = ProcessID[2];
            bracket[4] = ProcessID[3];
            bracket[5] = contentSize;

            bracket[bracket.Length - 1] = (byte)((linkedBrackets < 15) ? linkedBrackets : 15);
            bracket[bracket.Length - 1] |= 0b1010_0000;

            for (byte i = 0; i < 15; i++) {
                if (i < tagSize) bracket[i + 6] = TextFormat.PackChar(upTo15CharID[i]);
                else bracket[i + 6] = 0x05;
            }

            for (ushort i = 0; i < content.Length; i++) {
                bracket[i + 21] = content[i];
                if (i >= contentSize) break;
            }

            return bracket;
        }

        internal static AddressEntry? ParseBracket(byte[] content) {
            AddressEntry bracket = new AddressEntry();

            Span<byte> firstByte = stackalloc byte[2];
            firstByte = BinaryTooling.BitSplitter(content[0], 2);
            if (firstByte[1] != 0b0000_1010) { return null; }

            bracket.ID = BinaryTooling.WeldBytesIntoInt(new byte[] {content[1], content[2], content[3], content[4]});
            bracket.Size = content[3];

            bracket.Tag = "-";
            for (int i = 0; i <= firstByte[0]; i++){
                bracket.Tag += TextFormat.UnpackChar(content[i + 6]);
            }

            bracket.Content = new byte[bracket.Size];
            for (int i = 0; i < bracket.Size; i++){
                bracket.Content[i] = content[i + 21];
            }

            return bracket;
        }
    }

    static class BinaryTooling
    {
        internal static int WeldBytesIntoInt(byte[] byteArray)
        {
            sbyte Truncate;
            int result = 0;
            if (byteArray.Length > 4) Truncate = (sbyte)(byteArray.Length - 4);
            else Truncate = 0;

            for (sbyte i = (sbyte)(byteArray.Length - 1); i >= Truncate; i--)
            {
                result += byteArray[i] << (8 * i);
            }

            return result;
        }

        internal static byte[] SplitIntoBytes(int input)
        {
            byte[] result = new byte[4];
            result[0] = (byte)(input >> 0);
            result[1] = (byte)(input >> 8);
            result[2] = (byte)(input >> 16);
            result[3] = (byte)(input >> 24);
            return result;
        }

        internal static byte[] BitSplitter(byte input, byte splitInto = 8)
        {
            splitInto = (byte)((8 % splitInto == 0 && splitInto != 0) ? splitInto : 8);
            Span<byte> output = new byte[splitInto];
            byte bitWiseLength = (byte)(8 / splitInto);
            for (byte i = 0; i < splitInto; i++) {
                input = (i != 0) ? (byte)(input >> bitWiseLength) : input;
                for (sbyte j = (sbyte)(bitWiseLength - 1); j >= 0; j--){
                    output[i] <<= 1;
                    output[i] = (byte)(output[i] | ((input >> j) & 1));
                }
            }
            return output.ToArray();
        }
    }
}