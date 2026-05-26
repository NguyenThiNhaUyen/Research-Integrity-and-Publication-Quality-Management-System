# OCR S3 Test Cases

## Required AWS Permissions

- `sts:GetCallerIdentity`
- `s3:GetObject`
- `s3:GetObjectVersion`
- `textract:StartDocumentTextDetection`
- `textract:GetDocumentTextDetection`

If the S3 object is encrypted with SSE-KMS, the backend IAM user/role also needs:

- `kms:Decrypt`
- `kms:DescribeKey`

Set `Aws:ExpectedCallerArn` or `AWS_EXPECTED_CALLER_ARN` to the IAM user/role ARN that should be used by the backend. If STS returns a different ARN, the API returns `Backend is using different AWS credentials.`

## Test Cases

| Case | Steps | Expected Result |
|---|---|---|
| Upload PDF successfully | Call `POST /api/uploads` with `file` PDF and `type=PAPER_VERSION` | Response contains `fileId`, `url`, `key`, `s3Bucket`, `s3Key`, `contentType=application/pdf` |
| Save S3 metadata to uploaded file | Check database table `uploaded_files` after upload | `file_key` and `s3_key` contain folder/key only, `s3_bucket` contains bucket name, `url` contains full S3 URL |
| Attach file to paper version | Call `POST /api/papers/{paperId}/versions` with `fileId` | Paper version is created and `papers.s3_key` is updated from uploaded file metadata |
| Textract uses bucket and key | Call `POST /api/ocr/extract-text` with `fileId` | Log shows `bucket`, `key`, `region`, `fileType`; Textract receives bucket name and folder/key only |
| Wrong key | Change `uploaded_files.s3_key` or `file_key` to an invalid value, then call OCR | API returns clear S3 object metadata error instead of generic 500 |
| Wrong region or permission | Configure wrong AWS region or remove `s3:GetObject` permission, then call OCR | API returns region/permission-specific S3 metadata error |
| Wrong backend credentials | Set `AWS_EXPECTED_CALLER_ARN` to the intended IAM ARN and run OCR with different credentials | API returns `Backend is using different AWS credentials.` and logs actual ARN/account/userId |
| SSE-KMS object | Upload or select a PDF encrypted with SSE-KMS | Logs KMS hint; caller must have `kms:Decrypt` and `kms:DescribeKey` |

## Correct Textract Input

Textract must receive:

```text
Bucket = bucket-name
Name   = folder/file.pdf
```

Textract must not receive:

```text
Name = https://bucket.s3.region.amazonaws.com/folder/file.pdf
```
