VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Sheet1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Option Explicit

' === Main Usage ===
Sub UseAccessToken()
    Dim token As String
    token = ReadAccessTokenFromRegistry("Shukla\ShuklaApp")

    If token = "" Then
        MsgBox "? Failed to retrieve access token.", vbCritical
    Else
        MsgBox "? Access token loaded!" & vbCrLf & Left(token, 100) & "...", vbInformation
        Sheets(1).Range("A1").Value = token
    End If
    
    token = ReadAccessTokenFromRegistry("Shukla\ShuklaApp")
    If token = "" Then
        MsgBox "? Failed to retrieve access token.", vbCritical
    Else
        MsgBox "? Access token loaded!" & vbCrLf & Left(token, 100) & "...", vbInformation
        Sheets(1).Range("A2").Value = token
    End If
    
    token = ReadAccessTokenFromRegistry("Shukla\ShuklaApp")
    If token = "" Then
        MsgBox "? Failed to retrieve access token.", vbCritical
    Else
        MsgBox "? Access token loaded!" & vbCrLf & Left(token, 100) & "...", vbInformation
        Sheets(1).Range("A3").Value = token
    End If
    
End Sub

' === Core Token Reader ===
Public Function ReadAccessTokenFromRegistry(registrySubKey As String) As String
    Dim shell As Object
    Dim token As String
    Dim registryValuePath As String

    ' Step 1: Run Python to ensure token is refreshed
    If Not RunPythonTokenScript(registrySubKey) Then
        Debug.Print "? Python script failed."
        ReadAccessTokenFromRegistry = ""
        Exit Function
    End If

    ' Step 2: Read from registry
    On Error GoTo ReadError
    Set shell = CreateObject("WScript.Shell")
    registryValuePath = "HKEY_CURRENT_USER\" & registrySubKey & "\AccessToken"
    token = shell.RegRead(registryValuePath)

    ReadAccessTokenFromRegistry = token
    Exit Function

ReadError:
    Debug.Print "? Error reading token from registry: " & Err.Description
    ReadAccessTokenFromRegistry = ""
End Function

' === Run Python Token Logic ===
Private Function RunPythonTokenScript(registrySubKey As String) As Boolean
    Dim shell As Object
    Dim pythonExePath As String
    Dim scriptPath As String
    Dim command As String

    On Error GoTo Fail

    pythonExePath = """C:\Users\abhi0\AppData\Local\Programs\Python\Python313\python.exe"""
    scriptPath = """" & ThisWorkbook.Path & "\auth_get_token_v4.py" & """"
    command = pythonExePath & " " & scriptPath & " """ & registrySubKey & """"

    Set shell = CreateObject("WScript.Shell")
    shell.Run command, 1, True ' Wait for Python to complete

    RunPythonTokenScript = True
    Exit Function

Fail:
    Debug.Print "? Failed to run Python script: " & Err.Description
    RunPythonTokenScript = False
End Function

