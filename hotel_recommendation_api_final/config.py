import os
from dotenv import load_dotenv

load_dotenv()

HOTEL_CSV_PATH = os.getenv("HOTEL_CSV_PATH", "hotels_with_address.csv")
