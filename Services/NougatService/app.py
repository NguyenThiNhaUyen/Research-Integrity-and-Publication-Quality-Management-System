import os
import re
import asyncio
import logging
import time
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
    processingTimeSeconds: Optional[float] = None


class PageOcrResult(BaseModel):
    page_number: int
    markdown: str = ""
    success: bool = False
    error: Optional[str] = None
    attempts: int = 0


TEMP_DIR = Path(os.environ.get("NOUGAT_TEMP_DIR", "/app/temp"))
LEGACY_BATCH_SIZE = os.environ.get("NOUGAT_BATCHSIZE")
BATCH_SIZE = int(os.environ.get("NOUGAT_BATCH_SIZE", LEGACY_BATCH_SIZE or default_batch_size()))
MAX_WORKERS = int(os.environ.get("NOUGAT_MAX_WORKERS", "2"))
MAX_RETRY = int(os.environ.get("NOUGAT_MAX_RETRY", "3"))
MODEL_TAG = os.environ.get("NOUGAT_MODEL", "0.1.0-small")
USE_CUDA = os.environ.get("NOUGAT_USE_CUDA", "false").lower() in {"1", "true", "yes", "on"}

app = FastAPI(title="RIPQMS Nougat OCR Service")
model: NougatModel | None = None
inference_semaphore: asyncio.Semaphore | None = None


@app.on_event("startup")
async def load_model() -> None:
    global model, BATCH_SIZE, inference_semaphore

    logger.info(
        "Loading Nougat model. model_tag=%s batch_size=%s max_workers=%s max_retry=%s temp_dir=%s",
        MODEL_TAG,
        BATCH_SIZE,
        MAX_WORKERS,
        MAX_RETRY,
        TEMP_DIR,
    )
    checkpoint = get_checkpoint(model_tag=MODEL_TAG)
    model = NougatModel.from_pretrained(checkpoint)
    cuda_enabled = USE_CUDA and torch.cuda.is_available()
    if USE_CUDA and not cuda_enabled:
        logger.warning("NOUGAT_USE_CUDA=true but CUDA is not available. Falling back to CPU.")
    model = move_to_device(model, cuda=cuda_enabled)
    if BATCH_SIZE <= 0:
        BATCH_SIZE = 1
    model.eval()
    inference_semaphore = asyncio.Semaphore(MAX_WORKERS)
    TEMP_DIR.mkdir(parents=True, exist_ok=True)
    logger.info(
        "Nougat model loaded successfully. model_tag=%s batch_size=%s max_workers=%s max_retry=%s temp_dir=%s",
        MODEL_TAG,
        BATCH_SIZE,
        MAX_WORKERS,
        MAX_RETRY,
        TEMP_DIR,
    )


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/nougat/convert", response_model=NougatConvertResponse)
async def convert(file: UploadFile = File(...)) -> NougatConvertResponse:
    request_id = uuid4().hex
    started_at = time.perf_counter()
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

        markdown, pages = await convert_pdf_to_markdown(pdf_bytes, request_id=request_id)
        processing_time = round(time.perf_counter() - started_at, 3)
        logger.info(
            "Nougat convert OCR completed. request_id=%s filename=%s pages=%s markdown_length=%s processing_time_seconds=%s",
            request_id,
            file.filename,
            pages,
            len(markdown),
            processing_time,
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
                processingTimeSeconds=processing_time,
            )

        return NougatConvertResponse(
            success=True,
            markdown=markdown,
            pages=pages,
            processingTimeSeconds=processing_time,
        )
    except Exception as exc:
        processing_time = round(time.perf_counter() - started_at, 3)
        logger.exception("Nougat conversion failed. request_id=%s filename=%s", request_id, file.filename)
        return NougatConvertResponse(success=False, error=str(exc), processingTimeSeconds=processing_time)


@app.post("/api/nougat/convert-page", response_model=NougatConvertResponse)
async def convert_page(file: UploadFile = File(...)) -> NougatConvertResponse:
    request_id = uuid4().hex
    started_at = time.perf_counter()
    logger.info(
        "Nougat convert-page request received. request_id=%s filename=%s content_type=%s",
        request_id,
        file.filename,
        file.content_type,
    )

    if file.content_type not in {"application/pdf", "application/x-pdf"}:
        logger.warning(
            "Nougat convert-page rejected non-PDF file. request_id=%s filename=%s content_type=%s",
            request_id,
            file.filename,
            file.content_type,
        )
        return NougatConvertResponse(success=False, error="Only PDF files are supported.")

    try:
        pdf_bytes = await file.read()
        logger.info(
            "Nougat convert-page file read complete. request_id=%s filename=%s bytes=%s",
            request_id,
            file.filename,
            len(pdf_bytes),
        )
        if not pdf_bytes:
            logger.warning("Nougat convert-page rejected empty PDF. request_id=%s filename=%s", request_id, file.filename)
            return NougatConvertResponse(success=False, error="Uploaded PDF is empty.")

        markdown, pages = await convert_pdf_to_markdown(pdf_bytes, request_id=request_id)
        processing_time = round(time.perf_counter() - started_at, 3)
        if pages != 1:
            logger.warning(
                "Nougat convert-page received PDF with unexpected page count. request_id=%s filename=%s pages=%s",
                request_id,
                file.filename,
                pages,
            )

        if not markdown.strip():
            return NougatConvertResponse(
                success=False,
                pages=pages,
                error="Nougat returned empty markdown.",
                processingTimeSeconds=processing_time,
            )

        return NougatConvertResponse(
            success=True,
            markdown=markdown,
            pages=pages,
            processingTimeSeconds=processing_time,
        )
    except Exception as exc:
        processing_time = round(time.perf_counter() - started_at, 3)
        logger.exception("Nougat convert-page failed. request_id=%s filename=%s", request_id, file.filename)
        return NougatConvertResponse(success=False, error=str(exc), processingTimeSeconds=processing_time)


