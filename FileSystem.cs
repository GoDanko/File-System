using System;
using FileSystem.HostFileAPI;
using FileSystem.FSComponents;
using Debugging;
using System.Text;

namespace FileSystem.MemoryManager {

    class DiscAccess {
        internal const ushort BracketMetaSizeNoID = 5;      // SET here (const)
        internal ushort ByteSizeOfPartition;                // SET at line 24
        internal int DataPartitions;                        // SET at line 25
        internal ManageBinary BinaryFile;                   // SET at line 31
        internal ushort MinBracketSize = 32;                // SET at line 39
        internal int HeaderOffset;                          // SET at line 65
        internal int AddressTableOffset;                    // SET at line 77
        // Assumes -4 optional bytes containing the ID
        int TotalByteMemory;                                // SET at line 88
        internal ushort HeaderSize = 18;                    // SET at line 124


        public DiscAccess(ushort partitionByteSize, int memoryPartitions)
        {
            ByteSizeOfPartition = partitionByteSize;
            DataPartitions = memoryPartitions;
        }

        internal static DiscAccess InitialiseStorage(ushort partitionByteSize = 256, int memoryPartitions = 65535) {
            DiscAccess storage = new DiscAccess(partitionByteSize, memoryPartitions);
            if (File.Exists(Path.Combine(ManageBinary.LocalAppPath, "1A.FS-TestingGround", "FS.bin"))) {
                storage.BinaryFile = new ManageBinary(Path.Combine(ManageBinary.LocalAppPath, "1A.FS-TestingGround"), "FS.bin");
                if (EvaluateDisc(storage)) { return storage; }
                else { return storage.CreateNewDisc(); }
            }
            else { return storage.CreateNewDisc(); }
        }

        internal DiscAccess CreateNewDisc() {
            MinBracketSize = (ByteSizeOfPartition < 256) ? (ushort)32 : (ushort)(ByteSizeOfPartition / 8);
            BinaryFile = new ManageBinary(Path.Combine(ManageBinary.LocalAppPath, "1A.FS-TestingGround"), "FS.bin");
            BinaryFile.OverwriteBinary(new byte[0]);
            BinaryFile = PartitionVirtualDisc();
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
            BinaryFile.AppendToBinary(CreateDiscHeader());

            int i = 0;
            Span<byte> MetaDataIDMap = new byte[257];
            while (i < 255) {
                MetaDataIDMap[0] = BinaryTooling.SplitIntoBytes(i)[0];
                BinaryFile.AppendToBinary(MetaDataIDMap);
                i++;
            }
            HeaderOffset = HeaderSize + i * MetaDataIDMap.Length;

            int AddressSectionPartitions = (ushort)(DataPartitions / ((MinBracketSize - BracketMetaSizeNoID + 4) / 2));
            i = 1;
            Span<byte> addressPartition = new byte[ByteSizeOfPartition + 2];
            while (i < AddressSectionPartitions) {
                Span<byte> byteWiseSplitAddress = BinaryTooling.SplitIntoBytes(i);
                addressPartition[0] = byteWiseSplitAddress[0];
                addressPartition[1] = byteWiseSplitAddress[1];
                BinaryFile.AppendToBinary(addressPartition);
                i++;
            }
            AddressTableOffset = HeaderOffset + i * (ByteSizeOfPartition + 2);

            i = 1;
            Span<byte> dataPartition = new byte[ByteSizeOfPartition + 2];
            while (i < DataPartitions) {
                Span<byte> byteWiseSplitAddress = BinaryTooling.SplitIntoBytes(i);
                dataPartition[0] = byteWiseSplitAddress[0];
                dataPartition[1] = byteWiseSplitAddress[1];
                BinaryFile.AppendToBinary(dataPartition);
                i++;
            }
            TotalByteMemory = AddressTableOffset + i * (ByteSizeOfPartition + 2);
            BinaryFile.BinaryByteMaxSize = TotalByteMemory;

            return BinaryFile;
        }

