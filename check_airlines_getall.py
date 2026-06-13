import urllib.request
import json

req = urllib.request.Request("http://localhost:5210/api/airline/airlines")
with urllib.request.urlopen(req) as response:
    res = json.loads(response.read().decode())
    print("GetAll response keys:", res.keys())
    if "data" in res:
        print("Data type:", type(res["data"]))
        print("Number of airlines:", len(res["data"]))
        print("Sample airlines:")
        for a in res["data"][:10]:
            print(a)
    else:
        print("Response preview:", res)
