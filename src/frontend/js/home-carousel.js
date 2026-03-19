const carousel = document.querySelector('[data-carousel]');

if (carousel) {
  const items = Array.from(carousel.querySelectorAll('.carousel-item'));
  const prevButton = carousel.querySelector('.carousel-control.prev');
  const nextButton = carousel.querySelector('.carousel-control.next');
  const dotsContainer = carousel.querySelector('.carousel-dots');

  let activeIndex = 0;

  const getShortestOffset = (index, active, length) => {
    let offset = index - active;

    if (offset > length / 2) {
      offset -= length;
    }

    if (offset < -length / 2) {
      offset += length;
    }

    return offset;
  };

  const getSpacing = () => (window.innerWidth <= 700 ? 105 : 170);

  const dots = items.map((_, index) => {
    const dot = document.createElement('button');
    dot.type = 'button';
    dot.className = 'carousel-dot';
    dot.setAttribute('aria-label', `Ir para o evento ${index + 1}`);

    dot.addEventListener('click', () => {
      activeIndex = index;
      updateCarousel();
    });

    dotsContainer.appendChild(dot);
    return dot;
  });

  const updateCarousel = () => {
    const spacing = getSpacing();

    items.forEach((item, index) => {
      const offset = getShortestOffset(index, activeIndex, items.length);
      const distance = Math.abs(offset);
      const isHidden = distance > 2;
      const scale = Math.max(0.6, 1 - distance * 0.18);
      const yShift = distance === 0 ? 0 : 16;

      item.style.transform = `translateX(calc(-50% + ${offset * spacing}px)) translateY(${yShift}px) scale(${scale})`;
      item.style.opacity = isHidden ? '0' : String(Math.max(0.3, 1 - distance * 0.35));
      item.style.zIndex = String(50 - distance);
      item.classList.toggle('is-active', distance === 0);
      item.classList.toggle('is-side', distance > 0 && distance <= 2);
      item.classList.toggle('is-hidden', isHidden);
      item.setAttribute('aria-hidden', isHidden ? 'true' : 'false');
      item.tabIndex = distance === 0 ? 0 : -1;
    });

    dots.forEach((dot, index) => {
      dot.classList.toggle('active', index === activeIndex);
      dot.setAttribute('aria-current', index === activeIndex ? 'true' : 'false');
    });
  };

  prevButton?.addEventListener('click', () => {
    activeIndex = (activeIndex - 1 + items.length) % items.length;
    updateCarousel();
  });

  nextButton?.addEventListener('click', () => {
    activeIndex = (activeIndex + 1) % items.length;
    updateCarousel();
  });

  window.addEventListener('resize', updateCarousel);

  updateCarousel();
}
