using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var path = "/Volumes/Data-1/test/test"; // Updated SMB share path
        Console.WriteLine($"Monitoring folder: {path}. Press any key to exit...");

        var activeFiles = new Dictionary<string, long>();
        var completedFiles = new HashSet<string>();

        while (true)
        {
            var currentFiles = Directory.GetFiles(path);

            foreach (var file in currentFiles)
            {
                if (completedFiles.Contains(file))
                {
                    continue; // Skip files that are already completed
                }

                long currentSize;

                try
                {
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        currentSize = stream.Length;
                        Console.WriteLine($"Current size of {file} is {currentSize}");
                    }
                }
                catch (IOException)
                {
                    // Failed to open the file; likely still being written to
                    continue;
                }

                if (!activeFiles.ContainsKey(file))
                {
                    activeFiles[file] = currentSize;
                    Console.WriteLine($"File created: {file}");
                    continue;
                }

                if (currentSize == 0 || currentSize != activeFiles[file])
                {
                    activeFiles[file] = currentSize;
                }
                else
                {
                    // Try to acquire a lock on the file
                    try
                    {
                        using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            // If we can acquire a lock, the file is not being written to anymore
                            Console.WriteLine($"File finished copying: {file}");
                            completedFiles.Add(file);
                            activeFiles.Remove(file); // Remove the file from the active files dictionary
                        }
                    }
                    catch (IOException)
                    {
                        // Failed to acquire a lock; file is still being written to
                        continue;
                    }
                }
            }

            System.Threading.Thread.Sleep(2000); // Wait for 2 seconds before checking again
        }
    }
}
