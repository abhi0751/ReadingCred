# ReadingCred

Minimum code to read and write credential in Windows credential manager and Windows Registry.

Tested with Windows 11(x64) and Dot Net version 8.0

![Code in module](/D:/Github-Repos-Code/abhi0751/ReadingCred/msalvba/Code-in-module.png)
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



