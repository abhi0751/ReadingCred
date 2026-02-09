using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using static ConfigCreator.Program;
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

        static void Main(string[] args)
        {
            string commandAssemblyName = "Configcreator.exe";
            string mode = "Auto";   // default
            string fileName = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {

                    case "-help":
                    case "-h":
                        ShowHelp(commandAssemblyName);
                        return;


                    case "-mode":
                    case "-m":

                        i++;
                        if (i >= args.Length)
                        {
                            Console.WriteLine("Error: -mode requires a value.");
                            return;
                        }

                        string modeValue = args[i].ToLower();

                        switch (modeValue)
                        {


                            case "-auto":
                            case "-a":
                                mode = "Auto";
                                break;

                            case "-file":
                            case "-f":
                                mode = "File";
                                if (i + 1 < args.Length)
                                {
                                    fileName = args[i + 1];
                                    i++;
                                }
                                else
                                {
                                    Console.WriteLine("Error: -file requires a filename.");
                                    return;
                                }


                                break;

                            case "-manual":
                            case "-m":
                                mode = "Manual";
                                break;

                            default:
                                Console.WriteLine($"Invalid mode: {modeValue}");
                                return;
                        }
                        break;

                    // Only valid AFTER mode=file




                    default:
                        Console.WriteLine($"Unknown argument: {args[i]} at {i}");
                        return;
                }
            }

            // -------- VALIDATION --------
            if (mode == "file" && string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("File mode requires  <filename>");
                return;
            }

            // -------- EXECUTION --------
            Console.WriteLine($"Mode : {mode}");
            Console.WriteLine($"File : {fileName}");




            // ---------- Execute Based on Mode ----------
            Config config;

            switch (mode)
            {
                case "Auto":
                    Console.WriteLine("Running in AUTO mode...");
                    config = LoadConfiginAutoMode();
                    RunConfig(config);
                    break;

                case "File":
                    config = LoadConfig(fileName);
                    break;

                case "Manual":
                    Console.WriteLine("Running in MANUAL mode...");
                    RunManualMode();
                    // config = GetConfigFromUserInput();
                    break;

                default:
                    throw new Exception("Unexpected mode");
            }
        }
        // RunApplication(config);













        static void RunManualMode()
        {
            Console.WriteLine("Running in MANUAL mode...");
            Console.Write("Would you like to continue with manual entry? (yes/no): ");
            string choice = Console.ReadLine()?.Trim().ToLower();

            if (choice == "no")
            {
                Console.WriteLine("Exiting program...");
                return;
            }

             Config config = new Config();

            Console.Write("Enter registry hive (HKCU or HKLM): ");
            config.RegistryHive = Console.ReadLine()?.Trim().ToUpper() ?? "HKCU";

            Console.Write("Enter registry base path (e.g., Software\\MyManualApp): ");
            config.RegistryBasePath = Console.ReadLine()?.Trim();

            RegistryKey rootKey = GetRootRegistryKey(config.RegistryHive);
            if (rootKey == null)
            {
                Console.WriteLine($"Invalid registry hive: {config.RegistryHive}. Exiting.");
                return;
            }
            CheckOrCreateBasePath(rootKey, config.RegistryBasePath);

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

            static void ShowHelp(string commandAssemblyName)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine($"  {commandAssemblyName} [options]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -h | -help                           Show help");
                Console.WriteLine("  -m | -mode <value>                   Set run mode");
                Console.WriteLine("       auto | -a                        Automatic mode");
                Console.WriteLine("       file | -f  <filename>           Load from file");
                Console.WriteLine("       manual | -m           Manual entry");

                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine($"  {commandAssemblyName} -m auto");
                Console.WriteLine($"  {commandAssemblyName} -m file -f cfg.json");
                Console.WriteLine($"  {commandAssemblyName} -m manual");
            }

            static Config LoadConfiginAutoMode()
            {
                string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string filePath = Path.Combine(exePath, "a1.txt");
                return LoadConfig(filePath);



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

            static void RunConfig(Config config)
            {
                if (string.IsNullOrWhiteSpace(config.RegistryHive))
                    config.RegistryHive = "HKCU";

                if (string.IsNullOrWhiteSpace(config.RegistryBasePath))
                {
                    Console.WriteLine("Config file is missing 'RegistryBasePath'. Exiting.");
                    return;
                }

                Console.WriteLine($"Loaded config. Registry Hive: {config.RegistryHive}, Path: HKEY_{config.RegistryHive}\\{config.RegistryBasePath}");
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

                if (config.KeysToCreate?.Count > 0)
                {
                    foreach (var keyentity in config.KeysToCreate)
                    {
                        var inputkey = keyentity.Split(":");
                        string keyName = inputkey[0].Trim();
                        string keyvalue = inputkey[1].Trim();
                        WriteToRegistry(rootKey, config.RegistryBasePath, keyName, keyvalue);
                    }
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
