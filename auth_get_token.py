# auth_get_token.py

import msal
import json
import webbrowser

CLIENT_ID = 'YOUR_CLIENT_ID'
TENANT_ID = 'common'  # or your tenant ID
AUTHORITY = f'https://login.microsoftonline.com/{TENANT_ID}'
REDIRECT_URI = 'http://localhost'
SCOPE = ['User.Read']  # or more, e.g. ['https://graph.microsoft.com/.default']

def get_token():
    app = msal.PublicClientApplication(CLIENT_ID, authority=AUTHORITY)

    # Try silent login first (optional)
    accounts = app.get_accounts()
    if accounts:
        result = app.acquire_token_silent(SCOPE, account=accounts[0])
        if result and "access_token" in result:
            return result["access_token"]

    # Fallback: interactive login
    result = app.acquire_token_interactive(
        scopes=SCOPE,
        redirect_uri=REDIRECT_URI
    )

    if "access_token" in result:
        return result["access_token"]
    else:
        raise Exception("Login failed: " + json.dumps(result, indent=2))

if __name__ == "__main__":
    token = get_token()
    with open("token.txt", "w") as f:
        f.write(token)
    print("Token saved to token.txt")
