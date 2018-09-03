using System;
using System.IO;
using JeremyAnsel.Media.WavefrontObj;

namespace ModelToObj
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                Console.Error.WriteLine("Input file is required");
                return;
            }

            string input = args[0];
            string output = input.Replace(".model", ".obj");
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
                output = args[1];
            
            ModelImporter importer = new ModelImporter();
            ObjFile obj = importer.Import(input);
            string directory = Path.GetDirectoryName(output);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            obj.WriteTo(output);
        }
    }
}
