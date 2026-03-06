import json
import time
import uuid
from pathlib import Path

import requests

API = "http://127.0.0.1:7865"
OUT = Path("C:/Users/Philipp/Documents/Fooocus_win64_2-5-0/Fooocus/outputs")


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

def build_data67():
    dep67 = cfg["dependencies"][67]
    data = []
    for cid in dep67["inputs"]:
        comp = components.get(cid, {})
        label = str(comp.get("props", {}).get("label", "")).lower()
        elem = str(comp.get("props", {}).get("elem_id", "")).lower()
        ctype = str(comp.get("type", "")).lower()
        val = default_for_component(comp)

        if elem == "positive_prompt":
            val = "pixel art zen gardener symbol with rake and leaf, Kyoto garden aesthetic, calm green palette, 64x64 pixel art"
        elif elem == "negative_prompt":
            val = "text, watermark, logo, blurry, low quality, jpeg artifacts"
        elif ctype == "checkboxgroup" and "selected styles" in label:
            val = ["Sai Pixel Art"]
        elif label == "image number":
            val = 1
        elif label == "random":
            val = False
        elif label == "seed":
            val = "12345"

        data.append(val)
    return data

before = {str(p.resolve()) for p in OUT.rglob("*.png")}
session_hash = uuid.uuid4().hex[:12]
url = f"{API}/api/predict"

for fn_index, data in [(73, []), (65, []), (66, [False, "12345"]), (67, build_data67())]:
    r = requests.post(url, json={"fn_index": fn_index, "data": data, "session_hash": session_hash}, timeout=1200)
    print("fn", fn_index, "status", r.status_code)
    r.raise_for_status()
    jr = r.json()
    print(" keys", list(jr.keys()), "is_generating", jr.get("is_generating"))

saw_true = False
last = None
for i in range(2000):
    r = requests.post(url, json={"fn_index": 68, "data": [None], "session_hash": session_hash}, timeout=1200)
    r.raise_for_status()
    jr = r.json()
    last = jr
    is_gen = bool(jr.get("is_generating", False))
    if is_gen:
        saw_true = True
    if saw_true and not is_gen:
        print("done after", i)
        break
    if i % 20 == 0:
        print("poll", i, "is_generating", is_gen)
    time.sleep(0.2)

Path("foocus_api/test_chain_last.json").write_text(json.dumps(last, indent=2, default=str), encoding="utf-8")

after = [p for p in OUT.rglob("*.png") if str(p.resolve()) not in before]
print("new_png_count", len(after))
if after:
    newest = max(after, key=lambda p: p.stat().st_mtime)
    print("newest", newest)
print("final_data", json.dumps((last or {}).get("data"), default=str)[:800])
