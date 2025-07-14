# auth_get_token.py

import sys
import msal
import winreg
from datetime import datetime, timezone

# ----------------------------
# Configuration Defaults
# ----------------------------

DEFAULT_CLIENT_ID = ''
DEFAULT_TENANT_ID = ''
DEFAULT_SCOPE = ['User.Read']

# Will be set dynamically from command line
REGISTRY_PATH = None

# Output token file
TOKEN_FILE = "D:\\msalvba\\token.txt"


# ----------------------------
# Registry Path Setter
# ----------------------------

def set_registry_path(registry_path: str):
    """
    Sets the global registry path to be used for reading/writing registry values.
    """
    global REGISTRY_PATH
    REGISTRY_PATH = registry_path


# ----------------------------
# Registry Access Methods
# ----------------------------

def read_registry_value(key_name: str, default=None):
    """
    Reads a string value from HKEY_CURRENT_USER\\REGISTRY_PATH.
    Returns the value if found, otherwise returns the default.
    """
    if not REGISTRY_PATH:
        raise ValueError("Registry path not set. Call set_registry_path(path) first.")

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
    Stores the token and creation time in the registry under REGISTRY_PATH.
    """
    if not REGISTRY_PATH:
        raise ValueError("Registry path not set. Call set_registry_path(path) first.")

    try:
        registry_key = winreg.CreateKey(winreg.HKEY_CURRENT_USER, REGISTRY_PATH)
        winreg.SetValueEx(registry_key, "AccessToken", 0, winreg.REG_SZ, token)

        timestamp = datetime.now(timezone.utc).isoformat()
        winreg.SetValueEx(registry_key, "TokenCreated", 0, winreg.REG_SZ, timestamp)

        winreg.CloseKey(registry_key)
        print("Token and creation time saved to Windows Registry.")
    except Exception as e:
        print(f"Failed to write to registry: {e}")


# ----------------------------
# Token Retrieval
# ----------------------------

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


# ----------------------------
# Main Entry Point
# ----------------------------

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python auth_get_token.py <RegistryPath>")
        sys.exit(1)

    # Set registry path from command-line argument
    input_registry_path = sys.argv[1]
    set_registry_path(input_registry_path)

    # Token flow
    token = get_token()
    if token:
        with open(TOKEN_FILE, "w") as f:
            f.write(token)
        print(f"Token saved to {TOKEN_FILE}")

        store_token_in_registry(token)
    else:
        print("Token was not saved due to error.")
