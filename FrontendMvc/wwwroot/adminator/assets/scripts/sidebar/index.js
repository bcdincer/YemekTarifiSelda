// Vanilla JS slide animations
function slideUp(element, duration = 200, callback = null) {
  element.style.height = `${element.scrollHeight  }px`;
  element.style.transition = `height ${duration}ms ease`;
  element.style.overflow = 'hidden';
  
  requestAnimationFrame(() => {
    element.style.height = '0';
    element.style.paddingTop = '0';
    element.style.paddingBottom = '0';
    element.style.marginTop = '0';
    element.style.marginBottom = '0';
  });
  
  setTimeout(() => {
    element.style.display = 'none';
    element.style.removeProperty('height');
    element.style.removeProperty('padding-top');
    element.style.removeProperty('padding-bottom');
    element.style.removeProperty('margin-top');
    element.style.removeProperty('margin-bottom');
    element.style.removeProperty('overflow');
    element.style.removeProperty('transition');
    if (callback) callback();
  }, duration);
}

function slideDown(element, duration = 200, callback = null) {
  element.style.removeProperty('display');
  let display = window.getComputedStyle(element).display;
  if (display === 'none') display = 'block';
  
  element.style.display = display;
  element.style.height = '0';
  element.style.paddingTop = '0';
  element.style.paddingBottom = '0';
  element.style.marginTop = '0';
  element.style.marginBottom = '0';
  element.style.overflow = 'hidden';
  
  const height = element.scrollHeight;
  
  element.style.transition = `height ${duration}ms ease`;
  
  requestAnimationFrame(() => {
    element.style.height = `${height  }px`;
    element.style.removeProperty('padding-top');
    element.style.removeProperty('padding-bottom');
    element.style.removeProperty('margin-top');
    element.style.removeProperty('margin-bottom');
  });
  
  setTimeout(() => {
    element.style.removeProperty('height');
    element.style.removeProperty('overflow');
    element.style.removeProperty('transition');
    if (callback) callback();
  }, duration);
}

export default (function () {
  // This file is deprecated - sidebar functionality is now handled by Sidebar.js component
  // Keeping this file empty to prevent conflicts with the modern Sidebar component
  // All sidebar functionality is now in ./components/Sidebar.js
  
  // Only handle sidebar toggle for resize events (if needed)
  const sidebarToggleById = document.getElementById('sidebar-toggle');
  if (sidebarToggleById) {
    sidebarToggleById.addEventListener('click', e => {
      // Don't prevent default - let Sidebar.js handle it
      setTimeout(() => {
        window.dispatchEvent(new Event('resize'));
      }, 300);
    });
  }
}());
