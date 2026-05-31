import os
import re
import logging
from functools import partial
from pathlib import Path
from typing import Optional
from uuid import uuid4

import torch
from fastapi import FastAPI, File, UploadFile
from nougat import NougatModel
from nougat.postprocessing import markdown_compatible
from nougat.utils.checkpoint import get_checkpoint
from nougat.utils.dataset import LazyDataset
from nougat.utils.device import default_batch_size, move_to_device
from pydantic import BaseModel

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("ripqms-nougat")


class NougatConvertResponse(BaseModel):
    success: bool
    markdown: Optional[str] = None
    pages: Optional[int] = None
    error: Optional[str] = None


TEMP_DIR = Path(os.environ.get("NOUGAT_TEMP_DIR", "/app/temp"))
BATCH_SIZE = int(os.environ.get("NOUGAT_BATCHSIZE", default_batch_size()))
MODEL_TAG = os.environ.get("NOUGAT_MODEL", "0.1.0-small")

app = FastAPI(title="RIPQMS Nougat OCR Service")
model: NougatModel | None = None


@app.on_event("startup")
async def load_model() -> None:
    global model, BATCH_SIZE

    logger.info("Loading Nougat model. model_tag=%s batch_size=%s temp_dir=%s", MODEL_TAG, BATCH_SIZE, TEMP_DIR)
    checkpoint = get_checkpoint(model_tag=MODEL_TAG)
    model = NougatModel.from_pretrained(checkpoint)
    model = move_to_device(model, cuda=BATCH_SIZE > 0)
    if BATCH_SIZE <= 0:
        BATCH_SIZE = 1
    model.eval()
    TEMP_DIR.mkdir(parents=True, exist_ok=True)
    logger.info("Nougat model loaded successfully. model_tag=%s batch_size=%s temp_dir=%s", MODEL_TAG, BATCH_SIZE, TEMP_DIR)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/nougat/convert", response_model=NougatConvertResponse)
async def convert(file: UploadFile = File(...)) -> NougatConvertResponse:
    request_id = uuid4().hex
    logger.info(
        "Nougat convert request received. request_id=%s filename=%s content_type=%s",
        request_id,
        file.filename,
        file.content_type,
    )

    if file.content_type not in {"application/pdf", "application/x-pdf"}:
        logger.warning(
            "Nougat convert rejected non-PDF file. request_id=%s filename=%s content_type=%s",
            request_id,
            file.filename,
            file.content_type,
        )
        return NougatConvertResponse(success=False, error="Only PDF files are supported.")

    try:
        pdf_bytes = await file.read()
        logger.info(
            "Nougat convert file read complete. request_id=%s filename=%s bytes=%s",
            request_id,
            file.filename,
            len(pdf_bytes),
        )
        if not pdf_bytes:
            logger.warning("Nougat convert rejected empty PDF. request_id=%s filename=%s", request_id, file.filename)
            return NougatConvertResponse(success=False, error="Uploaded PDF is empty.")

        markdown, pages = convert_pdf_to_markdown(pdf_bytes, request_id=request_id)
        logger.info(
            "Nougat convert OCR completed. request_id=%s filename=%s pages=%s markdown_length=%s",
            request_id,
            file.filename,
            pages,
            len(markdown),
        )
        if not markdown.strip():
            logger.warning(
                "Nougat convert returned empty markdown. request_id=%s filename=%s pages=%s",
                request_id,
                file.filename,
                pages,
            )
            return NougatConvertResponse(
                success=False,
                pages=pages,
                error="Nougat returned empty markdown.",
            )

        return NougatConvertResponse(success=True, markdown=markdown, pages=pages)
    except Exception as exc:
        logger.exception("Nougat conversion failed. request_id=%s filename=%s", request_id, file.filename)
        return NougatConvertResponse(success=False, error=str(exc))


def convert_pdf_to_markdown(pdf_bytes: bytes, request_id: str) -> tuple[str, int]:
    if model is None:
        raise RuntimeError("Nougat model is not loaded.")

    pdf_path = TEMP_DIR / f"{uuid4().hex}.pdf"
    logger.info("Writing temporary PDF. request_id=%s path=%s bytes=%s", request_id, pdf_path, len(pdf_bytes))
    pdf_path.write_bytes(pdf_bytes)

    try:
        logger.info("Creating Nougat LazyDataset. request_id=%s path=%s", request_id, pdf_path)
        dataset = LazyDataset(
            pdf_path,
            partial(model.encoder.prepare_input, random_padding=False),
        )
        logger.info("Nougat LazyDataset created. request_id=%s pages=%s", request_id, dataset.size)
        if dataset.size == 0:
            return "", 0

        logger.info("Creating Nougat dataloader. request_id=%s batch_size=%s", request_id, BATCH_SIZE)
        dataloader = torch.utils.data.DataLoader(
            dataset,
            batch_size=BATCH_SIZE,
            shuffle=False,
            collate_fn=LazyDataset.ignore_none_collate,
        )

        predictions: list[str] = []
        page_num = 0
        for sample, is_last_page in dataloader:
            if sample is None:
                logger.warning("Nougat dataloader returned empty sample. request_id=%s", request_id)
                continue

            logger.info("Running Nougat inference batch. request_id=%s current_page=%s", request_id, page_num + 1)
            model_output = model.inference(image_tensors=sample, early_stopping=True)
            predictions_batch = model_output.get("predictions", [])
            repeats_batch = model_output.get("repeats", [])
            logger.info(
                "Nougat inference batch completed. request_id=%s predictions=%s repeats=%s",
                request_id,
                len(predictions_batch),
                len(repeats_batch),
            )
            for index, output in enumerate(predictions_batch):
                page_num += 1
                if output.strip() == "[MISSING_PAGE_POST]":
                    predictions.append(f"\n\n[MISSING_PAGE_EMPTY:{page_num}]\n\n")
                elif index < len(repeats_batch) and repeats_batch[index] is not None:
                    if repeats_batch[index] > 0:
                        predictions.append(f"\n\n[MISSING_PAGE_FAIL:{page_num}]\n\n")
                    else:
                        predictions.append(f"\n\n[MISSING_PAGE_EMPTY:{page_num}]\n\n")
                else:
                    predictions.append(markdown_compatible(output))

                is_last = index < len(is_last_page) and bool(is_last_page[index])
                if is_last:
                    markdown = "".join(predictions).strip()
                    markdown = re.sub(r"\n{3,}", "\n\n", markdown).strip()
                    logger.info(
                        "Nougat markdown assembled at last page. request_id=%s pages=%s markdown_length=%s",
                        request_id,
                        dataset.size,
                        len(markdown),
                    )
                    return markdown, dataset.size

        markdown = "".join(predictions).strip()
        markdown = re.sub(r"\n{3,}", "\n\n", markdown).strip()
        logger.info(
            "Nougat markdown assembled after dataloader ended. request_id=%s pages=%s markdown_length=%s",
            request_id,
            dataset.size,
            len(markdown),
        )
        return markdown, dataset.size
    finally:
        logger.info("Removing temporary PDF. request_id=%s path=%s", request_id, pdf_path)
        pdf_path.unlink(missing_ok=True)
