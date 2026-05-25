# Upload va Paper Version Flow

## Muc tieu

He thong tach rieng hai viec:

- Upload file len storage.
- Tao paper version chinh thuc tu file da upload.

Dieu nay giup tranh viec moi file upload len deu tu dong tro thanh version cua paper.

## Khai niem

### UploadedFile

`UploadedFile` la metadata cua file da duoc upload len S3.

Upload thanh cong chi co nghia la file da ton tai trong he thong. Upload thanh cong chua co nghia la da tao paper version.

### PaperVersion

`PaperVersion` la mot version chinh thuc cua paper. No chi duoc tao khi user xac nhan attach mot `UploadedFile` vao paper.

## Flow dung

```text
1. User chon file
2. FE goi POST /api/uploads voi file + type
3. BE upload file len S3 va tao UploadedFile
4. BE tra ve fileId
5. FE hien thi thong tin file de user check lai
6. User bam Tao version / Save version
7. FE goi POST /api/papers/{paperId}/versions voi fileId, versionName, changeLog
8. BE tao PaperVersion tu UploadedFile metadata
```

## Rule business

```text
UploadedFile != PaperVersion
```

Mot file upload thanh cong chi tro thanh paper version khi duoc attach vao paper bang API:

```http
POST /api/papers/{paperId}/versions
```

Do do:

- Moi lan upload khong mac dinh tao version.
- Moi lan create paper version se tao mot version moi.
- PaperVersionController khong upload file truc tiep.
- UploadController khong biet paper domain.
