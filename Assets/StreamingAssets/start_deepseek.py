import os
import threading
import json
from flask import Flask, request, jsonify, make_response
from transformers import AutoModelForCausalLM, AutoTokenizer
import torch

app = Flask(__name__)

# Флаг, указывающий, что сервер готов к работе
server_running = False

# Путь к модели
MODEL_PATH = os.path.join(os.path.dirname(__file__), "DeepSeek")

try:
    # Загружаем модель и токенизатор
    tokenizer = AutoTokenizer.from_pretrained(MODEL_PATH)
    model = AutoModelForCausalLM.from_pretrained(MODEL_PATH).to("cuda" if torch.cuda.is_available() else "cpu")
    server_running = True  # Устанавливаем флаг, если загрузка прошла успешно
    print("Модель успешно загружена. Флаг server_running установлен в True.")
except Exception as e:
    print("Ошибка загрузки модели:", e)

# Эндпоинт для проверки статуса сервера
@app.route("/status", methods=["GET"])
def status():
    return jsonify({"ServerRunning": server_running})

@app.route("/generate", methods=["POST"])
def generate():
    try:
        print("Получен запрос на генерацию.")
        print(f"Заголовки запроса: {request.headers}")
        print(f"Raw данные запроса: {request.data}")
        
        data = request.get_json()
        if data is None:
            print("Не удалось декодировать JSON.")
            return make_response(jsonify({"error": "Invalid JSON format"}), 400)
        
        prompt = data.get("Prompt", "")
        if not prompt:
            print("Промт пустой.")
            return make_response(jsonify({"error": "Empty prompt"}), 400)
        print(f"Получен промт: {prompt}")

        inputs = tokenizer(prompt, return_tensors="pt").to(model.device)
        prompt_length = inputs["input_ids"].shape[1]
        max_length = prompt_length + 1000

        with torch.no_grad():
            outputs = model.generate(
                inputs["input_ids"],
                max_length=max_length,
                temperature=0.7,
                top_k=50,
                top_p=0.9,
                do_sample=True
            )

        response_text = tokenizer.decode(outputs[0][prompt_length:], skip_special_tokens=True)
        print(f"Сгенерированный ответ: {response_text}")

        response_data = json.dumps({"response": response_text}, ensure_ascii=False)
        response = make_response(response_data, 200)
        response.headers["Content-Type"] = "application/json; charset=utf-8"
        print("Ответ отправлен клиенту.")
        return response

    except json.JSONDecodeError as e:
        print(f"Ошибка на сервере: {e}")
        return make_response(jsonify({"error": "Invalid JSON"}), 400)
    except Exception as e:
        print(f"Ошибка на сервере: {e}")
        return make_response(jsonify({"error": str(e)}), 500)

# Функция для graceful shutdown сервера с fallback
def shutdown_server():
    func = request.environ.get('werkzeug.server.shutdown')
    if func is None:
        # Выводим отладочную информацию о доступных ключах
        print("Ключ 'werkzeug.server.shutdown' не найден в request.environ. Доступные ключи:", list(request.environ.keys()))
        print("Завершаем работу сервера с помощью os._exit(0).")
        os._exit(0)
    else:
        func()

# Эндпоинт для graceful shutdown, вызывающий shutdown_server в отдельном потоке
@app.route("/shutdown", methods=["POST"])
def shutdown():
    threading.Thread(target=shutdown_server).start()
    return jsonify({"message": "Сервер завершает работу..."}), 200

if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000, debug=False)