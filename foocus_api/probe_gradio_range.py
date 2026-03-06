import json
from pathlib import Path

cfg = json.loads(Path("foocus_api/gradio_config.json").read_text(encoding="utf-8"))
components = {c["id"]: c for c in cfg["components"] if "id" in c}

for idx in range(60, 76):
    dep = cfg["dependencies"][idx]
    ins = dep.get("inputs", [])
    outs = dep.get("outputs", [])
    print("IDX", idx, "targets", dep.get("targets"), "trigger", dep.get("trigger"), "backend", dep.get("backend_fn"), "queue", dep.get("queue"), "inputs", len(ins), "outputs", len(outs))
    print(" in(first20)=", [(i, components.get(i, {}).get("type"), components.get(i, {}).get("props", {}).get("label"), components.get(i, {}).get("props", {}).get("elem_id")) for i in ins[:20]])
    print(" out=", [(o, components.get(o, {}).get("type"), components.get(o, {}).get("props", {}).get("label"), components.get(o, {}).get("props", {}).get("elem_id")) for o in outs])
    print("-")
