#!/usr/bin/env python3
"""Generate icons from the Run of the Nine guide using a Fooocus-compatible API."""

from __future__ import annotations

import argparse
import base64
import json
import os
import re
import traceback
import sys
import time
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from urllib.parse import urljoin

import requests

FILENAME_TOKEN_RE = re.compile(r"\b(?:icon|frame)_[a-z0-9_]+\.png\b", re.IGNORECASE)
VALID_FILENAME_RE = re.compile(
    r"^(?:icon_[a-z0-9]+(?:_[a-z0-9]+)*|frame_[a-z0-9]+(?:_[a-z0-9]+)*)\.png$",
    re.IGNORECASE,
)
BLOCK_SEPARATOR_RE = re.compile(r"\n-{8,}\n")

FOOOCUS_STYLE = "Sai Pixel Art"


@dataclass
class IconTask:
    filename: str
    prompt: str


def _resolve_user_path(path_value: str, workspace_root: Path) -> Path:
    candidate = Path(path_value)
    if candidate.is_absolute():
        return candidate
    return (workspace_root / candidate).resolve()


def _snapshot_pngs(root: Path) -> dict[str, tuple[float, int]]:
    snap: dict[str, tuple[float, int]] = {}
    if not root.exists():
        return snap
    for p in root.rglob("*.png"):
        stat = p.stat()
        snap[str(p.resolve())] = (stat.st_mtime, stat.st_size)
    return snap


def _wait_for_new_png(
    root: Path,
    before: dict[str, tuple[float, int]],
    timeout_seconds: float,
    poll_seconds: float,
) -> Path | None:
    deadline = time.time() + timeout_seconds
    newest: Path | None = None
    newest_mtime = -1.0

    while time.time() < deadline:
        if root.exists():
            for p in root.rglob("*.png"):
                resolved = str(p.resolve())
                stat = p.stat()
                now = (stat.st_mtime, stat.st_size)
                prev = before.get(resolved)
                # Accept both truly new files and overwritten existing files.
                if prev is None or now != prev:
                    if stat.st_mtime >= newest_mtime:
                        newest_mtime = stat.st_mtime
                        newest = p
        if newest is not None:
            return newest
        time.sleep(poll_seconds)

    return None


def append_log(log_file: Path | None, event: str, payload: dict[str, Any]) -> None:
    if log_file is None:
        return

    log_file.parent.mkdir(parents=True, exist_ok=True)
    entry = {
        "ts": time.strftime("%Y-%m-%dT%H:%M:%S"),
        "event": event,
        **payload,
    }
    with log_file.open("a", encoding="utf-8") as f:
        f.write(json.dumps(entry, ensure_ascii=True, default=str) + "\n")


def normalize_line(line: str) -> str:
    cleaned = line.strip().replace("{=html}", "")
    if cleaned.endswith("\\"):
        cleaned = cleaned[:-1].strip()
    return cleaned


def extract_filenames_from_line(line: str) -> list[str]:
    return [match.group(0).lower() for match in FILENAME_TOKEN_RE.finditer(line)]


def parse_guide(markdown_text: str) -> list[IconTask]:
    tasks: list[IconTask] = []
    seen: set[str] = set()

    blocks = BLOCK_SEPARATOR_RE.split(markdown_text)
    for block in blocks:
        lines = [normalize_line(line) for line in block.splitlines()]
        lines = [line for line in lines if line]
        if not lines:
            continue

        filenames: list[str] = []
        for line in lines:
            filenames.extend(extract_filenames_from_line(line))

        if not filenames:
            continue

        lower_block = "\n".join(lines).lower()
        if "naming convention" in lower_block or "examples" in lower_block:
            continue

        pending_filenames: list[str] = []
        prompt_count = 0
        i = 0
        while i < len(lines):
            line = lines[i]
            lower_line = line.lower()

            current_line_filenames = extract_filenames_from_line(line)
            if current_line_filenames:
                for filename in current_line_filenames:
                    if filename not in pending_filenames:
                        pending_filenames.append(filename)

            if lower_line == "prompt":
                prompt_count += 1
                if not pending_filenames:
                    raise ValueError(
                        "Found 'Prompt' without preceding filename list in guide block:\n"
                        + "\n".join(lines)
                    )

                prompt_lines: list[str] = []
                j = i + 1
                while j < len(lines):
                    candidate = lines[j]
                    candidate_lower = candidate.lower()

                    if candidate_lower == "prompt":
                        break
                    if extract_filenames_from_line(candidate):
                        break
                    if j + 1 < len(lines) and extract_filenames_from_line(lines[j + 1]):
                        break

                    prompt_lines.append(candidate)
                    j += 1

                prompt = " ".join(prompt_lines).strip()
                if not prompt:
                    raise ValueError(
                        f"Empty prompt text after 'Prompt' for filenames: {', '.join(pending_filenames)}"
                    )

                for filename in pending_filenames:
                    if filename in seen:
                        continue
                    if not VALID_FILENAME_RE.match(filename):
                        raise ValueError(
                            f"Filename '{filename}' does not match naming convention "
                            "icon_<category>_<name>[_<rarity>].png or frame_<...>.png"
                        )
                    tasks.append(IconTask(filename=filename, prompt=prompt))
                    seen.add(filename)

                pending_filenames = []
                i = j
                continue

            i += 1

        if pending_filenames:
            raise ValueError(
                f"Missing 'Prompt' block for filenames: {', '.join(pending_filenames)}. "
                "Each icon block must define a prompt after a line containing 'Prompt'."
            )

        if filenames and prompt_count == 0:
            raise ValueError(
                "No 'Prompt' entries found in a block that contains filenames:\n" + "\n".join(lines)
            )

    return tasks


