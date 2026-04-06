from PIL import Image
import torch
from transformers import AutoProcessor, AutoModelForCausalLM

MODEL_ID = "microsoft/Florence-2-large"

device = "cuda:0" if torch.cuda.is_available() else "cpu"
torch_dtype = torch.float16 if torch.cuda.is_available() else torch.float32

print(f"[Florence] Device: {device}")
print("[Florence] Loading model...")

processor = AutoProcessor.from_pretrained(
    MODEL_ID,
    trust_remote_code=True
)

model = AutoModelForCausalLM.from_pretrained(
    MODEL_ID,
    trust_remote_code=True,
    torch_dtype=torch_dtype,
    attn_implementation="eager",
).to(device)

model.eval()

print("[Florence] Model loaded successfully")


def generate_caption(image_path: str) -> str:
    image = Image.open(image_path).convert("RGB")
    prompt = "<CAPTION>"

    inputs = processor(
        text=prompt,
        images=image,
        return_tensors="pt"
    )

    inputs = {k: v.to(device) for k, v in inputs.items()}

    with torch.no_grad():
        generated_ids = model.generate(
            **inputs,
            max_new_tokens=64,
            num_beams=3
        )

    text = processor.batch_decode(
        generated_ids,
        skip_special_tokens=True
    )[0]

    return text.strip()