import base64
import json
import uuid
from pathlib import Path

import requests

API = "http://127.0.0.1:7865"


def b64_to_bytes(s: str):
    if s.startswith("data:image") and "," in s:
        s = s.split(",", 1)[1]
    try:
        return base64.b64decode(s, validate=True)
    except Exception:
        return None


def default_for_component(comp):
    props = comp.get("props", {})
    if "value" in props:
        return props["value"]
    ctype = str(comp.get("type", "")).lower()
    if ctype in {"textbox", "markdown", "html"}:
        return ""
    if ctype == "checkbox":
        return False
    if ctype == "checkboxgroup":
        return []
    if ctype in {"slider", "number"}:
        return 0
    if ctype in {"dropdown", "radio"}:
        choices = props.get("choices") or []
        if choices:
            first = choices[0]
            if isinstance(first, list) and first:
                return first[0]
            return first
        return None
    return None


cfg = requests.get(f"{API}/config", timeout=30).json()
components = {c["id"]: c for c in cfg["components"] if "id" in c}
d67 = cfg["dependencies"][67]
d68 = cfg["dependencies"][68]

inputs67 = []
for cid in d67["inputs"]:
    comp = components.get(cid, {})
    label = str(comp.get("props", {}).get("label", "")).lower()
    elem = str(comp.get("props", {}).get("elem_id", "")).lower()
    val = default_for_component(comp)

    if elem == "positive_prompt" or ("prompt" in label and "negative" not in label):
        val = "pixel art icon, bonsai tree, zen garden, clean silhouette, 64x64"
    elif elem == "negative_prompt" or ("negative" in label and "prompt" in label):
        val = "text, watermark, logo, blurry, low quality"
    elif label == "image number":
        val = 1
    elif label == "seed":
        val = "12345"
    elif label == "random":
        val = False

    inputs67.append(val)

payload67 = {"fn_index": 67, "data": inputs67}
session_hash = uuid.uuid4().hex[:12]

r73 = requests.post(
    f"{API}/api/predict",
    json={"fn_index": 73, "data": [], "session_hash": session_hash},
    timeout=3600,
)
r73.raise_for_status()
Path("foocus_api/debug_73.json").write_text(json.dumps(r73.json(), indent=2), encoding="utf-8")

payload67["session_hash"] = session_hash
r67 = requests.post(f"{API}/api/predict", json=payload67, timeout=3600)
r67.raise_for_status()
out67 = r67.json()
Path("foocus_api/debug_67.json").write_text(json.dumps(out67, indent=2), encoding="utf-8")

state = out67.get("data", [None])[0]
payload68 = {"fn_index": 68, "data": [state]}
payload68["session_hash"] = session_hash
last = None
for step in range(1800):
    r68 = requests.post(f"{API}/api/predict", json=payload68, timeout=3600)
    r68.raise_for_status()
    out68 = r68.json()
    last = out68
    if not out68.get("is_generating", False):
        break

if last is None:
    raise RuntimeError("No response from fn_index 68 polling")

Path("foocus_api/debug_68.json").write_text(json.dumps(last, indent=2), encoding="utf-8")

saved = 0
for item in last.get("data", []):
    if isinstance(item, list):
        for row in item:
            if isinstance(row, list) and row:
                first = row[0]
                if isinstance(first, dict) and isinstance(first.get("data"), str):
                    b = b64_to_bytes(first["data"])
                    if b:
                        Path("docs/icons_foocus/test_single.png").write_bytes(b)
                        saved += 1
                        break

print("saved", saved)