def flatten_json(value: Any) -> list[Any]:
    if isinstance(value, list):
        result: list[Any] = []
        for item in value:
            result.extend(flatten_json(item))
        return result
    if isinstance(value, dict):
        result = []
        for item in value.values():
            result.extend(flatten_json(item))
        return result
    return [value]


def decode_base64_image(value: str) -> bytes | None:
    if not isinstance(value, str):
        return None

    if value.startswith("data:image") and "," in value:
        value = value.split(",", 1)[1]

    if len(value) < 128:
        return None

    try:
        return base64.b64decode(value, validate=True)
    except Exception:
        return None


def extract_images_from_response(data: Any, base_url: str, session: requests.Session) -> list[bytes]:
    images: list[bytes] = []

    if isinstance(data, dict) and "images" in data and isinstance(data["images"], list):
        for item in data["images"]:
            if isinstance(item, str):
                decoded = decode_base64_image(item)
                if decoded:
                    images.append(decoded)

    flattened = flatten_json(data)
    for item in flattened:
        if isinstance(item, str):
            decoded = decode_base64_image(item)
            if decoded:
                images.append(decoded)
                continue

            if item.startswith("http://") or item.startswith("https://") or item.startswith("/"):
                image_url = item if item.startswith("http") else urljoin(base_url + "/", item.lstrip("/"))
                response = session.get(image_url, timeout=300)
                if response.ok and response.content:
                    images.append(response.content)

    return images


def build_payload(endpoint: str, prompt: str, negative_prompt: str, seed: int) -> dict[str, Any]:
    if endpoint.startswith("/sdapi/"):
        return {
            "prompt": prompt,
            "negative_prompt": negative_prompt,
            "steps": 28,
            "cfg_scale": 6.0,
            "sampler_name": "DPM++ 2M Karras",
            "width": 64,
            "height": 64,
            "seed": seed,
            "batch_size": 1,
            "n_iter": 1,
        }

    # Payload shape compatible with common Fooocus-API wrappers.
    return {
        "prompt": prompt,
        "negative_prompt": negative_prompt,
        "style_selections": [FOOOCUS_STYLE],
        "performance_selection": "Speed",
        "aspect_ratios_selection": "1024*1024",
        "image_number": 1,
        "image_seed": seed,
        "sharpness": 2,
        "guidance_scale": 4,
    }


def try_generate(
    session: requests.Session,
    api_url: str,
    prompt: str,
    negative_prompt: str,
    seed: int,
) -> list[bytes]:
    candidates = [
        "/v1/generation/text-to-image",
        "/v2/generation/text-to-image",
        "/api/v1/generation/text-to-image",
        "/api/v1/txt2img",
        "/sdapi/v1/txt2img",
    ]

    errors: list[str] = []
    for endpoint in candidates:
        url = f"{api_url.rstrip('/')}{endpoint}"
        payload = build_payload(endpoint, prompt, negative_prompt, seed)
        try:
            response = session.post(url, json=payload, timeout=900)
        except requests.RequestException as exc:
            errors.append(f"{endpoint}: {exc}")
            continue

        if not response.ok:
            body_preview = response.text.strip().replace("\n", " ")[:220]
            errors.append(f"{endpoint}: HTTP {response.status_code} {body_preview}")
            continue

        content_type = response.headers.get("Content-Type", "").lower()
        if "application/json" in content_type:
            data = response.json()
            images = extract_images_from_response(data, api_url, session)
            if images:
                return images
            errors.append(f"{endpoint}: No image found in JSON response")
            continue

        if response.content:
            return [response.content]

        errors.append(f"{endpoint}: Empty response")

    joined = "\n".join(errors)
    raise RuntimeError(
        "Could not generate image from any known endpoint. "
        "Please verify --api-url points to a Fooocus API server.\n"
        f"Tried:\n{joined}"
    )


