import pyodbc

conn_str = "Driver={ODBC Driver 17 for SQL Server};Server=tcp:ahmed-app-sql-server.database.windows.net,1433;Database=myappdb;Uid=tour123;Pwd=Ahmed123@;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;"
conn = pyodbc.connect(conn_str)
cursor = conn.cursor()
cursor.execute("SELECT Email FROM Users WHERE Role = 'Admin'")
row = cursor.fetchone()
if row:
    print(f"Admin Email: {row[0]}")
else:
    print("No admin found.")
