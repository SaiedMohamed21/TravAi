import urllib.request
import urllib.error
import json

url = "http://localhost:5210/api/Users/register"
req_data = {
    "userName": "testadmin2",
    "name": "Test Admin",
    "email": "testadmin2@travai.com",
    "password": "Password123!",
    "dateOfBirth": "1990-01-01",
    "gender": "Male",
    "phoneNumber": "01234567890"
}

data = json.dumps(req_data).encode('utf-8')
req = urllib.request.Request(url, data=data, headers={'Content-Type': 'application/json'})
try:
    with urllib.request.urlopen(req) as response:
        res = response.read().decode('utf-8')
        print(f"Register Success:")
        print(res)
except urllib.error.HTTPError as e:
    err = e.read().decode('utf-8')
    print(f"Register Failed: {e.code}")
    print(err)

# We must update the role manually to 'Admin' using DB
import pyodbc
conn_str = "Driver={ODBC Driver 17 for SQL Server};Server=tcp:ahmed-app-sql-server.database.windows.net,1433;Database=myappdb;Uid=tour123;Pwd=Ahmed123@;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;"
conn = pyodbc.connect(conn_str)
cursor = conn.cursor()
cursor.execute("UPDATE Users SET Role = 'Admin' WHERE Email = 'testadmin2@travai.com'")
conn.commit()
print("Updated to Admin role.")

# Now login
url_login = "http://localhost:5210/api/Users/login"
login_data = json.dumps({"email": "testadmin2@travai.com", "password": "Password123!"}).encode('utf-8')
req_login = urllib.request.Request(url_login, data=login_data, headers={'Content-Type': 'application/json'})
try:
    with urllib.request.urlopen(req_login) as response:
        res = response.read().decode('utf-8')
        print(f"Login Success:")
        print(res)
except urllib.error.HTTPError as e:
    err = e.read().decode('utf-8')
    print(f"Login Failed: {e.code}")
    print(err)