def _find_generate_fn_index(config: dict[str, Any]) -> int:
    # For Gradio 3.x configs without fn_index, dependency array index is the fn_index.
    for dep_index, dep in enumerate(config.get("dependencies", [])):
        outputs = dep.get("outputs", [])
        if outputs:
            return int(dep.get("fn_index", dep_index))
    raise RuntimeError("Could not locate any callable function in Gradio config")


def _find_component_id_by_elem_id(config: dict[str, Any], elem_id: str) -> int | None:
    for comp in config.get("components", []):
        if str(comp.get("props", {}).get("elem_id", "")) == elem_id:
            return int(comp["id"])
    return None


def _find_gradio_chain_indices(config: dict[str, Any]) -> tuple[int, int, int, int, int]:
    components = {comp["id"]: comp for comp in config.get("components", []) if "id" in comp}
    generate_button_id = _find_component_id_by_elem_id(config, "generate_button")
    reset_button_id = _find_component_id_by_elem_id(config, "reset_button")

    if generate_button_id is None:
        raise RuntimeError("Could not find generate_button component in Gradio config")

    reset_index: int | None = None
    click_index: int | None = None
    seed_index: int | None = None
    task_index: int | None = None
    poll_index: int | None = None

    for dep_index, dep in enumerate(config.get("dependencies", [])):
        idx = int(dep.get("fn_index", dep_index))
        targets = dep.get("targets", [])
        trigger = dep.get("trigger")
        inputs = dep.get("inputs", [])
        outputs = dep.get("outputs", [])

        if reset_button_id is not None and reset_button_id in targets and trigger == "click":
            if len(inputs) == 0 and len(outputs) >= 2:
                reset_index = idx

        if generate_button_id in targets and trigger == "then":
            if len(inputs) == 2 and len(outputs) == 1:
                seed_index = idx

            if len(inputs) > 50 and len(outputs) == 1:
                out_comp = components.get(outputs[0], {})
                if str(out_comp.get("type", "")).lower() == "state":
                    task_index = idx

            has_finished_gallery = False
            for output_id in outputs:
                comp = components.get(output_id, {})
                ctype = str(comp.get("type", "")).lower()
                label = str(comp.get("props", {}).get("label", ""))
                elem_id = str(comp.get("props", {}).get("elem_id", ""))
                if ctype == "gallery" and ("Finished Images" in label or elem_id == "final_gallery"):
                    has_finished_gallery = True
                    break
            if has_finished_gallery:
                poll_index = idx

        if generate_button_id in targets and trigger == "click":
            if len(inputs) == 0 and len(outputs) >= 4:
                click_index = idx

    if reset_index is None or click_index is None or seed_index is None or task_index is None or poll_index is None:
        raise RuntimeError(
            "Could not locate full generation chain indices in Gradio config "
            f"(reset={reset_index}, click={click_index}, seed={seed_index}, task={task_index}, poll={poll_index})"
        )

    return reset_index, click_index, seed_index, task_index, poll_index


def _default_for_component(comp: dict[str, Any]) -> Any:
    props = comp.get("props", {})
    if "value" in props:
        return props["value"]

    ctype = str(comp.get("type", "")).lower()
    if ctype in {"textbox", "markdown", "html"}:
        return ""
    if ctype in {"checkbox"}:
        return False
    if ctype in {"checkboxgroup"}:
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


