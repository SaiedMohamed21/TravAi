import urllib.request, json
payload = json.dumps({"flights": [{"departure_city": "Cairo", "arrival_city": "Frankfurt", "flight_class": "Economy", "airline": "EgyptAir", "duration": "4h 30m", "stops": 0, "price_USD": 350.0, "route": "CAI_FRA", "departure_datetime": "6/28/2026 10:00", "arrival_datetime": "6/28/2026 14:30", "duration_minutes": 270}]}).encode("utf-8")
req = urllib.request.Request("https://travai-flight-api.onrender.com/recommend-flight", data=payload, headers={"Content-Type": "application/json"}, method="POST")
try:
    with urllib.request.urlopen(req) as response:
        print(response.getcode())
        print(response.read().decode())
except urllib.error.HTTPError as e:
    print(e.code)
    print(e.read().decode())

