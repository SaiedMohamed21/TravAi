import urllib.request
import urllib.error
import json

url = "http://localhost:5210/api/Users/login"
login_data = json.dumps({"email": "testadmin2@travai.com", "password": "WrongPassword!"}).encode('utf-8')
req_login = urllib.request.Request(url, data=login_data, headers={'Content-Type': 'application/json'})
try:
    with urllib.request.urlopen(req_login) as response:
        res = response.read().decode('utf-8')
        print(res)
except urllib.error.HTTPError as e:
    err = e.read().decode('utf-8')
    print(err)
