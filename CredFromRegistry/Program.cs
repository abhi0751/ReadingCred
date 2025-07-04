using Microsoft.Win32;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace CredFromRegistry
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            Console.WriteLine("creating key at  Hkey/currentuser");
            string keylocation = @"Shukla\ShuklaApp";
            string resitrykey = "appid";
            string registryvalue = "demoApp";

            WriteInWinRegistry(keylocation, resitrykey, registryvalue);

             string abc = ReadFromWinRegitry(keylocation, resitrykey);
             Console.WriteLine($"reading Registry value {abc}");

             Console.WriteLine($"Trying Reading non existing Registry location :  {ReadFromWinRegitry(@"Shukla\ShuklaApp1", "appid123")}");


        }



        private static void WriteInWinRegistry(string RegistoryLocation, string RegistryKey,string RegistryKeyValue)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistoryLocation);
            key.SetValue(RegistryKey, RegistryKeyValue);
            key.Close();
        }


        private static string ReadFromWinRegitry(string RegistoryLocation, string RegistryKey)
        { 
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistoryLocation);

            string keyvalue = "Registry location not exists. ";
            
            if (key != null)
            {
               keyvalue = (string)key.GetValue(RegistryKey);
                if(keyvalue == null)
                {
                    keyvalue = "Key either not exists or has null value";
                }
                
            }
            
            return keyvalue;

        }

    }







}
