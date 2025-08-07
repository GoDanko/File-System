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
            // Setting Up Disk
                TextFormat.CreateWriteTable();
                FS storage = new FS(DiscAccess.InitialiseStorage(256));
        }
    }
}