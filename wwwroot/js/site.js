// Interacciones del navegador que no dependen de una libreria externa.

// Menu movil: muestra u oculta la navegacion y mantiene aria-expanded sincronizado.
document.addEventListener("DOMContentLoaded", () => {
  const toggle = document.querySelector("[data-menu-toggle]");
  const menu = document.getElementById("mobile-menu");

  if (!toggle || !menu) {
    return;
  }

  const setOpen = (open) => {
    menu.classList.toggle("hidden", !open);
    toggle.setAttribute("aria-expanded", String(open));
    toggle.querySelector("[data-menu-icon='open']")?.classList.toggle("hidden", open);
    toggle.querySelector("[data-menu-icon='close']")?.classList.toggle("hidden", !open);
  };

  toggle.addEventListener("click", () => {
    setOpen(toggle.getAttribute("aria-expanded") !== "true");
  });

  // Al navegar desde el menu movil, se cierra para no tapar la pagina destino.
  menu.querySelectorAll("a").forEach((link) => {
    link.addEventListener("click", () => setOpen(false));
  });

  // Si la ventana crece a escritorio, el menu movil queda oculto por CSS,
  // pero se restablece el estado para la proxima vez que se abra.
  window.addEventListener("resize", () => {
    if (window.matchMedia("(min-width: 640px)").matches) {
      setOpen(false);
    }
  });
});
