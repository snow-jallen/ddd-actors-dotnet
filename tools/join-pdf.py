#!/usr/bin/env python3
# /// script
# requires-python = ">=3.9"
# dependencies = ["pypdf>=5"]
# ///
"""Join PDFs, keeping each input's outline (bookmarks): join-pdf.py OUT IN1 IN2 ..."""
import sys
from pypdf import PdfWriter

out, *inputs = sys.argv[1:]
writer = PdfWriter()
for path in inputs:
    writer.append(path, import_outline=True)
writer.page_mode = "/UseOutlines"
with open(out, "wb") as f:
    writer.write(f)
