# auth_get_token.py

import msal
import winreg  # For Windows Registry operations
from datetime import datetime, timezone

# Default fallback values (can be overridden by registry)
DEFAULT_CLIENT_ID = ''
DEFAULT_TENANT_ID = ''
DEFAULT_SCOPE = ['User.Read']


def read_registry_value(key_name: str, default=None):
    """
    Reads a string value from HKEY_CURRENT_USER\\Shukla\\ShuklaApp.
    Returns the value if found, otherwise returns the default.
    """
    try:
        registry_key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Shukla\ShuklaApp", 0, winreg.KEY_READ)
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
    Stores the token and creation time in the registry under HKEY_CURRENT_USER\\Shukla\\ShuklaApp.
    """
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
        with open("D:\\msalvba\\token.txt", "w") as f:
            f.write(token)
        print("Token saved to token.txt")

        # Save to registry
        store_token_in_registry(token)
    else:
        print("Token was not saved due to error.")
