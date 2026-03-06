import json
from pathlib import Path

cfg = json.loads(Path("foocus_api/gradio_config.json").read_text(encoding="utf-8"))
components = {c["id"]: c for c in cfg["components"] if "id" in c}

for idx, dep in enumerate(cfg["dependencies"]):
    outs = dep.get("outputs", [])
    if 1 in outs:
        print("idx", idx, "targets", dep.get("targets"), "trigger", dep.get("trigger"), "backend", dep.get("backend_fn"), "queue", dep.get("queue"), "inputs", dep.get("inputs"), "outputs", outs)
        print(" in labels", [(i, components.get(i, {}).get("type"), components.get(i, {}).get("props", {}).get("label"), components.get(i, {}).get("props", {}).get("elem_id")) for i in dep.get("inputs", [])[:10]])
        print("-")
