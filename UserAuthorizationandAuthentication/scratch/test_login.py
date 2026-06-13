import requests

url = "http://localhost:5210/api/Users/login"
email = "admin1@gmail.com"
passwords = ["Admin123!", "admin123", "password", "Password123!", "12345678"]

for pwd in passwords:
    resp = requests.post(url, json={"email": email, "password": pwd})
    if resp.status_code == 200:
        print(f"Success with password: {pwd}")
        print(resp.json())
        break
    else:
        print(f"Failed with {pwd}: {resp.status_code}")
