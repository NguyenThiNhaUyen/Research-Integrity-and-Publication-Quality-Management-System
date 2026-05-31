import json
import urllib.request

with urllib.request.urlopen("http://127.0.0.1:8001/health", timeout=5) as response:
    body = json.loads(response.read().decode("utf-8"))

if response.status != 200 or body.get("status") != "ok":
    raise RuntimeError(f"Nougat health check failed: status={response.status}, body={body}")

print("Nougat health check passed")
