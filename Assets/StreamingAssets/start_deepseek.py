import os
import threading
import json
from flask import Flask, request, jsonify, make_response
from transformers import AutoModelForCausalLM, AutoTokenizer
import torch

app = Flask(__name__)

# Flag indicating the server is ready
server_running = False

# Path to the model
MODEL_PATH = os.path.join(os.path.dirname(__file__), "DeepSeek")

try:
    # Load model and tokenizer
    tokenizer = AutoTokenizer.from_pretrained(MODEL_PATH)
    model = AutoModelForCausalLM.from_pretrained(MODEL_PATH).to("cuda" if torch.cuda.is_available() else "cpu")
    server_running = True  # Set flag if loading succeeded
    print("Model loaded successfully. server_running flag set to True.")
except Exception as e:
    print("Error loading model:", e)

# Endpoint to check server status
@app.route("/status", methods=["GET"])
def status():
    return jsonify({"ServerRunning": server_running})

@app.route("/generate", methods=["POST"])
def generate():
    try:
        print("Received generation request.")
        print(f"Request headers: {request.headers}")
        print(f"Raw request data: {request.data}")
        
        data = request.get_json()
        if data is None:
            print("Failed to decode JSON.")
            return make_response(jsonify({"error": "Invalid JSON format"}), 400)
        
        prompt = data.get("Prompt", "")
        if not prompt:
            print("Prompt is empty.")
            return make_response(jsonify({"error": "Empty prompt"}), 400)
        print(f"Received prompt: {prompt}")

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
        print(f"Generated response: {response_text}")

        response_data = json.dumps({"response": response_text}, ensure_ascii=False)
        response = make_response(response_data, 200)
        response.headers["Content-Type"] = "application/json; charset=utf-8"
        print("Response sent to client.")
        return response

    except json.JSONDecodeError as e:
        print(f"Server error: {e}")
        return make_response(jsonify({"error": "Invalid JSON"}), 400)
    except Exception as e:
        print(f"Server error: {e}")
        return make_response(jsonify({"error": str(e)}), 500)

# Graceful shutdown function with fallback
def shutdown_server():
    func = request.environ.get('werkzeug.server.shutdown')
    if func is None:
        # Output debug information about available keys
        print("Key 'werkzeug.server.shutdown' not found in request.environ. Available keys:", list(request.environ.keys()))
        print("Terminating server via os._exit(0).")
        os._exit(0)
    else:
        func()

# Graceful shutdown endpoint
@app.route("/shutdown", methods=["POST"])
def shutdown():
    threading.Thread(target=shutdown_server).start()
    return jsonify({"message": "Server is shutting down..."}), 200

if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000, debug=False)