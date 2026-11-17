import os
import sys
import uvicorn

# Ensure backend folder is in path
sys.path.insert(0, os.path.dirname(__file__))

if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8000))
    host = os.environ.get("HOST", "0.0.0.0")
    print(f"Starting Immersive Anatomy Lab AI Backend on http://{host}:{port} ...")
    uvicorn.run("main:app", host=host, port=port, log_level="info")
