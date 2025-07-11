# auth_get_token.py

import msal
import json
import winreg  # For Windows Registry operations
from datetime import datetime, timezone


CLIENT_ID = ''
TENANT_ID = ''
AUTHORITY = f'https://login.microsoftonline.com/{TENANT_ID}'
REDIRECT_URI = 'https://login.microsoftonline.com/common/oauth2/nativeclient'
SCOPE = ['User.Read']


def store_token_in_registry(token):
    try:
        registry_key = winreg.CreateKey(winreg.HKEY_CURRENT_USER, r"Shukla\ShuklaApp")
        winreg.SetValueEx(registry_key, "AccessToken", 0, winreg.REG_SZ, token)

        timestamp = datetime.now(timezone.utc).isoformat()
        winreg.SetValueEx(registry_key, "TokenCreated", 0, winreg.REG_SZ, timestamp)

        winreg.CloseKey(registry_key)
        print("Token and creation time saved to Windows Registry.")
    except Exception as e:
        print(f"Failed to write to registry: {e}")


def get_token():
    app = msal.PublicClientApplication(
        client_id=CLIENT_ID,
        authority=AUTHORITY
    )

    result = None

    accounts = app.get_accounts()
    if accounts:
        print("Pick the account you want to use to proceed:")
        for a in accounts:
            print(a["username"])

        chosen = accounts[0]
        result = app.acquire_token_silent(SCOPE, account=chosen)

    if not result:
        result = app.acquire_token_interactive(scopes=SCOPE)

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
        with open("D:\msalvba\token.txt", "w") as f:
            f.write(token)
        print("Token saved to token.txt")

	# Save to registry
        store_token_in_registry(token)
    else:
        print("Token was not saved due to error.")

