from fastapi import FastAPI
from pydantic import BaseModel

from backend.florence_model import generate_caption
from backend.rampp_model import generate_ram_tags

print("🔥 MAIN.PY ELINDULT")

app = FastAPI()


class ImageRequest(BaseModel):
    image_path: str


@app.get("/")
def root():
    return {"message": "ImageInsight backend is running"}


@app.post("/analyze")
def analyze(req: ImageRequest):
    try:
        caption = generate_caption(req.image_path)
        ram_tags = generate_ram_tags(req.image_path)

        return {
            "caption": caption,
            "ram_tags": ram_tags
        }

    except Exception as e:
        return {"error": str(e)}