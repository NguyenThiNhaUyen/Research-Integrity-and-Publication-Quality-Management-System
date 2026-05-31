import importlib.metadata

import pypdfium2
from nougat import NougatModel

if not hasattr(pypdfium2.PdfDocument, "render"):
    version = importlib.metadata.version("pypdfium2")
    raise RuntimeError(
        "pypdfium2 is incompatible with nougat-ocr==0.1.17: "
        f"PdfDocument.render() is missing. Installed pypdfium2={version}."
    )

print("Nougat loaded successfully")
