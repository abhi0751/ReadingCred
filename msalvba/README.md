# ReadingCred

Minimum code to read and write credential in Windows credential manager and Windows Registry.

Tested with Windows 11(x64) and Dot Net version 8.0
# Setup Windows Registry
Add your 
```plaintext
Client ID, Scope, TenantId
```

in registry. This should be in "Computer\HKEY_CURRENT_USER\"

Registry folder names you can choose as per your need, this will use in python code and VBA code

![Win Registry setup](Win-Registry-setup.png)

# File Structure in Folder

All these files must be in same folder
![Files in Folder](Files-in-Folder.png)

# Open auth_get_token.py
Open this python file and adjust the path as per your folder location.

This code will Read the ClientID, TenantID and scope from Registry.

This code will also write the acquired Token to Registry and Token.txt File.

This token from Registry will further read by VBA code.

Define these parameter values 
```plaintext
REGISTRY_PATH = r"Shukla\ShuklaApp"
TOKEN_FILE = "D:\\msalvba\\token.txt"
```
![parameter in python code](parameter-in-python-code.png)

```python
# auth_get_token.py

import msal
import winreg  # For Windows Registry operations
from datetime import datetime, timezone

# Default fallback values (can be overridden by registry)
DEFAULT_CLIENT_ID = ''
DEFAULT_TENANT_ID = ''
DEFAULT_SCOPE = ['User.Read']

REGISTRY_PATH = r"Shukla\ShuklaApp"
TOKEN_FILE = "D:\\msalvba\\token.txt"


def read_registry_value(key_name: str, default=None):
    """
    Reads a string value from HKEY_CURRENT_USER\\REGISTRY_PATH.
    Returns the value if found, otherwise returns the default.
    """
    try:
        registry_key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, REGISTRY_PATH, 0, winreg.KEY_READ)
        value, _ = winreg.QueryValueEx(registry_key, key_name)
        winreg.CloseKey(registry_key)
        return value
    except FileNotFoundError:
        print(f"Registry key '{key_name}' not found.")
    except Exception as e:
        print(f"Error reading registry key '{key_name}': {e}")
    return default


def store_token_in_registry(token):
    """
    Stores the token and creation time in the registry under HKEY_CURRENT_USER\\REGISTRY_PATH.
    """
    try:
        registry_key = winreg.CreateKey(winreg.HKEY_CURRENT_USER, REGISTRY_PATH)
        winreg.SetValueEx(registry_key, "AccessToken", 0, winreg.REG_SZ, token)

        timestamp = datetime.now(timezone.utc).isoformat()
        winreg.SetValueEx(registry_key, "TokenCreated", 0, winreg.REG_SZ, timestamp)

        winreg.CloseKey(registry_key)
        print("Token and creation time saved to Windows Registry.")
    except Exception as e:
        print(f"Failed to write to registry: {e}")


def get_token():
    client_id = read_registry_value("ClientId", DEFAULT_CLIENT_ID)
    tenant_id = read_registry_value("TenantId", DEFAULT_TENANT_ID)
    scope_str = read_registry_value("Scope", ",".join(DEFAULT_SCOPE))

    if not client_id or not tenant_id:
        print("Client ID or Tenant ID is missing in registry and no default is set.")
        return None

    authority = f"https://login.microsoftonline.com/{tenant_id}"
    scopes = [s.strip() for s in scope_str.split(",")]

    app = msal.PublicClientApplication(
        client_id=client_id,
        authority=authority
    )

    result = None
    accounts = app.get_accounts()

    if accounts:
        print("Pick the account you want to use to proceed:")
        for a in accounts:
            print(a["username"])
        chosen = accounts[0]
        result = app.acquire_token_silent(scopes, account=chosen)

    if not result:
        result = app.acquire_token_interactive(scopes=scopes)

    if "access_token" in result:
        print("Access token acquired.")
        return result["access_token"]
    else:
        print("Failed to acquire token.")
        print("Error:", result.get("error"))
        print("Description:", result.get("error_description"))
        print("Correlation ID:", result.get("correlation_id"))
        return None


if __name__ == "__main__":
    token = get_token()
    if token:
        with open(TOKEN_FILE, "w") as f:
            f.write(token)
        print(f"Token saved to {TOKEN_FILE}")

        # Save to registry
        store_token_in_registry(token)
    else:
        print("Token was not saved due to error.")
```


# VBA Module   
  
```vbscript
Sub GetAccessTokenFromRegistry()
    Dim shell As Object
    Dim token As String
    Dim registryPath As String
    
    Dim pythonExePath As String
    Dim scriptPath As String

    pythonExePath = """C:\Users\abhi0\AppData\Local\Programs\Python\Python313\python.exe"""
    scriptPath = """" & ThisWorkbook.Path & "\auth_get_token.py" & """"

    ' Define the registry path to the token
    registryPath = "HKEY_CURRENT_USER\Shukla\ShuklaApp\AccessToken"

    ' Run Python script to generate token
    Set shell = CreateObject("WScript.Shell")
    shell.Run pythonExePath & " " & scriptPath, 1, True

    On Error GoTo ReadError
    ' Read the access token from the registry
    token = shell.RegRead(registryPath)

    MsgBox "Access token retrieved!" & vbCrLf & Left(token, 100) & "..."

    ' Optional: store token in a worksheet cell
    Sheets(1).Range("A1").Value = token
    Exit Sub

ReadError:
    MsgBox "Failed to read access token from registry. Ensure Python script ran successfully.", vbCritical
End Sub
```
![Code in module](Code-in-module.png)
# Call this module in main 

```vbscript
Private Sub Workbook_Open()
    Call GetAccessTokenFromRegistry
End Sub
```

![call in main sheet](call-in-main-sheet.png)

# Run your Macro Enabled Excel file
it will launch a browser , select the account.

![Auth account selection](Auth-account-selection.png)

After the authentication. you will see a message in browser

![Auth success browser](Auth-success-browser.png)

You will see something similar in your excel. You can validate, this token is written in Token.txt file and in your registry location. 

VBA code has read this token from Registry

![Token-excel](Token-in-excel.png)

Token Stored in Registry 

![Token-in-registry](Token-in-registry.png)

Token stored in Token.Txt

![Token-Stored-in-txt](Token-stored-in-txt.png)



