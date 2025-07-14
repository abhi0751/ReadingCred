import sys
import msal
import winreg
import base64
import json
import time
from datetime import timezone, datetime, timedelta

# ----------------------------
# Configuration
# ----------------------------
DEFAULT_SCOPE = ['User.Read']
DEFAULT_CLIENT_ID = ''
DEFAULT_TENANT_ID = ''
REGISTRY_PATH = None
TOKEN_FILE = "D:\\msalvba\\token.txt"
EXPIRY_THRESHOLD_MINUTES = 85  # Refresh token if less than this many minutes remain


# ----------------------------
# Registry Functions
# ----------------------------

def set_registry_path(reg_path: str):
    global REGISTRY_PATH
    REGISTRY_PATH = reg_path


def read_registry_value(key_name: str, default=None):
    if not REGISTRY_PATH:
        raise ValueError("Registry path not set.")
    try:
        reg_key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, REGISTRY_PATH, 0, winreg.KEY_READ)
        value, _ = winreg.QueryValueEx(reg_key, key_name)
        winreg.CloseKey(reg_key)
        return value
    except FileNotFoundError:
        return default
    except Exception as e:
        print(f"Error reading registry key '{key_name}': {e}")
        return default


def store_token_in_registry(token: str):
    if not REGISTRY_PATH:
        raise ValueError("Registry path not set.")
    try:
        reg_key = winreg.CreateKey(winreg.HKEY_CURRENT_USER, REGISTRY_PATH)
        winreg.SetValueEx(reg_key, "AccessToken", 0, winreg.REG_SZ, token)
        timestamp = datetime.now(timezone.utc).isoformat()
        winreg.SetValueEx(reg_key, "TokenCreated", 0, winreg.REG_SZ, timestamp)
        winreg.CloseKey(reg_key)
        print("✅ Token and timestamp saved to registry.")
    except Exception as e:
        print(f"Failed to write to registry: {e}")


# ----------------------------
# Token Expiry Checker (JWT Parsing)
# ----------------------------

def is_token_valid():
    """
    Decodes the JWT token from registry and checks whether it's valid for at least 15 more minutes.
    """
    token = read_registry_value("AccessToken")
    if not token:
        print("No token found in registry.")
        return False

    try:
        parts = token.split('.')
        if len(parts) != 3:
            print("Invalid JWT format.")
            return False

        # Base64 decode the payload
        payload_encoded = parts[1]
        payload_encoded += '=' * (-len(payload_encoded) % 4)
        payload_json = base64.urlsafe_b64decode(payload_encoded).decode('utf-8')
        payload = json.loads(payload_json)

        exp_timestamp = payload.get("exp")
        if not exp_timestamp:
            print("No 'exp' claim found in token.")
            return False

        current_timestamp = int(time.time())
        seconds_remaining = exp_timestamp - current_timestamp
        minutes_remaining = seconds_remaining / 60

        print(f"Token expires in {int(minutes_remaining)} minutes.")
        return minutes_remaining > EXPIRY_THRESHOLD_MINUTES

    except Exception as e:
        print(f"Error decoding token: {e}")
        return False


# ----------------------------
# Token Retrieval Logic
# ----------------------------

def get_token():
    # Use cached token if still valid
    if is_token_valid():
        token = read_registry_value("AccessToken")
        print("✅ Using cached token from registry.")
        return token

    # Else acquire new token
    print(f"Existing token is empty, invalid, or expiring within {EXPIRY_THRESHOLD_MINUTES} minutes. Acquiring new token...")
    client_id = read_registry_value("ClientId", DEFAULT_CLIENT_ID)
    tenant_id = read_registry_value("TenantId", DEFAULT_TENANT_ID)
    scope_str = read_registry_value("Scope", ",".join(DEFAULT_SCOPE))

    if not client_id or not tenant_id:
        print("❌ Client ID or Tenant ID missing in registry.")
        return None

    authority = f"https://login.microsoftonline.com/{tenant_id}"
    scopes = [s.strip() for s in scope_str.split(",")]

    app = msal.PublicClientApplication(client_id=client_id, authority=authority)

    result = None
    accounts = app.get_accounts()
    if accounts:
        result = app.acquire_token_silent(scopes, account=accounts[0])

    if not result:
        result = app.acquire_token_interactive(scopes=scopes)

    if "access_token" in result:
        token = result["access_token"]
        store_token_in_registry(token)
        return token
    else:
        print("❌ Failed to acquire token.")
        print("Error:", result.get("error"))
        print("Description:", result.get("error_description"))
        return None


# ----------------------------
# Main Entry Point
# ----------------------------

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python auth_get_token.py <RegistryPath>")
        sys.exit(1)
    print("Usage:" +sys.argv[1]) 
    set_registry_path(sys.argv[1])

    token = get_token()

    if token:
       store_token_in_registry(token)
       print(token)  # <- RETURN to VBA
    else:
       print("ERROR: Token acquisition failed")
