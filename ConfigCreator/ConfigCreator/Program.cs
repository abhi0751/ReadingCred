using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Win32;
namespace ConfigCreator
{
    class Program
    {
        public class Config
        {
            public string RegistryHive { get; set; } = "HKCU";
            public string RegistryBasePath { get; set; }
            public List<string> KeysToCreate { get; set; } = new();
        }

        static void Main()
        {
            Console.Write("Enter config file name (e.g., config1.json): ");
            string fileName = Console.ReadLine()?.Trim();

            Config config = null;
            bool useManualEntry = false;

            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            {
                Console.WriteLine("Input file not provided or doesn't exist.");

                Console.Write("Would you like to continue with manual entry? (yes/no): ");
                string choice = Console.ReadLine()?.Trim().ToLower();

                if (choice == "no")
                {
                    Console.WriteLine("Exiting program...");
                    return;
                }

                config = new Config();

                Console.Write("Enter registry hive (HKCU or HKLM): ");
                config.RegistryHive = Console.ReadLine()?.Trim().ToUpper() ?? "HKCU";

                Console.Write("Enter registry base path (e.g., Software\\MyManualApp): ");
                config.RegistryBasePath = Console.ReadLine()?.Trim();

                useManualEntry = true;
            }
            else
            {
                config = LoadConfig(fileName);

                if (string.IsNullOrWhiteSpace(config.RegistryHive))
                    config.RegistryHive = "HKCU";

                if (string.IsNullOrWhiteSpace(config.RegistryBasePath))
                {
                    Console.WriteLine("Config file is missing 'RegistryBasePath'. Exiting.");
                    return;
                }

                Console.WriteLine($"Loaded config. Registry Hive: {config.RegistryHive}, Path: HKEY_{config.RegistryHive}\\{config.RegistryBasePath}");
            }

            RegistryKey rootKey = GetRootRegistryKey(config.RegistryHive);
            if (rootKey == null)
            {
                Console.WriteLine($"Invalid registry hive: {config.RegistryHive}. Exiting.");
                return;
            }

            // Validate and optionally create base path
            if (!CheckOrCreateBasePath(rootKey, config.RegistryBasePath))
            {
                Console.WriteLine("Exiting program.");
                return;
            }

            if (!useManualEntry && config.KeysToCreate?.Count > 0)
            {
                foreach (var keyName in config.KeysToCreate)
                {
                    Console.Write($"Enter value for '{keyName}': ");
                    string value = Console.ReadLine();
                    WriteToRegistry(rootKey, config.RegistryBasePath, keyName, value);
                }
            }

            while (true)
            {
                Console.Write("\nWould you like to continue and add more keys? (yes/no): ");
                string answer = Console.ReadLine()?.Trim().ToLower();

                if (answer == "no")
                {
                    Console.WriteLine("Exiting program...");
                    break;
                }
                else if (answer == "yes")
                {
                    Console.Write("Enter new key name: ");
                    string keyName = Console.ReadLine();

                    Console.Write("Enter value for the key: ");
                    string value = Console.ReadLine();

                    WriteToRegistry(rootKey, config.RegistryBasePath, keyName, value);
                }
                else
                {
                    Console.WriteLine("Please enter 'yes' or 'no'.");
                }
            }
        }

        static Config LoadConfig(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Config>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load config: {ex.Message}");
                return new Config();
            }
        }

        static RegistryKey GetRootRegistryKey(string hive)
        {
            return hive switch
            {
                "HKCU" => Registry.CurrentUser,
                "HKLM" => Registry.LocalMachine,
                _ => null
            };
        }

        static bool CheckOrCreateBasePath(RegistryKey root, string basePath)
        {
            RegistryKey existingKey = root.OpenSubKey(basePath, writable: true);

            if (existingKey == null)
            {
                Console.WriteLine($"Registry base path '{root.Name}\\{basePath}' does not exist.");

                Console.Write("Would you like to create it? (yes/no): ");
                string answer = Console.ReadLine()?.Trim().ToLower();

                if (answer != "yes")
                    return false;

                try
                {
                    root.CreateSubKey(basePath);
                    Console.WriteLine($"Created base path: {root.Name}\\{basePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create registry path: {ex.Message}");
                    return false;
                }
            }
            else
            {
                existingKey.Dispose();
            }

            return true;
        }

        static void WriteToRegistry(RegistryKey root, string basePath, string keyName, string value)
        {
            try
            {
                using (RegistryKey baseKey = root.CreateSubKey(basePath))
                {
                    baseKey.SetValue(keyName, value);
                    Console.WriteLine($"'{keyName}' = '{value}' written successfully under {root.Name}\\{basePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to registry: {ex.Message}");
            }
        }
    }
}