#!/usr/bin/env python3
"""
Post-process the Smithy-generated OpenAPI JSON with patches that Smithy
cannot express natively.

Storage patches:
  1. UploadObject / UpdateObject requestBody: inject multipart/form-data schema.
     Smithy has no native multipart/form-data support. The @httpMultipartForm
     trait in the model documents intent; this script performs the actual injection.
  2. UploadChunk body: format: byte → format: binary.
     (@streaming blob translates to format:byte but swift-openapi-generator
      needs format:binary to emit HTTPBody instead of Base64EncodedData)

Database patches:
  3. FilterOperator enum — defined in Smithy but not referenced as a member
     type (filter map values are raw strings), so it is absent from the
     generated output. Injected here so OpenAPI-based generators emit it.
"""
import json
import sys

path = sys.argv[1] if len(sys.argv) > 1 else "output/openapi/StorageService.openapi.json"

with open(path) as f:
    d = json.load(f)

service_title = d.get("info", {}).get("title", "")

# ── Patch 3: FilterOperator enum (DatabaseService only) ───────────────────
if service_title == "Supabase Database API":
    d["components"]["schemas"]["FilterOperator"] = {
        "type": "string",
        "description": (
            "PostgREST column filter operators. "
            "Format a filter value as \"{operator}.{value}\", e.g. \"eq.5\". "
            "Prefix with \"not.\" to negate: \"not.eq.5\". "
            "For logical grouping use keys \"or\" / \"and\" in the filters map."
        ),
        "enum": [
            "eq", "neq", "lt", "lte", "gt", "gte",
            "like", "ilike", "match", "imatch",
            "is", "isdistinct", "in",
            "cs", "cd", "ov",
            "sl", "sr", "nxl", "nxr", "adj",
            "fts", "plfts", "phfts", "wfts",
        ],
    }
    with open(path, "w") as f:
        json.dump(d, f, indent=4)
    print(f"Patched (database): {path}")
    sys.exit(0)

# ── Patch 1: multipart/form-data for UploadObject and UpdateObject ────────
MULTIPART_BODY = {
    "required": True,
    "content": {
        "multipart/form-data": {
            "schema": {
                "type": "object",
                "required": ["file"],
                "properties": {
                    "file": {"type": "string", "format": "binary"},
                    "cacheControl": {"type": "string"},
                    "metadata": {"type": "object"},
                },
            }
        }
    },
}

upload_path = "/object/{bucketId}/{wildcardPath+}"
if upload_path in d.get("paths", {}):
    for method in ("post", "put"):
        if method in d["paths"][upload_path]:
            d["paths"][upload_path][method]["requestBody"] = MULTIPART_BODY

# ── Patch 2: streaming blob → binary ──────────────────────────────────────
schema = d["components"]["schemas"].get("UploadChunkInputPayload", {})
if schema.get("format") == "byte":
    schema["format"] = "binary"

with open(path, "w") as f:
    json.dump(d, f, indent=2)

print(f"Patched (storage): {path}")
