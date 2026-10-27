// Acciones del catalogo que intercambian fragmentos Razor sin recargar la pagina.
document.addEventListener("submit", async (event) => {
  const form = event.target;
  if (!(form instanceof HTMLFormElement)) {
    return;
  }

  if (form.matches("[data-ajax-delete]")) {
    event.preventDefault();

    if (!window.confirm("¿Seguro que quieres eliminar este producto?")) {
      return;
    }

    const button = form.querySelector('button[type="submit"]');
    if (button) {
      button.disabled = true;
    }

    try {
      const response = await fetch(form.action, {
        method: "POST",
        body: new FormData(form),
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "text/html"
        }
      });

      if (response.redirected) {
        window.location.assign(response.url);
        return;
      }

      if (!response.ok) {
        throw new Error("No se pudo eliminar el producto. Inténtalo de nuevo.");
      }

      const html = await response.text();
      const documentFragment = new DOMParser().parseFromString(html, "text/html");
      const resultadosActualizados = documentFragment.querySelector("#producto-resultados");
      const resultadosActuales = document.querySelector("#producto-resultados");

      if (!resultadosActualizados || !resultadosActuales) {
        throw new Error("El servidor devolvió una respuesta inesperada al eliminar el producto.");
      }

      resultadosActuales.replaceWith(resultadosActualizados);
      mostrarMensaje("Producto eliminado correctamente.");
    } catch (error) {
      mostrarMensaje(error.message, true);
      if (button) {
        button.disabled = false;
      }
    }

    return;
  }

  if (!form.matches("[data-ajax-stock]")) {
    return;
  }

  event.preventDefault();

  const section = form.closest("[data-product-stock]");
  const button = form.querySelector('button[type="submit"]');
  if (button) {
    button.disabled = true;
  }

  try {
    const response = await fetch(form.action, {
      method: "POST",
      body: new FormData(form),
      headers: {
        "X-Requested-With": "XMLHttpRequest",
        Accept: "text/html"
      }
    });

    if (response.redirected) {
      window.location.assign(response.url);
      return;
    }

    const html = await response.text();
    const documentFragment = new DOMParser().parseFromString(html, "text/html");
    const stockActualizado = documentFragment.querySelector("[data-product-stock]");

    if (!stockActualizado || !section) {
      throw new Error("El servidor devolvió una respuesta inesperada al actualizar el stock.");
    }

    const stockGuardado = stockActualizado.querySelector('input[name="Stock"]')?.value;
    section.replaceWith(stockActualizado);

    if (response.ok) {
      const card = document.querySelector(`[data-product-card="${stockActualizado.dataset.productStock}"]`);
      const badgeAgotado = card?.querySelector("[data-out-of-stock]");
      if (badgeAgotado && stockGuardado !== undefined) {
        badgeAgotado.classList.toggle("hidden", Number(stockGuardado) > 0);
      }

      mostrarMensaje("Stock actualizado correctamente.");
    }
  } catch (error) {
    mostrarMensaje(error.message, true);
    if (button) {
      button.disabled = false;
    }
  }
});

function mostrarMensaje(texto, esError = false) {
  const contenedor = document.getElementById("ajax-messages");
  if (!contenedor) {
    return;
  }

  const mensaje = document.createElement("p");
  mensaje.className = esError
    ? "rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700"
    : "rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800";
  mensaje.setAttribute("role", esError ? "alert" : "status");
  mensaje.textContent = texto;
  contenedor.replaceChildren(mensaje);
}
