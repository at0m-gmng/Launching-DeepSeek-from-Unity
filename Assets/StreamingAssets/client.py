import requests
import json
import sys

API_URL = "http://127.0.0.1:5000/generate"
TIMEOUT = 10  # Timeout in seconds

def send_request(prompt):
    payload = {"prompt": prompt}
    headers = {"Content-Type": "application/json; charset=utf-8"}

    try:
        print(f"Send a request to {API_URL}: {json.dumps(payload, ensure_ascii=False)}")
        
        # We use json=payload to let the requests library serialize the data itself
        response = requests.post(API_URL, json=payload, headers=headers)

        print(f"HTTP response code: {response.status_code}")
        print("Server response:", response.text)

        response.raise_for_status()  # HTTP Error Checking

        data = response.json()  # Parsing the JSON response
        print("Reply from the model:", data.get("response", "Error in answer"))

    except requests.exceptions.Timeout:
        print(f"⏳ The server did not respond in {TIMEOUT} seconds. Try again later.")
    except requests.exceptions.RequestException as e:
        print("❌ Error while requesting:", e)
    except KeyboardInterrupt:
        print("\n🚪 The program has been terminated by the user.")
        sys.exit(0)  # We terminate the program correctly

if __name__ == "__main__":
    try:
        user_prompt = input("Enter your model request: ")
        send_request(user_prompt)
    except KeyboardInterrupt:
        print("\n🚪 The program has been terminated by the user.")
        sys.exit(0)