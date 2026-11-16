import uvicorn
import os
import sys

# Ensure current folder is in path
sys.path.insert(0, os.path.dirname(__file__))

if __name__ == "__main__":
    print("Starting Immersive Anatomy Lab AI Backend on http://127.0.0.1:8000 ...")
    uvicorn.run("main:app", host="127.0.0.1", port=8000, log_level="info")
