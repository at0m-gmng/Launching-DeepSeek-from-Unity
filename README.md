# Local launch of the DeepSeek model from Unity

## Preparing the application for launch

1) Download the AI model DeepSeek from the link:
   https://huggingface.co/unsloth/DeepSeek-R1-Distill-Llama-8B/tree/main
2) Place the downloaded files in StreammingAsssets/DeepSeek
3) Launch the Editor Application (at the moment the build is not supported)

## How does this work?

1) First, the system will check for the presence of the Python file to be installed. If it is not found, the installer will be downloaded and launched (and deleted after successful installation).
2) If Python is successfully installed, the presence of DeepSeek model files is checked. It can also be downloaded if you have your own host and can place the archive file with the model on it. But downloading manually may be faster.
3) If the model files are found, then we can start the local server. But before that, we need to install the necessary Python dependencies for this.
4) If the dependencies are installed successfully, we launch the local Flask server and wait for it to complete its launch.
5) Once the server has started, we can communicate with the model.

## Note:
- This is an experimental project.
- The model is not configured, since the main task was to launch the model from Unity. On weak devices, answers to even banal questions are generated for up to 10 minutes. The answer includes "DeepThink", so if you change the number of tokens in the .py files, take this into account.
- At the time of writing the readme, the functionality is in the process of fixing errors.
- If you have any edits or suggestions, you can contact me and we will make this project better.