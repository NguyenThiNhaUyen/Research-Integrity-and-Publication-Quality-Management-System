# Nougat OCR Service

FastAPI microservice for converting uploaded PDF files to Markdown with `nougat-ocr`.

## Local Setup

```bash
pip install -r requirements.txt
uvicorn app:app --host 0.0.0.0 --port 8001
```

The service exposes:

```text
POST /api/nougat/convert
```

## Docker Compose

From the repository root:

```bash
docker compose up -d
```

Test directly:

```bash
curl -X POST \
  http://localhost:8001/api/nougat/convert \
  -F "file=@sample.pdf"
```

## Production Notes

- The ASP.NET API reaches this service at `http://nougat-service:8001` inside Docker Compose.
- Use `NOUGAT_MODEL` to select the Nougat model tag. Default: `0.1.0-small`.
- Use `NOUGAT_BATCHSIZE=0` for CPU-only execution. Positive values enable CUDA when available.
- Mount a persistent or disposable temp volume at `/app/temp` for rendered PDF pages.
- For GPU deployments, switch to a CUDA-capable Python/PyTorch image and configure the container runtime accordingly.
