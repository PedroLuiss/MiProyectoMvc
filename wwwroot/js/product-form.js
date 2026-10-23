// Muestra una previsualizacion local antes de subir la foto al servidor.
document.addEventListener("DOMContentLoaded", () => {
  const input = document.querySelector("[data-image-input]");
  const preview = document.getElementById("imagen-preview");

  if (!(input instanceof HTMLInputElement) || !(preview instanceof HTMLImageElement)) {
    return;
  }

  let currentPreviewUrl;

  input.addEventListener("change", () => {
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    if (currentPreviewUrl) {
      URL.revokeObjectURL(currentPreviewUrl);
    }

    currentPreviewUrl = URL.createObjectURL(file);
    preview.src = currentPreviewUrl;
  });

  window.addEventListener("pagehide", () => {
    if (currentPreviewUrl) {
      URL.revokeObjectURL(currentPreviewUrl);
    }
  });
});