def _build_gradio_inputs(
    config: dict[str, Any],
    fn_index: int,
    prompt: str,
    negative_prompt: str,
    seed: int,
) -> list[Any]:
    components = {comp["id"]: comp for comp in config.get("components", []) if "id" in comp}
    dependency = None
    for dep_index, dep in enumerate(config.get("dependencies", [])):
        dep_fn_index = int(dep.get("fn_index", dep_index))
        if dep_fn_index == fn_index:
            dependency = dep
            break

    if dependency is None:
        raise RuntimeError(f"Could not find dependency for fn_index {fn_index}")

    data: list[Any] = []
    for input_id in dependency.get("inputs", []):
        comp = components.get(input_id, {})
        label = str(comp.get("props", {}).get("label", "")).strip().lower()
        elem_id = str(comp.get("props", {}).get("elem_id", "")).strip().lower()
        ctype = str(comp.get("type", "")).strip().lower()
        value = _default_for_component(comp)

        if elem_id == "positive_prompt":
            value = prompt
        elif elem_id == "negative_prompt":
            value = negative_prompt
        elif "negative" in label and "prompt" in label:
            value = negative_prompt
        elif label == "prompt" or ("prompt" in label and "negative" not in label):
            value = prompt
        elif ctype == "checkboxgroup" and ("selected styles" in label or "style" in elem_id):
            value = [FOOOCUS_STYLE]
        elif "image number" in label:
            value = 1
        elif "image seed" in label:
            value = seed
        elif "seed" == label and isinstance(value, (int, float)):
            value = seed

        data.append(value)

    return data


def _extract_gradio_images(result: Any) -> list[bytes]:
    images: list[bytes] = []
    flattened = flatten_json(result)
    for item in flattened:
        if isinstance(item, str):
            decoded = decode_base64_image(item)
            if decoded:
                images.append(decoded)
        if isinstance(item, dict):
            data = item.get("data")
            if isinstance(data, str):
                decoded = decode_base64_image(data)
                if decoded:
                    images.append(decoded)
    return images


