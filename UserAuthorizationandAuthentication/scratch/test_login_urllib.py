import urllib.request
import urllib.error
import json

url = "http://localhost:5210/api/Users/login"
email = "admin1@gmail.com"
passwords = ["Admin123!", "admin123", "password", "Password123!", "12345678"]

for pwd in passwords:
    data = json.dumps({"email": email, "password": pwd}).encode('utf-8')
    req = urllib.request.Request(url, data=data, headers={'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req) as response:
            res = response.read().decode('utf-8')
            print(f"Success with password: {pwd}")
            print(res)
            break
    except urllib.error.HTTPError as e:
        print(f"Failed with {pwd}: {e.code}")
