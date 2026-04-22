from fastapi import FastAPI
from pydantic import BaseModel

from backend.llm import generate_caption_tags
from backend.florence_model import generate_caption
from backend.rampp_model import generate_ram_tags

print("🔥 MAIN.PY ELINDULT")

app = FastAPI()


class ImageRequest(BaseModel):
    image_path: str


def normalize_tag(tag: str) -> str:
    return str(tag).strip().lower()


def merge_tags(ram_tags: list[str], llm_tags: list[str]) -> list[str]:
    final_tags = []
    seen = set()

    for tag in ram_tags + llm_tags:
        t = normalize_tag(tag)
        if t and t not in seen:
            seen.add(t)
            final_tags.append(t)

    return final_tags


@app.get("/")
def root():
    return {"message": "ImageInsight backend is running"}


@app.post("/analyze")
def analyze(req: ImageRequest):
    try:
        caption = generate_caption(req.image_path)
        ram_tags = generate_ram_tags(req.image_path)
        llm_tags = generate_caption_tags(caption)

        final_tags = merge_tags(ram_tags, llm_tags)

        return {
            "caption": caption,
            "ram_tags": ram_tags,
            "llm_tags": llm_tags,
            "final_tags": final_tags
        }

    except Exception as e:
        return {"error": str(e)}