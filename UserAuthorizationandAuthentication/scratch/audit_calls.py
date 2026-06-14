import urllib.request
import urllib.error
import json

# 1. Log in to get token
login_url = "http://localhost:5210/api/Users/login"
login_payload = json.dumps({"email": "admin1@gmail.com", "password": "123456789"}).encode("utf-8")
req = urllib.request.Request(
    login_url,
    data=login_payload,
    headers={"Content-Type": "application/json"},
    method="POST"
)

try:
    with urllib.request.urlopen(req) as response:
        login_res = json.loads(response.read().decode("utf-8"))
        token = login_res.get("data", {}).get("token")
except Exception as e:
    print("Login failed:", e)
    exit(1)

headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

def call_get(url):
    req = urllib.request.Request(url, headers=headers, method="GET")
    try:
        with urllib.request.urlopen(req) as response:
            return response.status, json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        try:
            err_body = e.read().decode("utf-8")
            return e.code, err_body
        except:
            return e.code, str(e)
    except Exception as e:
        return 500, str(e)

print("=== 1. HOTEL REQUESTS ===")
status, res = call_get("http://localhost:5210/api/Admin/applications")
print("Status:", status)
if status == 200:
    data = res.get("data", [])
    print(f"Total requests: {len(data)}")
    if data:
        print("Sample request keys:", list(data[0].keys()))
        print("Sample request details:", json.dumps(data[0], indent=2))

print("\n=== 2. HOTEL REVIEWS ===")
status, res = call_get("http://localhost:5210/api/admin/hotel-reviews?pageSize=5")
print("Status:", status)
if status == 200:
    data = res.get("data", {})
    print("Reviews response keys:", list(data.keys()))
    print(f"Total reviews in DB: {data.get('totalCount')}")
    items = data.get("items", [])
    if items:
        print("Sample review details:", json.dumps(items[0], indent=2))

print("\n=== 3. HOTEL REPORTS (KPIS) ===")
status, res = call_get("http://localhost:5210/api/Admin/dashboard/kpis")
print("Status:", status)
if status == 200:
    data = res.get("data", {})
    print("KPIs keys:", list(data.keys()))
    print("Top Hotels count:", len(data.get("topHotels", [])))
    if data.get("topHotels"):
        print("Sample top hotel:", json.dumps(data.get("topHotels")[0], indent=2))
    print("Top Cities count:", len(data.get("topCities", [])))
    if data.get("topCities"):
        print("Sample top city:", json.dumps(data.get("topCities")[0], indent=2))

print("\n=== 4. HOTEL REPORTS (CHARTS) ===")
status, res = call_get("http://localhost:5210/api/Admin/dashboard/charts")
print("Status:", status)
if status == 200:
    data = res.get("data", {})
    print("Charts keys:", list(data.keys()))

print("\n=== 5. FLIGHT REPORTS ===")
status, res = call_get("http://localhost:5210/api/airline/admin/reports")
print("Status:", status)
if status == 200:
    data = res.get("data", {})
    print("Flight stats keys:", list(data.keys()))
    print("Revenue By Airline sample:", json.dumps(data.get("revenueByAirline", [])[:2], indent=2))
    print("Top Routes sample:", json.dumps(data.get("topRoutes", [])[:2], indent=2))
    print("Route Performance sample:", json.dumps(data.get("routePerformance", [])[:2], indent=2))
