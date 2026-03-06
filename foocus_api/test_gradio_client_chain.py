import json
from pathlib import Path

import requests
from gradio_client import Client

API = "http://127.0.0.1:7865"


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

client = Client(API)
client.predict(fn_index=73)
state_result = client.predict(*inputs67, fn_index=67)
Path("foocus_api/debug_client_67.json").write_text(json.dumps(state_result, indent=2, default=str), encoding="utf-8")

out68 = client.submit(None, fn_index=68).result()
Path("foocus_api/debug_client_68.json").write_text(json.dumps(out68, indent=2, default=str), encoding="utf-8")
print(type(out68), len(out68) if hasattr(out68, '__len__') else 'na')
print(out68)
