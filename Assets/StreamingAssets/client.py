import requests
import json
import sys

API_URL = "http://127.0.0.1:5000/generate"
TIMEOUT = 10  # Таймаут в секундах

def send_request(prompt):
    payload = {"prompt": prompt}  # Просто передаем строку, она уже в нужной кодировке
    headers = {"Content-Type": "application/json; charset=utf-8"}

    try:
        # Логируем отправляемые данные
        print(f"Отправляем запрос на {API_URL}: {json.dumps(payload, ensure_ascii=False)}")
        
        # Используем json=payload, чтобы библиотека requests сама сериализовала данные
        response = requests.post(API_URL, json=payload, headers=headers)

        # Логируем код ответа и сам ответ
        print(f"HTTP-код ответа: {response.status_code}")
        print("Ответ сервера:", response.text)

        response.raise_for_status()  # Проверка на ошибки HTTP

        data = response.json()  # Парсим JSON-ответ
        print("Ответ от модели:", data.get("response", "Ошибка в ответе"))

    except requests.exceptions.Timeout:
        print(f"⏳ Сервер не ответил за {TIMEOUT} секунд. Попробуйте позже.")
    except requests.exceptions.RequestException as e:
        print("❌ Ошибка при запросе:", e)
    except KeyboardInterrupt:
        print("\n🚪 Программа завершена пользователем.")
        sys.exit(0)  # Завершаем программу корректно

if __name__ == "__main__":
    try:
        user_prompt = input("Введите ваш запрос к модели: ")
        send_request(user_prompt)
    except KeyboardInterrupt:
        print("\n🚪 Программа завершена пользователем.")
        sys.exit(0)