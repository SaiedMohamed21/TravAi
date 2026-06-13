import urllib.request
import urllib.error
import json

# 1. Login to get token
login_url = "http://localhost:5210/api/Users/login"
login_data = json.dumps({"email": "admin1@gmail.com", "password": "123456789"}).encode('utf-8')
login_req = urllib.request.Request(login_url, data=login_data, headers={'Content-Type': 'application/json'})

try:
    with urllib.request.urlopen(login_req) as response:
        res = json.loads(response.read().decode('utf-8'))
        token = res["data"]["token"]
        print("Login Success. Token acquired.")
        
        # 2. Call User Detail Aggregation API
        detail_url = "http://localhost:5210/api/airline/admin/users/1"
        detail_req = urllib.request.Request(detail_url, headers={
            'Content-Type': 'application/json',
            'Authorization': f'Bearer {token}'
        })
        
        with urllib.request.urlopen(detail_req) as detail_response:
            detail_res = json.loads(detail_response.read().decode('utf-8'))
            print("\nUser Details Aggregation Response:")
            print(json.dumps(detail_res, indent=2))
            
except urllib.error.HTTPError as e:
    print(f"Error {e.code}: {e.read().decode('utf-8')}")
