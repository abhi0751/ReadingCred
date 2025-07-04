using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;




internal partial class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        string appName = "ShuklaApp";
        string userName = "user123";
        string password = "P@ssw0rd!";
        string comment = "User login information";

        CredentialManager123.WriteCredential(appName, userName, password);

        Console.WriteLine("Credential saved successfully.");

        


            var credential = CredentialManager123.ReadCredential(appName);
        if (credential == null)
        {
            Console.WriteLine("No credential found.");
            
        }

        Console.WriteLine($"UserName: {credential.UserName}");
        Console.WriteLine($"Secret: {credential.Password}");
       

    }
}

    

