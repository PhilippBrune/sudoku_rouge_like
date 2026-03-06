import json
from pathlib import Path

cfg = json.loads(Path("foocus_api/gradio_config.json").read_text(encoding="utf-8"))
components = {c["id"]: c for c in cfg["components"] if "id" in c}

print("dependency_count=", len(cfg["dependencies"]))

for idx, dep in enumerate(cfg["dependencies"]):
    outs = dep.get("outputs", [])
    labels = []
    has_gallery_output = False
    for oid in outs:
        comp = components.get(oid, {})
        ctype = str(comp.get("type", "")).lower()
        label = str(comp.get("props", {}).get("label", ""))
        elem_id = str(comp.get("props", {}).get("elem_id", ""))
        labels.append((oid, ctype, label, elem_id))
        if ctype == "gallery" or "finished images" in label.lower() or elem_id == "final_gallery":
            has_gallery_output = True

    is_generate_click = 15 in dep.get("targets", []) and dep.get("trigger") == "click"
    if not (has_gallery_output or is_generate_click):
        continue

    ins = dep.get("inputs", [])
    in_meta = []
    for iid in ins:
        comp = components.get(iid, {})
        in_meta.append(
            (
                iid,
                str(comp.get("type", "")).lower(),
                str(comp.get("props", {}).get("label", "")),
                str(comp.get("props", {}).get("elem_id", "")),
            )
        )

    print("idx=", idx, "targets=", dep.get("targets"), "trigger=", dep.get("trigger"), "queue=", dep.get("queue"), "inputs=", len(ins))
    print(" outputs:", labels)
    print(" first inputs:", in_meta[:25])
    print("-")
