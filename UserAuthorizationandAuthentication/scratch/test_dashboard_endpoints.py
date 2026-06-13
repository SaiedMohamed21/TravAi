import urllib.request
import urllib.error
import json

# Login
url_login = "http://localhost:5210/api/Users/login"
login_data = json.dumps({"email": "testadmin2@travai.com", "password": "Password123!"}).encode('utf-8')
req_login = urllib.request.Request(url_login, data=login_data, headers={'Content-Type': 'application/json'})

token = ""
try:
    with urllib.request.urlopen(req_login) as response:
        res = json.loads(response.read().decode('utf-8'))
        token = res["data"]["token"]
except urllib.error.HTTPError as e:
    print(f"Login Failed: {e.code}")
    exit(1)

# Get KPIs
req_kpi = urllib.request.Request("http://localhost:5210/api/Admin/dashboard/kpis", headers={'Authorization': f'Bearer {token}'})
try:
    with urllib.request.urlopen(req_kpi) as response:
        print("KPIs Response:")
        kpi_json = json.loads(response.read().decode('utf-8'))
        print(json.dumps(kpi_json, indent=2)[:500] + "...")
except urllib.error.HTTPError as e:
    print(f"KPI Failed: {e.code}")

# Get Charts
req_chart = urllib.request.Request("http://localhost:5210/api/Admin/dashboard/charts", headers={'Authorization': f'Bearer {token}'})
try:
    with urllib.request.urlopen(req_chart) as response:
        print("\nCharts Response:")
        chart_json = json.loads(response.read().decode('utf-8'))
        print(json.dumps(chart_json, indent=2)[:500] + "...")
except urllib.error.HTTPError as e:
    print(f"Charts Failed: {e.code}")
