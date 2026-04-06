from pathlib import Path

import torch
from PIL import Image

from ram.models import ram_plus
from ram import get_transform
from ram import inference_ram as inference

MODEL_CHECKPOINT = Path("pretrained/ram_plus_swin_large_14m.pth")
IMAGE_SIZE = 384

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

print(f"[RAM++] Device: {device}")
print("[RAM++] Loading model...")

transform = get_transform(image_size=IMAGE_SIZE)

model = ram_plus(
    pretrained=str(MODEL_CHECKPOINT),
    image_size=IMAGE_SIZE,
    vit="swin_l"
)

model.eval()
model = model.to(device)

print("[RAM++] Model loaded successfully")


def generate_ram_tags(image_path: str) -> list[str]:
    image = Image.open(image_path).convert("RGB")
    image_tensor = transform(image).unsqueeze(0).to(device)

    result = inference(image_tensor, model)

#result[0] means that tags are in english.
    english_tags = result[0]

    tags = [tag.strip() for tag in english_tags.split("|") if tag.strip()]
    return tags