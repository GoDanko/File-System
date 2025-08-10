using System;
using FileSystem.MemoryManager;
using FileSystem.FSComponents;
using Debugging;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Setting Up Stuff
            Console.Clear();
            EventLogger.StartSession(true);
            FS storage = new FS(DiscAccess.InitialiseStorage(256));

            EventLogger.CursorReturnLog($"Do you want to re-image the disc?", 1, 1, false);
            if (Console.ReadKey(true).KeyChar == 'y') {
                EventLogger.CursorReturnLog($"Overwriting the Disc. Please wait...", 2, 1, false);
                storage.targetDisc = storage.targetDisc.CreateNewDisc();
                Console.Clear();
            }

            int howMuchContent = 128;
            byte[] RandomContent = new byte[howMuchContent];
            Random RNG = new Random();
            for (int i = 0; i < howMuchContent; i++) {
                RandomContent[i] = (byte)RNG.Next(1, 255);
            }

            DiscAccess.LogByteArrayToHexString(AddressEntry.OrderBrackets(RandomContent));
        }
    }
}