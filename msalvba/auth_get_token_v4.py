import sys
import msal
import winreg
import base64
import json
import time
from datetime import timezone, datetime

# ----------------------------
# Configuration
# ----------------------------
DEFAULT_SCOPE = ['User.Read']
DEFAULT_CLIENT_ID = ''
DEFAULT_TENANT_ID = ''
EXPIRY_THRESHOLD_MINUTES = 15  # Refresh token if less than this many minutes remain

# Global registry path to be set dynamically
REGISTRY_PATH = None

# ----------------------------
# Registry Functions
# ----------------------------

def set_registry_path(reg_path: str):
    """Set the base registry path to use for read/write."""
    global REGISTRY_PATH
    REGISTRY_PATH = reg_path

def read_registry_value(key_name: str, default=None):
    """Read a value from the registry."""
    if not REGISTRY_PATH:
        raise ValueError("Registry path not set.")
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, REGISTRY_PATH, 0, winreg.KEY_READ) as reg_key:
            value, _ = winreg.QueryValueEx(reg_key, key_name)
            return value
    except FileNotFoundError:
        return default
    except Exception as e:
        print(f"Error reading registry key '{key_name}': {e}")
        return default

def store_token_in_registry(token: str):
    """Store token and timestamp in registry."""
    if not REGISTRY_PATH:
        raise ValueError("Registry path not set.")
    try:
        with winreg.CreateKey(winreg.HKEY_CURRENT_USER, REGISTRY_PATH) as reg_key:
            winreg.SetValueEx(reg_key, "AccessToken", 0, winreg.REG_SZ, token)
            timestamp = datetime.now(timezone.utc).isoformat()
            winreg.SetValueEx(reg_key, "TokenCreated", 0, winreg.REG_SZ, timestamp)
        print("✅ Token and timestamp saved to registry.")
    except Exception as e:
        print(f"❌ Failed to write to registry: {e}")

# ----------------------------
# Token Validation
# ----------------------------

def is_token_valid(token: str):
    """Check if the token is a valid JWT and not expiring soon."""
    try:
        parts = token.split('.')
        if len(parts) != 3:
            print("❌ Invalid JWT format.")
            return False

        payload_encoded = parts[1] + '=' * (-len(parts[1]) % 4)
        payload_json = base64.urlsafe_b64decode(payload_encoded).decode('utf-8')
        payload = json.loads(payload_json)

        exp_timestamp = payload.get("exp")
        if not exp_timestamp:
            print("❌ No 'exp' claim found in token.")
            return False

        current_timestamp = int(time.time())
        seconds_remaining = exp_timestamp - current_timestamp
        minutes_remaining = seconds_remaining / 60

        print(f"⏱ Token expires in {int(minutes_remaining)} minutes.")
        return minutes_remaining > EXPIRY_THRESHOLD_MINUTES

    except Exception as e:
        print(f"❌ Error decoding token: {e}")
        return False

# ----------------------------
# Token Logic
# ----------------------------

def acquire_token():
    """Acquire a new token via MSAL."""
    client_id = read_registry_value("ClientId", DEFAULT_CLIENT_ID)
    tenant_id = read_registry_value("TenantId", DEFAULT_TENANT_ID)
    scope_str = read_registry_value("Scope", ",".join(DEFAULT_SCOPE))

    if not client_id or not tenant_id:
        print("❌ Client ID or Tenant ID missing in registry.")
        return None

    authority = f"https://login.microsoftonline.com/{tenant_id}"
    scopes = [s.strip() for s in scope_str.split(",")]

    app = msal.PublicClientApplication(client_id=client_id, authority=authority)

    accounts = app.get_accounts()
    result = app.acquire_token_silent(scopes, account=accounts[0]) if accounts else None

    if not result:
        result = app.acquire_token_interactive(scopes=scopes)

    if "access_token" in result:
        token = result["access_token"]
        print("✅ New token acquired.")
        return token
    else:
        print("❌ Failed to acquire token.")
        print("Error:", result.get("error"))
        print("Description:", result.get("error_description"))
        return None

def get_token():
    """Return a valid token, acquiring a new one if needed."""
    token = read_registry_value("AccessToken")
    if token and is_token_valid(token):
        print("✅ Using valid token from registry.")
        return token

    print(f"⚠️ Token missing, invalid, or expiring soon. Requesting new token...")
    token = acquire_token()
    if token:
        store_token_in_registry(token)
    return token

# ----------------------------
# Main Execution Entry
# ----------------------------

def main():
    if len(sys.argv) < 2:
        print("Usage: python auth_get_token.py <RegistryPath>")
        sys.exit(1)
    
    print("Getting token from this registry" + sys.argv[1])

    reg_path_arg = sys.argv[1]
    set_registry_path(reg_path_arg)
    token = get_token()

    if token:
        print(token)  # <- return to VBA
    else:
        print("ERROR: Token acquisition failed")

if __name__ == "__main__":
    main()
