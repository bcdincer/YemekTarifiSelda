// Recipe Form JavaScript
// Bu dosya Create ve Edit sayfaları için ortak JavaScript kodlarını içerir

// Görsel önizleme işlemleri
(function initImagePreview() {
    const imageFileInput = document.getElementById('imageFile');
    const imagePreview = document.getElementById('imagePreview');
    const previewImg = document.getElementById('previewImg');
    const removeImageBtn = document.getElementById('removeImage');
    const imageUrlInput = document.getElementById('imageUrl');

    // Dosya seçildiğinde önizleme göster
    if (imageFileInput) {
        imageFileInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    if (previewImg) previewImg.src = e.target.result;
                    if (imagePreview) imagePreview.style.display = 'block';
                    if (imageUrlInput) imageUrlInput.value = ''; // URL input'unu temizle
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // Görseli kaldır
    if (removeImageBtn) {
        removeImageBtn.addEventListener('click', function() {
            if (imageFileInput) imageFileInput.value = '';
            if (imagePreview) imagePreview.style.display = 'none';
            if (previewImg) previewImg.src = '';
        });
    }

    // URL input'u değiştiğinde dosya input'unu temizle
    if (imageUrlInput) {
        imageUrlInput.addEventListener('input', function() {
            if (this.value && imageFileInput) {
                imageFileInput.value = '';
                if (imagePreview) imagePreview.style.display = 'none';
            }
        });
    }
})();

// Dinamik Liste Fonksiyonları
let stepCounter = 0;
let draggedElement = null;

function addStepItem(value = '') {
    const list = document.getElementById('stepsList');
    if (!list) return;
    const item = createListItem('step', stepCounter++, value);
    list.appendChild(item);
    updateStepsTextarea();
    makeSortable(list);
}

function createListItem(type, index, value = '') {
    const item = document.createElement('div');
    item.className = 'dynamic-list-item';
    item.dataset.index = index;
    item.draggable = true;

    const dragHandle = document.createElement('div');
    dragHandle.className = 'drag-handle';
    dragHandle.innerHTML = '<i class="ti-menu"></i>';

    const number = document.createElement('div');
    number.className = 'item-number';
    number.textContent = (document.querySelectorAll(`#${type}sList .dynamic-list-item`).length + 1) + '.';

    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'item-input';
    input.value = value;
    input.placeholder = type === 'ingredient' ? 'Örn: 500g tavuk göğsü' : 'Örn: Tavuğu küp küp doğrayın';
    input.addEventListener('input', () => {
        if (type === 'step') {
            updateStepsTextarea();
            updateItemNumbers(type);
        }
    });

    const removeBtn = document.createElement('div');
    removeBtn.className = 'item-remove';
    removeBtn.innerHTML = '<i class="ti-close"></i>';
    removeBtn.onclick = () => {
        item.remove();
        if (type === 'step') {
            updateStepsTextarea();
            updateItemNumbers(type);
        }
    };

    item.appendChild(dragHandle);
    item.appendChild(number);
    item.appendChild(input);
    item.appendChild(removeBtn);

    // Drag & Drop event listeners
    item.addEventListener('dragstart', (e) => {
        draggedElement = item;
        item.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
    });

    item.addEventListener('dragend', () => {
        item.classList.remove('dragging');
        draggedElement = null;
    });

    item.addEventListener('dragover', (e) => {
        e.preventDefault();
        if (draggedElement && draggedElement !== item) {
            const rect = item.getBoundingClientRect();
            const next = (e.clientY - rect.top) / (rect.bottom - rect.top) > 0.5;
            item.parentNode.insertBefore(draggedElement, next ? item.nextSibling : item);
            if (type === 'step') {
                updateItemNumbers(type);
                updateStepsTextarea();
            }
        }
    });

    return item;
}

function updateItemNumbers(type) {
    const items = document.querySelectorAll(`#${type}sList .dynamic-list-item`);
    items.forEach((item, index) => {
        const numberEl = item.querySelector('.item-number');
        if (numberEl) {
            numberEl.textContent = (index + 1) + '.';
        }
    });
}

function updateStepsTextarea() {
    const items = document.querySelectorAll('#stepsList .item-input');
    const stepsTextarea = document.getElementById('Steps');
    if (stepsTextarea) {
        const values = Array.from(items).map(input => input.value.trim()).filter(v => v);
        stepsTextarea.value = values.join('\n');
    }
}

function makeSortable(list) {
    // Sortable functionality is handled by drag & drop event listeners
}

// Mevcut adımları yükle (Edit sayfası için)
function loadExistingSteps() {
    const stepsTextarea = document.getElementById('Steps');
    if (stepsTextarea && stepsTextarea.value) {
        const steps = stepsTextarea.value.split('\n').filter(s => s.trim());
        steps.forEach(step => {
            // Numaralandırma varsa kaldır (örn: "1. ", "2. " vb.)
            const cleanStep = step.replace(/^\d+\.\s*/, '').trim();
            if (cleanStep) {
                addStepItem(cleanStep);
            }
        });
    }
    // Hiç adım yoksa en az bir tane ekle (Create sayfası için)
    if (document.querySelectorAll('#stepsList .dynamic-list-item').length === 0) {
        addStepItem();
    }
}

// Quill için yardımcı fonksiyonlar
const convertHtmlToPlainText = (html) => {
    if (!html) return '';
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = html;
    let plainText = '';
    const processNode = (node) => {
        if (node.nodeType === Node.TEXT_NODE) {
            plainText += node.textContent;
        } else if (node.nodeType === Node.ELEMENT_NODE) {
            const tagName = node.tagName.toLowerCase();
            if (tagName === 'p' || tagName === 'div' || tagName === 'h2' || tagName === 'h3') {
                if (plainText && !plainText.endsWith('\n')) {
                    plainText += '\n';
                }
                Array.from(node.childNodes).forEach(processNode);
                if (plainText && !plainText.endsWith('\n')) {
                    plainText += '\n';
                }
            } else if (tagName === 'li') {
                plainText += '• ';
                Array.from(node.childNodes).forEach(processNode);
                plainText += '\n';
            } else if (tagName === 'ul' || tagName === 'ol') {
                Array.from(node.childNodes).forEach(processNode);
            } else {
                Array.from(node.childNodes).forEach(processNode);
            }
        }
    };
    Array.from(tempDiv.childNodes).forEach(processNode);
    return plainText.trim();
};

const convertPlainTextToHtml = (plainText) => {
    if (!plainText) return '';
    // Her satırı <p> tag'ine al, bullet point'leri <li>'ye çevir
    const lines = plainText.split('\n').filter(line => line.trim());
    let html = '';
    let inList = false;
    
    lines.forEach(line => {
        const trimmed = line.trim();
        if (trimmed.startsWith('• ') || trimmed.startsWith('- ')) {
            if (!inList) {
                html += '<ul>';
                inList = true;
            }
            const content = trimmed.replace(/^[•\-]\s*/, '');
            html += `<li>${content}</li>`;
        } else {
            if (inList) {
                html += '</ul>';
                inList = false;
            }
            html += `<p>${trimmed}</p>`;
        }
    });
    if (inList) {
        html += '</ul>';
    }
    return html || '<p><br></p>';
};

// Quill için Malzemeler editörü
let ingredientsQuill;

// Quill editörünü başlat
function initQuillEditor(isEditMode = false) {
    if (typeof Quill === 'undefined') {
        console.error('Quill kütüphanesi yüklenemedi');
        return;
    }

    try {
        const toolbarOptions = [
            [{ header: [2, 3, false] }],
            ['bold', 'italic', 'underline'],
            [{ 'list': 'ordered'}, { 'list': 'bullet' }],
            ['clean']
        ];

        const placeholder = isEditMode 
            ? 'Her malzemeyi ayrı satıra yazın\nÖrn:\n500g tavuk göğsü\n2 su bardağı pilav\n1 adet soğan'
            : 'Örn:\n\nHamuru için;\n• 125 g tereyağı\n• 1,5 su bardağı su\n• 1 su bardağı + 1 yemek kaşığı un\n\nKreması için;\n• 4 su bardağı süt\n• 1 su bardağı şeker';

        ingredientsQuill = new Quill('#ingredientsEditor', {
            modules: {
                toolbar: toolbarOptions,
                history: { delay: 1000, maxStack: 50 }
            },
            placeholder: placeholder,
            theme: 'snow'
        });

        // Yükseklik/scroll ayarları
        const qlContainer = document.querySelector('#ingredientsEditor .ql-container');
        const qlEditor = document.querySelector('#ingredientsEditor .ql-editor');
        if (qlContainer) {
            qlContainer.style.minHeight = '400px';
            qlContainer.style.height = '400px';
            qlContainer.style.maxHeight = '800px';
            qlContainer.style.overflowY = 'auto';
        }
        if (qlEditor) {
            qlEditor.style.minHeight = '360px';
            qlEditor.style.height = '360px';
            qlEditor.setAttribute('spellcheck', 'false');
        }

        // Dark mode uygulama
        const applyDarkModeStyles = () => {
            const isDarkMode = document.documentElement.getAttribute('data-theme') === 'dark';
            if (qlEditor) {
                if (isDarkMode) {
                    qlEditor.style.backgroundColor = '#2d2d3f';
                    qlEditor.style.color = '#e0e0e0';
                } else {
                    qlEditor.style.backgroundColor = '';
                    qlEditor.style.color = '';
                }
            }
            const qlToolbar = document.querySelector('#ingredientsEditor .ql-toolbar');
            if (qlToolbar) {
                if (isDarkMode) {
                    qlToolbar.style.backgroundColor = '#1e1e2e';
                    qlToolbar.style.borderColor = '#3d3d4f';
                } else {
                    qlToolbar.style.backgroundColor = '';
                    qlToolbar.style.borderColor = '';
                }
            }
        };

        applyDarkModeStyles();
        const themeObserver = new MutationObserver(() => applyDarkModeStyles());
        themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

        // Mevcut içeriği yükle
        const hiddenIngredients = document.getElementById('Ingredients');
        if (hiddenIngredients && hiddenIngredients.value) {
            if (isEditMode) {
                // Edit modunda: Plain text'i HTML'e çevir
                const htmlContent = convertPlainTextToHtml(hiddenIngredients.value);
                ingredientsQuill.root.innerHTML = htmlContent;
            } else {
                // Create modunda: Direkt HTML olarak yükle (eğer varsa)
                ingredientsQuill.root.innerHTML = hiddenIngredients.value;
            }
        }

        // İçerik değiştiğinde textarea'yı güncelle
        ingredientsQuill.on('text-change', () => {
            if (hiddenIngredients) {
                const htmlData = ingredientsQuill.root.innerHTML;
                const plain = convertHtmlToPlainText(htmlData);
                hiddenIngredients.value = plain;
            }
        });

        // Form submit sırasında da textarea'yı güncelle (güvenlik için)
        const form = document.querySelector('form');
        if (form) {
            form.addEventListener('submit', () => {
                if (ingredientsQuill && hiddenIngredients) {
                    const htmlData = ingredientsQuill.root.innerHTML;
                    const plain = convertHtmlToPlainText(htmlData);
                    hiddenIngredients.value = plain;
                }
                updateStepsTextarea();
            });
        }
    } catch (err) {
        console.error('Quill yüklenirken hata:', err);
    }
}

// Sayfa yüklendiğinde başlat
document.addEventListener('DOMContentLoaded', () => {
    // Edit sayfası mı kontrol et
    const isEditMode = document.querySelector('input[name="Id"]') !== null;
    
    // Quill editörünü başlat
    initQuillEditor(isEditMode);
    
    // Adımları yükle
    loadExistingSteps();
});

