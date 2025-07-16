VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Sheet1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Sub UseAccessToken()
    Dim token As String
    Dim regPath As String
    regPath = "Shukla\ShuklaApp"

    ' First call
    token = ReadAccessTokenFromRegistry(regPath)
    If token = "" Then
        MsgBox "? Failed to retrieve access token.", vbCritical
    Else
        MsgBox "? Access token loaded!" & vbCrLf & Left(token, 100) & "...", vbInformation
        Sheets(1).Range("A1").Value = token
    End If

    ' Second call
    token = ReadAccessTokenFromRegistry("Shukla\ShuklaApp")
    If token = "" Then
        MsgBox "? Failed to retrieve access token.", vbCritical
    Else
        MsgBox "? Access token loaded!" & vbCrLf & Left(token, 100) & "...", vbInformation
        Sheets(1).Range("A2").Value = token
    End If

    ' Third call
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

    ' Step 1: Run .NET EXE to refresh or validate token
    If Not RunDotNetTokenExecutable(registrySubKey) Then
        Debug.Print "? Token executable failed."
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

' === Run .NET Console App ===
Private Function RunDotNetTokenExecutable(registrySubKey As String) As Boolean
    Dim shell As Object
    Dim exePath As String
    Dim command As String

    On Error GoTo Fail

    ' ? Use absolute path here
    exePath = """D:\msalvba\AuthTokenManager\AuthTokenManager\bin\Debug\net8.0\AuthTokenManager.exe"""
    command = exePath & " """ & registrySubKey & """"

    Set shell = CreateObject("WScript.Shell")
    ' ?? Run invisibly: 0 = Hidden window, True = wait for completion
    shell.Run command, WINDOW_HIDDEN, True
    'shell.Run command, 1, True  ' Waits until EXE finishes

    RunDotNetTokenExecutable = True
    Exit Function

Fail:
    Debug.Print "? Failed to run .NET EXE: " & Err.Description
    RunDotNetTokenExecutable = False
End Function


