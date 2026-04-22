import json
import requests

OLLAMA_URL = "http://localhost:11434/api/generate"


def generate_caption_tags(caption: str) -> list[str]:
    prompt = f"""
You receive a detailed image description.

Task:
Extract useful visual tags from the description.

Rules:
- return ONLY a valid JSON array
- lowercase only
- short tags or short phrases only
- prefer nouns or short noun phrases
- no duplicates
- no markdown
- no explanation
- do not return full sentences
- if an adjective only makes sense with a noun, combine them
- do not return standalone adjectives like "blue", "clear", "long"

Example good output:
["aerial view", "highway", "field", "sky", "buildings"]

Image description:
{caption}
""".strip()

    response = requests.post(
        OLLAMA_URL,
        json={
            "model": "qwen2.5:3b",
            "prompt": prompt,
            "stream": False,
            "format": {
                "type": "array",
                "items": {"type": "string"}
            },
            "options": {
                "temperature": 0.0
            }
        },
        timeout=120
    )

    response.raise_for_status()
    result = response.json()["response"].strip()
    result = result.replace("```json", "").replace("```", "").strip()

    try:
        parsed = json.loads(result)
        if isinstance(parsed, list):
            return [str(x).strip().lower() for x in parsed if str(x).strip()]
    except Exception:
        pass

    cleaned = result.strip().strip("[]")
    parts = [p.strip().strip('"').strip("'") for p in cleaned.split(",")]
    return [p.lower() for p in parts if p]