        byte[] CreateDiscHeader()
        {
            byte trackByte = 0;

            Span<byte> result = new byte[HeaderSize];
            Span<byte> sizeOfPartition = BinaryTooling.SplitIntoBytes(ByteSizeOfPartition);
            result[trackByte++] = sizeOfPartition[0];
            result[trackByte++] = sizeOfPartition[1];
            Span<byte> amountOfPartitions = BinaryTooling.SplitIntoBytes(DataPartitions);
            result[trackByte++] = amountOfPartitions[0];
            result[trackByte++] = amountOfPartitions[1];
            result[trackByte++] = amountOfPartitions[2];
            result[trackByte++] = amountOfPartitions[3];
            Span<byte> FirstMassMemoryAddress = BinaryTooling.SplitIntoBytes(AddressTableOffset);
            result[trackByte++] = FirstMassMemoryAddress[0];
            result[trackByte++] = FirstMassMemoryAddress[1];
            Span<byte> sizePerAddress = BinaryTooling.SplitIntoBytes(MinBracketSize);
            result[trackByte++] = sizePerAddress[0];
            result[trackByte++] = sizePerAddress[1];
            Span<byte> totalByteMemory = BinaryTooling.SplitIntoBytes(TotalByteMemory);
            result[trackByte++] = totalByteMemory[0];
            result[trackByte++] = totalByteMemory[1];
            result[trackByte++] = totalByteMemory[2];
            result[trackByte++] = totalByteMemory[3];
            Span<byte> headerSize = BinaryTooling.SplitIntoBytes(HeaderSize);
            result[trackByte++] = headerSize[0];
            result[trackByte++] = headerSize[1];
            Span<byte> bracketFixedSize = BinaryTooling.SplitIntoBytes(BracketMetaSizeNoID);
            result[trackByte++] = bracketFixedSize[0];
            result[trackByte++] = bracketFixedSize[1];
            HeaderSize = trackByte;
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
            if (windowsFileMetadata.Length < evaluatedDisc.HeaderSize + 2) {
                EventLogger.Report("DISC EVALUATION: Failed. Disc Header is missing");
                return false;
            };
            evaluatedDisc.BinaryFile.BinaryByteMaxSize = evaluatedDisc.HeaderSize + 2;
            byte[] discHeader = evaluatedDisc.BinaryFile.ReadBinaryChunk(0, evaluatedDisc.HeaderSize + 2);
            if (BinaryTooling.WeldBytesIntoInt(new byte [] {discHeader[0], discHeader[1]}) != 1) {
                EventLogger.Report("DISC EVALUATION: Failed. Header address's missing");
                return false;
            };

            for (byte j = 3; j < evaluatedDisc.HeaderSize; j += 2) {
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
            evaluatedDisc.MinBracketSize = sizePerAddress;
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
        internal DiscAccess targetDisc;

        public FS(DiscAccess discForFS) {
            targetDisc = discForFS;
        }

        internal ushort[] WriteToDisc(ushort startingAddress, Span<byte> input, ushort addPartitionOffset = 0, bool overwrite = true, bool targetMemoryTable = false)
        {
            int addressesToOverwrite = input.Length / targetDisc.ByteSizeOfPartition;
            ushort[] overwrittenAddresses = (input.Length < targetDisc.ByteSizeOfPartition) ? new ushort[1] : new ushort[addressesToOverwrite];
            Span<byte> inputPointers = input;

            ushort? targetAddress = startingAddress;
            for (ushort i = 0; i < overwrittenAddresses.Length; i++) {
                targetAddress = overwrite ? targetAddress : FindPartiallyFreePartition((ushort)(startingAddress + i), (ushort)(targetDisc.ByteSizeOfPartition - addPartitionOffset), targetMemoryTable).address;
                if (targetAddress.HasValue) {
                    overwrittenAddresses[i] = targetAddress.Value;
                    int writeLength = (i != overwrittenAddresses.Length - 1) ? targetDisc.ByteSizeOfPartition : inputPointers.Length % targetDisc.ByteSizeOfPartition;
                    if (writeLength == 0) writeLength = targetDisc.ByteSizeOfPartition;
                    targetDisc.BinaryFile.WriteToBinary(inputPointers.Slice(i * targetDisc.ByteSizeOfPartition, writeLength), targetDisc.GetOffset(targetAddress.Value, addPartitionOffset, targetMemoryTable));
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
            if (readSpan == 0) readSpan = targetDisc.ByteSizeOfPartition;
            byte[] output = new byte[addresses.Length * targetDisc.ByteSizeOfPartition];
            Span<byte> outputPointers = output;
            for (ushort i = 0; i < addresses.Length; i++) {
                ReadOnlySpan<byte> singlePartition = targetDisc.BinaryFile.ReadBinaryChunk(targetDisc.GetOffset(addresses[i], default, targetMemoryTable), readSpan);
                singlePartition.CopyTo(outputPointers.Slice(i * targetDisc.ByteSizeOfPartition, targetDisc.ByteSizeOfPartition));
            }
            return output;
        }

        internal byte[] ReadFromDisc(ushort targetAddress, ushort addPartitionOffset = 0, ushort readSpan = 0, bool targetMemoryTable = false)
        {
            if (readSpan == 0) readSpan = targetDisc.ByteSizeOfPartition;
            if (targetDisc.ByteSizeOfPartition < addPartitionOffset + readSpan) addPartitionOffset = 0;
            return targetDisc.BinaryFile.ReadBinaryChunk(targetDisc.GetOffset(targetAddress, addPartitionOffset, targetMemoryTable), readSpan);
        }

        internal ushort? FindNextFreePartition(ushort startingAddress, bool targetMemoryTable = false)
        {
            ushort freeAddress = startingAddress;
            while (true) {
                if (!CheckIfPartitionIsOccupied(freeAddress, default, targetMemoryTable)) return freeAddress;
                else {
                    if (!targetMemoryTable) {
                        if (freeAddress == startingAddress - 1) return null;
                        if (freeAddress < targetDisc.DataPartitions) freeAddress++;
                        else freeAddress = (ushort)(targetDisc.AddressTableOffset + 1);
                    } else {
                        if (freeAddress == startingAddress - 1) return null;
                        if (freeAddress < targetDisc.AddressTableOffset) freeAddress++;
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
                        if (semiFreeAddress < targetDisc.DataPartitions) semiFreeAddress++;
                        else semiFreeAddress = (ushort)(targetDisc.AddressTableOffset + 1);
                    } else {
                        if (semiFreeAddress < targetDisc.AddressTableOffset) semiFreeAddress++;
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
            return (ushort)(targetDisc.ByteSizeOfPartition - lastOccupiedByte);
        }

        internal int[] FindMatchingPatterns(byte?[] targetPattern, bool targetMemoryTable = false) {
            List<int> result = new List<int> ();
            ushort maxIterations = (!targetMemoryTable) ? (ushort)targetDisc.DataPartitions : (ushort)targetDisc.AddressTableOffset;

            for (ushort targetedPartition = 1; targetedPartition < maxIterations; targetedPartition++) {
                int offset = targetDisc.GetOffset(targetedPartition, default, targetMemoryTable);
                Span<byte> binaryChunk = targetDisc.BinaryFile.ReadBinaryChunk(offset, targetDisc.ByteSizeOfPartition);

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

        internal (ushort[] adresses, ushort[] addedOffset) RegisterAddresses(Span<ushort> addressesToRegister) {

            ushort addressAmountPerBracket = (ushort)((targetDisc.MinBracketSize - DiscAccess.BracketMetaSizeNoID) / 2);
            ushort bracketsNeeded = (ushort)(addressesToRegister.Length / addressAmountPerBracket + 1);

            // if (upTo15CharID == "") upTo15CharID += AddressEntry.BracketCounter;
            // Check If the ID exists already;

            Span<byte[]> brackets = new byte[bracketsNeeded][];
            ushort ThisManyMoreNeeded = (ushort)(bracketsNeeded - 1);

            for (ushort i = 0; i < brackets.Length; i++) {
                Span<byte> byteNotedAddresses = ((i + 1) * addressAmountPerBracket <= addressesToRegister.Length) ? stackalloc byte[addressAmountPerBracket * 2] : stackalloc byte[addressesToRegister.Length % addressAmountPerBracket * 2];
                for (ushort j = 0; j < byteNotedAddresses.Length / 2; j++) {
                    Span<byte> individualAddress = BinaryTooling.SplitIntoBytes(addressesToRegister[i * addressAmountPerBracket + j]);
                    individualAddress.Slice(0, 2).CopyTo(byteNotedAddresses.Slice(j * 2, 2));
                }
                brackets[i] = AddressEntry.BuildBracket(byteNotedAddresses.ToArray(), (byte)(addressAmountPerBracket * 2), (byte)ThisManyMoreNeeded);
                ThisManyMoreNeeded--;
            }

            Span<ushort> overwrittenAddresses = new ushort[bracketsNeeded];
            Span<ushort> addressOffset = new ushort[bracketsNeeded];

            ushort bracketsPushed = 0;
            while (bracketsPushed < bracketsNeeded) {
                (ushort? address, ushort? remainingSpace) inputToAddress = FindPartiallyFreePartition(1, targetDisc.MinBracketSize, true);
                if (!inputToAddress.address.HasValue || !inputToAddress.remainingSpace.HasValue) return (new ushort[0], new ushort[0]);

                int bracketWiseSpace = (inputToAddress.remainingSpace.Value / targetDisc.MinBracketSize < bracketsNeeded - bracketsPushed) ? inputToAddress.remainingSpace.Value / targetDisc.MinBracketSize : bracketsNeeded - bracketsPushed;
                Span<byte> inputChunk = new byte[bracketWiseSpace * targetDisc.MinBracketSize];

                for (byte y = 0; y < bracketWiseSpace; y++)
                {
                    brackets[y + bracketsPushed].CopyTo(inputChunk.Slice(y * targetDisc.MinBracketSize, targetDisc.MinBracketSize));
                    overwrittenAddresses[y + bracketsPushed] = inputToAddress.address.Value;
                    addressOffset[y + bracketsPushed] = (ushort)(targetDisc.ByteSizeOfPartition - inputToAddress.remainingSpace.Value);
                }
                bracketsPushed += (ushort)(inputChunk.Length / targetDisc.MinBracketSize);
                WriteToDisc(inputToAddress.address.Value, inputChunk, (ushort)(targetDisc.ByteSizeOfPartition - inputToAddress.remainingSpace.Value), default, true);
            }

            return (overwrittenAddresses.ToArray(), addressOffset.ToArray());
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