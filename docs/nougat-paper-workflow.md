# Nougat Paper Workflow

The fresh Paper workflow is centered on a single upload endpoint:

```text
POST /api/papers/upload
```

The backend flow is:

```text
Upload PDF
Save PDF to S3
Create Paper
Create PaperVersion
Call Nougat Service
Upload Markdown to S3
Update PaperVersion.MarkdownS3Key
Return PaperVersionResponse
```

The Nougat microservice runs at:

```text
http://nougat-service:8001
```

Inside Docker Compose, the ASP.NET API uses `Nougat__BaseUrl=http://nougat-service:8001`.
