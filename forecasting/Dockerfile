# Forecast API image. Build context = repo root (needs data/): docker build -f forecasting/Dockerfile .
FROM python:3.14-slim

WORKDIR /app/forecasting

# Dependencies first: this layer is cached until requirements.txt changes.
COPY forecasting/requirements.txt .
RUN pip install --no-cache-dir -q -r requirements.txt

# Same layout as the repo, so ROOT.parent / "data" in train.py/app.py still works.
COPY data/ /app/data/
COPY forecasting/ /app/forecasting/

# Train at build time: model.joblib is gitignored, so the image creates its own.
RUN python train.py

# Run as non-root.
RUN useradd --no-create-home appuser
USER appuser

EXPOSE 8000
# 0.0.0.0 = listen on all interfaces inside the container (127.0.0.1 would be unreachable from outside).
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000"]