async def convert_pdf_to_markdown(pdf_bytes: bytes, request_id: str) -> tuple[str, int]:
    if model is None:
        raise RuntimeError("Nougat model is not loaded.")

    started_at = time.perf_counter()
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
        logger.info("Nougat total pages. request_id=%s total_pages=%s", request_id, dataset.size)
        if dataset.size == 0:
            return "", 0

        logger.info("Creating Nougat dataloader. request_id=%s batch_size=%s", request_id, BATCH_SIZE)
        dataloader = torch.utils.data.DataLoader(
            dataset,
            batch_size=BATCH_SIZE,
            shuffle=False,
            collate_fn=LazyDataset.ignore_none_collate,
        )

        results: list[PageOcrResult] = []
        page_cursor = 1
        for sample, is_last_page in dataloader:
            batch_size = 0 if sample is None else len(sample)
            page_numbers = list(range(page_cursor, page_cursor + batch_size))
            page_cursor += batch_size
            results.extend(await run_nougat_batch(sample, page_numbers, request_id))

        failed_pages = [result.page_number for result in results if not result.success]
        if failed_pages:
            raise RuntimeError(f"OCR failed for page(s): {', '.join(map(str, failed_pages))}")

        markdown = merge_page_markdown(results)
        processing_time = round(time.perf_counter() - started_at, 3)
        logger.info(
            "Nougat markdown assembled. request_id=%s pages=%s markdown_length=%s processing_time_seconds=%s",
            request_id,
            dataset.size,
            len(markdown),
            processing_time,
        )
        return markdown, dataset.size
    finally:
        logger.info("Removing temporary PDF. request_id=%s path=%s", request_id, pdf_path)
        pdf_path.unlink(missing_ok=True)


async def run_nougat_batch(
    sample: torch.Tensor | None,
    page_numbers: list[int],
    request_id: str,
) -> list[PageOcrResult]:
    if model is None:
        raise RuntimeError("Nougat model is not loaded.")
    if inference_semaphore is None:
        raise RuntimeError("Nougat inference semaphore is not initialized.")
    if sample is None:
        logger.warning("Nougat dataloader returned empty sample. request_id=%s pages=%s", request_id, page_numbers)
        return [
            PageOcrResult(
                page_number=page_number,
                success=False,
                error="Nougat dataloader returned empty sample.",
                attempts=MAX_RETRY,
            )
            for page_number in page_numbers
        ]

    for page_number in page_numbers:
        logger.info("Nougat page started. request_id=%s pageNumber=%s", request_id, page_number)

    last_error: Exception | None = None
    for attempt in range(1, MAX_RETRY + 1):
        try:
            async with inference_semaphore:
                model_output = await asyncio.to_thread(
                    model.inference,
                    image_tensors=sample,
                    early_stopping=True,
                )

            predictions_batch = model_output.get("predictions", [])
            repeats_batch = model_output.get("repeats", [])
            logger.info(
                "Nougat inference batch completed. request_id=%s pages=%s predictions=%s repeats=%s attempt=%s",
                request_id,
                page_numbers,
                len(predictions_batch),
                len(repeats_batch),
                attempt,
            )

            results: list[PageOcrResult] = []
            for index, page_number in enumerate(page_numbers):
                if index >= len(predictions_batch):
                    results.append(
                        PageOcrResult(
                            page_number=page_number,
                            success=False,
                            error="Nougat did not return a prediction for this page.",
                            attempts=attempt,
                        )
                    )
                    continue

                markdown = normalize_prediction(
                    output=predictions_batch[index],
                    repeats=repeats_batch[index] if index < len(repeats_batch) else None,
                    page_number=page_number,
                )
                results.append(
                    PageOcrResult(
                        page_number=page_number,
                        markdown=markdown,
                        success=True,
                        attempts=attempt,
                    )
                )
                logger.info(
                    "Nougat page completed. request_id=%s pageNumber=%s markdown_length=%s attempt=%s",
                    request_id,
                    page_number,
                    len(markdown),
                    attempt,
                )

            return results
        except Exception as exc:
            last_error = exc
            for page_number in page_numbers:
                logger.warning(
                    "Nougat page failed. request_id=%s pageNumber=%s attempt=%s max_retry=%s error=%s",
                    request_id,
                    page_number,
                    attempt,
                    MAX_RETRY,
                    exc,
                )

    return [
        PageOcrResult(
            page_number=page_number,
            success=False,
            error=str(last_error) if last_error else "Unknown Nougat inference error.",
            attempts=MAX_RETRY,
        )
        for page_number in page_numbers
    ]


def normalize_prediction(output: str, repeats: Optional[int], page_number: int) -> str:
    if output.strip() == "[MISSING_PAGE_POST]":
        return f"\n\n[MISSING_PAGE_EMPTY:{page_number}]\n\n"

    if repeats is not None:
        if repeats > 0:
            return f"\n\n[MISSING_PAGE_FAIL:{page_number}]\n\n"
        return f"\n\n[MISSING_PAGE_EMPTY:{page_number}]\n\n"

    return markdown_compatible(output)


def merge_page_markdown(results: list[PageOcrResult]) -> str:
    markdown = "\n\n".join(
        result.markdown.strip()
        for result in sorted(results, key=lambda item: item.page_number)
        if result.success
    ).strip()
    return re.sub(r"\n{3,}", "\n\n", markdown).strip()