def try_generate_gradio(
    api_url: str,
    prompt: str,
    negative_prompt: str,
    seed: int,
    fooocus_output_dir: Path,
    task_filename: str,
    log_file: Path | None = None,
    debug: bool = False,
) -> list[bytes]:
    config_url = f"{api_url.rstrip('/')}/config"
    config = requests.get(config_url, timeout=60).json()
    reset_idx, click_idx, seed_idx, task_idx, poll_idx = _find_gradio_chain_indices(config)

    append_log(
        log_file,
        "gradio_chain",
        {
            "filename": task_filename,
            "api_url": api_url,
            "reset_idx": reset_idx,
            "click_idx": click_idx,
            "seed_idx": seed_idx,
            "task_idx": task_idx,
            "poll_idx": poll_idx,
            "style": FOOOCUS_STYLE,
        },
    )

    before_snapshot = _snapshot_pngs(fooocus_output_dir)

    session_hash = uuid.uuid4().hex[:12]
    session = requests.Session()
    predict_url = f"{api_url.rstrip('/')}/api/predict"

    reset_payload = {"fn_index": reset_idx, "data": [], "session_hash": session_hash}
    r_reset = session.post(predict_url, json=reset_payload, timeout=120)
    r_reset.raise_for_status()
    append_log(
        log_file,
        "gradio_reset_sent",
        {
            "filename": task_filename,
            "fn_index": reset_idx,
            "session_hash": session_hash,
        },
    )

    click_payload = {"fn_index": click_idx, "data": [], "session_hash": session_hash}
    r_click = session.post(predict_url, json=click_payload, timeout=120)
    r_click.raise_for_status()

    random_seed = seed < 0
    seed_value = "0" if seed < 0 else str(seed)
    seed_payload = {
        "fn_index": seed_idx,
        "data": [random_seed, seed_value],
        "session_hash": session_hash,
    }
    r_seed = session.post(predict_url, json=seed_payload, timeout=120)
    r_seed.raise_for_status()
    append_log(
        log_file,
        "gradio_seed_sent",
        {
            "filename": task_filename,
            "fn_index": seed_idx,
            "random_seed": random_seed,
            "seed_value": seed_value,
        },
    )

    task_data = _build_gradio_inputs(config, task_idx, prompt, negative_prompt, seed)
    append_log(
        log_file,
        "gradio_task_inputs",
        {
            "filename": task_filename,
            "fn_index": task_idx,
            "session_hash": session_hash,
            "prompt": prompt,
            "negative_prompt": negative_prompt,
            "seed": seed,
            "style": FOOOCUS_STYLE,
            "input_count": len(task_data),
            "task_data_preview": [str(x)[:220] for x in task_data[:30]],
        },
    )
    task_payload = {"fn_index": task_idx, "data": task_data, "session_hash": session_hash}
    r_task = session.post(predict_url, json=task_payload, timeout=300)
    r_task.raise_for_status()
    task_result = r_task.json()
    append_log(
        log_file,
        "gradio_task_result",
        {
            "filename": task_filename,
            "is_generating": bool(task_result.get("is_generating", False)),
            "task_result_keys": sorted(task_result.keys()) if isinstance(task_result, dict) else [],
        },
    )

    poll_input = [None]
    if isinstance(task_result, dict) and isinstance(task_result.get("data"), list) and task_result["data"]:
        poll_input = [task_result["data"][0]]

    poll_payload = {"fn_index": poll_idx, "data": poll_input, "session_hash": session_hash}
    final_result: dict[str, Any] | None = None
    poll_iterations = 0
    saw_generating_true = False
    for _ in range(3600):
        r_poll = session.post(predict_url, json=poll_payload, timeout=300)
        r_poll.raise_for_status()
        result = r_poll.json()
        final_result = result
        poll_iterations += 1
        is_generating = bool(result.get("is_generating", False))
        if is_generating:
            saw_generating_true = True
        if saw_generating_true and not is_generating:
            break
        time.sleep(0.1)

    if final_result is None:
        raise RuntimeError("No response from Gradio polling stage")

    if not saw_generating_true:
        raise RuntimeError(
            "Generation did not start (is_generating never became true). "
            "Request payload likely did not trigger Fooocus worker."
        )

    newest_path = _wait_for_new_png(
        root=fooocus_output_dir,
        before=before_snapshot,
        timeout_seconds=45.0,
        poll_seconds=0.25,
    )

    if newest_path is None:
        if debug:
            debug_path = Path("foocus_api") / f"debug_gradio_result_{uuid.uuid4().hex[:8]}.json"
            debug_path.write_text(json.dumps(final_result, indent=2, default=str), encoding="utf-8")
            raise RuntimeError(
                f"No new PNG detected in {fooocus_output_dir}. "
                f"Debug written to {debug_path}."
            )
        raise RuntimeError(
            f"No new PNG detected in {fooocus_output_dir}. "
            "Fooocus may be running with image logging disabled or writing to a different temp path."
        )

    append_log(
        log_file,
        "gradio_output_picked",
        {
            "filename": task_filename,
            "poll_iterations": poll_iterations,
            "picked_output_path": str(newest_path),
            "picked_output_mtime": newest_path.stat().st_mtime,
            "final_is_generating": bool(final_result.get("is_generating", False)) if final_result else None,
            "final_data_preview": str(final_result.get("data", []))[:350] if isinstance(final_result, dict) else "",
        },
    )

    return [newest_path.read_bytes()]


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Generate and download all icon images defined in run_of_the_nine_icon_prompt_guide.md"
    )
    parser.add_argument(
        "--api-url",
        default="http://127.0.0.1:7865",
        help="Base URL of Fooocus API server (default: http://127.0.0.1:7865)",
    )
    parser.add_argument(
        "--guide",
        default="docs/run_of_the_nine_icon_prompt_guide.md",
        help="Path to prompt guide markdown",
    )
    parser.add_argument(
        "--output-dir",
        default="docs/icons_foocus",
        help="Directory to save generated PNG files",
    )
    parser.add_argument(
        "--negative-prompt",
        default="text, watermark, logo, blurry, low quality, jpeg artifacts",
        help="Negative prompt applied to every generation",
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=-1,
        help="Base seed. Use -1 for random; non-negative values are offset by icon index",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Overwrite files if they already exist",
    )
    parser.add_argument(
        "--delay-seconds",
        type=float,
        default=0.0,
        help="Optional pause between generation requests",
    )
    parser.add_argument(
        "--list-only",
        action="store_true",
        help="Only print parsed tasks without generating images",
    )
    parser.add_argument(
        "--api-mode",
        choices=["auto", "gradio", "rest"],
        default="auto",
        help="API mode: auto tries gradio first then rest",
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Write extra debug files on response parsing issues",
    )
    parser.add_argument(
        "--fooocus-output-dir",
        default=os.path.join(os.environ.get("TEMP", "C:/Windows/Temp"), "fooocus"),
        help="Fooocus image directory (default: %TEMP%/fooocus)",
    )
    parser.add_argument(
        "--log-file",
        default="foocus_api/generation_log.jsonl",
        help="Path to JSONL log file with prompts/payload summaries/results",
    )

    args = parser.parse_args()

    workspace_root = Path(__file__).resolve().parents[1]
    guide_path = _resolve_user_path(args.guide, workspace_root)
    output_dir = _resolve_user_path(args.output_dir, workspace_root)
    fooocus_output_dir = _resolve_user_path(args.fooocus_output_dir, workspace_root)
    log_file = _resolve_user_path(args.log_file, workspace_root) if args.log_file else None

    if log_file is not None:
        log_file.parent.mkdir(parents=True, exist_ok=True)
        log_file.write_text("", encoding="utf-8")

    if not guide_path.exists():
        print(f"Guide file not found: {guide_path}", file=sys.stderr)
        return 2

    markdown_text = guide_path.read_text(encoding="utf-8")
    tasks = parse_guide(markdown_text)
    if not tasks:
        print("No icon tasks found in guide.", file=sys.stderr)
        return 3

    print(f"Parsed {len(tasks)} icon tasks from {guide_path}")
    append_log(
        log_file,
        "run_start",
        {
            "guide": str(guide_path),
            "output_dir": str(output_dir),
            "api_url": args.api_url,
            "api_mode": args.api_mode,
            "style": FOOOCUS_STYLE,
            "task_count": len(tasks),
        },
    )
    if args.list_only:
        for task in tasks:
            print(f"- {task.filename}")
        return 0

    output_dir.mkdir(parents=True, exist_ok=True)
    session = requests.Session()

    generated = 0
    skipped = 0

    for idx, task in enumerate(tasks, start=1):
        out_path = output_dir / task.filename
        if out_path.exists() and not args.overwrite:
            skipped += 1
            print(f"[{idx}/{len(tasks)}] Skip existing: {out_path}")
            continue

        seed = -1 if args.seed < 0 else args.seed + idx - 1
        print(f"[{idx}/{len(tasks)}] Generating {task.filename} ...")
        append_log(
            log_file,
            "task_start",
            {
                "index": idx,
                "total": len(tasks),
                "filename": task.filename,
                "prompt": task.prompt,
                "negative_prompt": args.negative_prompt,
                "seed": seed,
                "style": FOOOCUS_STYLE,
            },
        )

        errors: list[str] = []
        images: list[bytes] | None = None

        if args.api_mode in {"auto", "gradio"}:
            try:
                images = try_generate_gradio(
                    api_url=args.api_url,
                    prompt=task.prompt,
                    negative_prompt=args.negative_prompt,
                    seed=seed,
                    fooocus_output_dir=fooocus_output_dir,
                    task_filename=task.filename,
                    log_file=log_file,
                    debug=args.debug,
                )
            except Exception as exc:
                errors.append(f"gradio: {exc}")
                append_log(
                    log_file,
                    "task_error",
                    {
                        "filename": task.filename,
                        "mode": "gradio",
                        "error": str(exc),
                        "traceback": traceback.format_exc(),
                    },
                )
                if args.api_mode == "gradio":
                    raise

        if images is None and args.api_mode in {"auto", "rest"}:
            try:
                images = try_generate(
                    session=session,
                    api_url=args.api_url,
                    prompt=task.prompt,
                    negative_prompt=args.negative_prompt,
                    seed=seed,
                )
            except Exception as exc:
                errors.append(f"rest: {exc}")
                append_log(
                    log_file,
                    "task_error",
                    {
                        "filename": task.filename,
                        "mode": "rest",
                        "error": str(exc),
                        "traceback": traceback.format_exc(),
                    },
                )

        if images is None:
            joined = "\n".join(errors)
            raise RuntimeError(f"Generation failed for {task.filename}\n{joined}")

        out_path.write_bytes(images[0])
        generated += 1
        print(f"Saved: {out_path}")
        append_log(
            log_file,
            "task_saved",
            {
                "filename": task.filename,
                "out_path": str(out_path),
                "bytes": len(images[0]),
            },
        )

        if args.delay_seconds > 0:
            time.sleep(args.delay_seconds)

    print(
        json.dumps(
            {
                "parsed": len(tasks),
                "generated": generated,
                "skipped": skipped,
                "output_dir": str(output_dir.resolve()),
                "api_url": args.api_url,
                "log_file": str(log_file.resolve()) if log_file else None,
            },
            indent=2,
        )
    )
    append_log(
        log_file,
        "run_end",
        {
            "parsed": len(tasks),
            "generated": generated,
            "skipped": skipped,
            "output_dir": str(output_dir.resolve()),
            "api_url": args.api_url,
        },
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
