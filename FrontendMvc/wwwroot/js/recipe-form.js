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

// EditRecipe sayfası için çoklu fotoğraf yönetimi
let existingImages = [];
let newImages = [];
let removedImageIds = [];
let primaryImageIndex = 0;

// EditRecipe ve Create sayfaları için görsel yönetimi fonksiyonları
function initEditRecipeImageManagement() {
    // Hem Edit hem de Create form'larını kontrol et
    const form = document.getElementById('editRecipeForm') || document.getElementById('createRecipeForm');
    if (!form) return; // Form bulunamadıysa çık
    
    // Mevcut görselleri yükle (server-side'dan gelen data)
    const existingImagesData = window.existingImagesData || [];
    existingImages = existingImagesData;
    
    console.log('EditRecipe image management initialized:', {
        existingImagesCount: existingImages.length,
        existingImages: existingImages
    });
    
    // Primary image index'i al
    const primaryImageIndexInput = document.getElementById('primaryImageIndex');
    if (primaryImageIndexInput) {
        primaryImageIndex = parseInt(primaryImageIndexInput.value) || 0;
    }
    
    // Ana fotoğrafı en üste taşı (sayfa yüklendiğinde)
    function movePrimaryImageToTop() {
        const existingImagesList = document.getElementById('existingImagesList');
        if (existingImagesList) {
            const items = Array.from(existingImagesList.querySelectorAll('.existing-image-item'));
            const primaryItem = items.find(item => {
                const imgId = parseInt(item.dataset.imageId);
                const img = existingImages.find(img => (img.Id || img.id) == imgId);
                return img && (img.IsPrimary || img.isPrimary);
            });
            
            if (primaryItem && primaryItem !== existingImagesList.firstElementChild) {
                existingImagesList.insertBefore(primaryItem, existingImagesList.firstChild);
            }
        }
    }
    
    // Sayfa yüklendiğinde ana fotoğrafı en üste taşı
    movePrimaryImageToTop();
    
    const imageFilesInput = document.getElementById('imageFiles');
    const imageUrlInput = document.getElementById('imageUrlInput');
    const addImageUrlBtn = document.getElementById('addImageUrl');
    const newImagesList = document.getElementById('newImagesList');
    const imageUrlsJsonInput = document.getElementById('imageUrlsJson');
    const removedImageIdsInput = document.getElementById('removedImageIds');
    
    // removedImageIdsInput'un başlangıç değerini set et
    if (removedImageIdsInput && !removedImageIdsInput.value) {
        removedImageIdsInput.value = JSON.stringify([]);
    }
    
    // İlk hidden input güncellemesi
    updateHiddenInputs();
    
    // Dosya seçildiğinde
    if (imageFilesInput) {
        imageFilesInput.addEventListener('change', function(e) {
            const files = Array.from(e.target.files);
            files.forEach(file => {
                if (file.size > 5 * 1024 * 1024) {
                    alert(`${file.name} dosyası 5MB'dan büyük!`);
                    return;
                }
                const reader = new FileReader();
                reader.onload = function(e) {
                    const imageData = {
                        url: e.target.result,
                        file: file,
                        isFile: true,
                        isPrimary: existingImages.length === 0 && newImages.length === 0
                    };
                    newImages.push(imageData);
                    if (existingImages.length === 0 && newImages.length === 1) {
                        primaryImageIndex = 0;
                        if (primaryImageIndexInput) primaryImageIndexInput.value = '0';
                    } else if (existingImages.length > 0 && newImages.length === 1) {
                        // Edit sayfasında ilk yeni görsel eklendiğinde
                        const keptExistingCount = existingImages.filter(img => {
                            const imgId = img.Id || img.id;
                            return imgId && !removedImageIds.includes(imgId);
                        }).length;
                        primaryImageIndex = keptExistingCount;
                        if (primaryImageIndexInput) primaryImageIndexInput.value = primaryImageIndex.toString();
                    }
                    updateHiddenInputs();
                    renderNewImagesList();
                };
                reader.readAsDataURL(file);
            });
        });
    }
    
    // URL ile ekleme
    if (addImageUrlBtn && imageUrlInput) {
        addImageUrlBtn.addEventListener('click', function() {
            const url = imageUrlInput.value.trim();
            if (!url) {
                alert('Lütfen geçerli bir URL giriniz!');
                return;
            }
            const imageData = {
                url: url,
                isFile: false,
                isPrimary: existingImages.length === 0 && newImages.length === 0
            };
            newImages.push(imageData);
            if (existingImages.length === 0 && newImages.length === 1) {
                primaryImageIndex = 0;
                if (primaryImageIndexInput) primaryImageIndexInput.value = '0';
            } else if (existingImages.length > 0 && newImages.length === 1) {
                // Edit sayfasında ilk yeni görsel eklendiğinde
                const keptExistingCount = existingImages.filter(img => {
                    const imgId = img.Id || img.id;
                    return imgId && !removedImageIds.includes(imgId);
                }).length;
                primaryImageIndex = keptExistingCount;
                if (primaryImageIndexInput) primaryImageIndexInput.value = primaryImageIndex.toString();
            }
            imageUrlInput.value = '';
            updateHiddenInputs();
            renderNewImagesList();
        });
    }
    
    // Yeni görselleri render et
    function renderNewImagesList() {
        if (!newImagesList) return;
        
        newImagesList.innerHTML = '';
        
        if (newImages.length === 0) return;
        
        // Ana fotoğrafı en üste almak için sırala
        const sortedNewImages = [...newImages].sort((a, b) => {
            if (a.isPrimary && !b.isPrimary) return -1;
            if (!a.isPrimary && b.isPrimary) return 1;
            return 0;
        });
        
        sortedNewImages.forEach((img, index) => {
            const imageItem = document.createElement('div');
            imageItem.className = 'image-item mB-10 p-10 bd bgc-grey-50';
            
            const globalIndex = existingImages.length + index;
            imageItem.innerHTML = `
                <div class="image-container">
                    <img src="${img.url}" alt="Yeni Görsel ${index + 1}" />
                    ${img.isPrimary ? '<span class="badge bg-primary image-badge">Ana Fotoğraf</span>' : ''}
                </div>
                <div class="image-content">
                    <div class="fw-600 mB-5">Yeni Görsel ${index + 1}</div>
                    <div class="fsz-sm text-muted mB-10">${img.isFile ? 'Yüklenecek dosya' : 'URL'}</div>
                    <div class="d-flex gap-10">
                        <button type="button" class="btn btn-sm ${img.isPrimary ? 'btn-primary' : 'btn-outline-primary'}" onclick="setNewPrimaryImage(${index})">
                            <i class="ti-star"></i> ${img.isPrimary ? 'Ana Fotoğraf' : 'Ana Fotoğraf Yap'}
                        </button>
                        <button type="button" class="btn btn-sm btn-danger" onclick="removeNewImage(${index})">
                            <i class="ti-close"></i> Kaldır
                        </button>
                    </div>
                </div>
            `;
            
            newImagesList.appendChild(imageItem);
        });
        
        updateHiddenInputs();
    }
    
    // Mevcut görseli ana fotoğraf yap
    window.setExistingPrimary = function(imageId) {
        const image = existingImages.find(img => (img.Id || img.id) == imageId);
        if (!image) return;
        
        // Tüm mevcut görselleri güncelle (silinmemiş olanlar)
        existingImages.forEach(img => {
            const imgId = img.Id || img.id;
            if (imgId && !removedImageIds.includes(imgId)) {
                img.IsPrimary = imgId == imageId;
                img.isPrimary = imgId == imageId;
            }
        });
        
        // Yeni görselleri güncelle
        newImages.forEach(img => {
            img.isPrimary = false;
        });
        
        // Primary image index'i hesapla (silinmemiş görseller arasından)
        const keptImages = existingImages.filter(img => {
            const imgId = img.Id || img.id;
            return imgId && !removedImageIds.includes(imgId);
        });
        primaryImageIndex = keptImages.findIndex(img => (img.Id || img.id) == imageId);
        if (primaryImageIndexInput) primaryImageIndexInput.value = primaryImageIndex.toString();
        
        // Hidden input'ları güncelle
        updateHiddenInputs();
        
        // UI'ı güncelle ve ana fotoğrafı en üste taşı
        const existingImagesList = document.getElementById('existingImagesList');
        if (existingImagesList) {
            // Tüm mevcut görselleri al
            const items = Array.from(existingImagesList.querySelectorAll('.existing-image-item'));
            
            // Ana fotoğrafı bul ve en üste taşı
            const primaryItem = items.find(item => item.dataset.imageId == imageId);
            if (primaryItem) {
                // Ana fotoğrafı en üste taşı
                existingImagesList.insertBefore(primaryItem, existingImagesList.firstChild);
                
                // Badge ekle/güncelle
                let badge = primaryItem.querySelector('.image-badge');
                if (!badge) {
                    const imageContainer = primaryItem.querySelector('.image-container');
                    if (imageContainer) {
                        badge = document.createElement('span');
                        badge.className = 'badge bg-primary image-badge';
                        badge.textContent = 'Ana Fotoğraf';
                        imageContainer.appendChild(badge);
                    }
                }
            }
            
            // Diğer görsellerdeki badge'leri kaldır ve butonları güncelle
            items.forEach(item => {
                const btn = item.querySelector('button');
                if (btn) {
                    const isPrimary = item.dataset.imageId == imageId;
                    btn.className = `btn btn-sm ${isPrimary ? 'btn-primary' : 'btn-outline-primary'}`;
                    btn.innerHTML = `<i class="ti-star"></i> ${isPrimary ? 'Ana Fotoğraf' : 'Ana Fotoğraf Yap'}`;
                    
                    // Badge'i güncelle
                    if (!isPrimary) {
                        const badge = item.querySelector('.image-badge');
                        if (badge) badge.remove();
                    }
                }
            });
        }
        
        renderNewImagesList();
    };
    
    // Mevcut görseli kaldır
    window.removeExistingImage = function(imageId) {
        if (confirm('Bu görseli kaldırmak istediğinize emin misiniz?')) {
            // removedImageIds array'ine ekle (existingImages'dan silme, sadece işaretle)
            if (!removedImageIds.includes(imageId)) {
                removedImageIds.push(imageId);
            }
            
            // Ana fotoğraf güncelle (silinmemiş görseller arasından)
            const keptImages = existingImages.filter(img => {
                const imgId = img.Id || img.id;
                return imgId && !removedImageIds.includes(imgId);
            });
            if (keptImages.length > 0 && !keptImages.some(img => img.IsPrimary || img.isPrimary)) {
                keptImages[0].IsPrimary = true;
                keptImages[0].isPrimary = true;
                primaryImageIndex = 0;
            }
            
            if (removedImageIdsInput) removedImageIdsInput.value = JSON.stringify(removedImageIds);
            
            // Hidden input'ları güncelle
            updateHiddenInputs();
            
            // UI'dan kaldır
            const item = document.querySelector(`[data-image-id="${imageId}"]`);
            if (item) item.remove();
        }
    };
    
    // Mevcut URL görselini kaldır
    window.removeExistingImageUrl = function() {
        if (confirm('Bu görseli kaldırmak istediğinize emin misiniz?')) {
            const item = document.querySelector('[data-image-url]');
            if (item) item.remove();
        }
    };
    
    // Yeni görseli ana fotoğraf yap
    window.setNewPrimaryImage = function(index) {
        // Tüm görselleri güncelle
        existingImages.forEach(img => {
            img.IsPrimary = false;
            img.isPrimary = false;
        });
        newImages.forEach((img, i) => {
            img.isPrimary = i === index;
        });
        
        // Primary image index'i hesapla
        // Create sayfasında existingImages boş, Edit sayfasında dolu olabilir
        const keptExistingCount = existingImages.filter(img => {
            const imgId = img.Id || img.id;
            return imgId && !removedImageIds.includes(imgId);
        }).length;
        
        primaryImageIndex = keptExistingCount + index;
        if (primaryImageIndexInput) primaryImageIndexInput.value = primaryImageIndex.toString();
        
        // Hidden input'ları güncelle
        updateHiddenInputs();
        
        // Yeni görselleri render et (ana fotoğraf en üstte olacak)
        renderNewImagesList();
    };
    
    // Yeni görseli kaldır
    window.removeNewImage = function(index) {
        if (confirm('Bu görseli kaldırmak istediğinize emin misiniz?')) {
            newImages.splice(index, 1);
            if (newImages.length > 0 && !newImages.some(img => img.isPrimary)) {
                newImages[0].isPrimary = true;
                // Primary image index'i güncelle
                const keptExistingCount = existingImages.filter(img => {
                    const imgId = img.Id || img.id;
                    return imgId && !removedImageIds.includes(imgId);
                }).length;
                primaryImageIndex = keptExistingCount;
                if (primaryImageIndexInput) primaryImageIndexInput.value = primaryImageIndex.toString();
            }
            updateHiddenInputs();
            renderNewImagesList();
        }
    };
    
    // Hidden input'ları güncelle
    function updateHiddenInputs() {
        if (imageUrlsJsonInput) {
            console.log('updateHiddenInputs çağrıldı:', {
                existingImagesLength: existingImages.length,
                existingImages: existingImages,
                newImagesLength: newImages.length,
                newImages: newImages,
                removedImageIds: removedImageIds
            });
            
            // Silinmemiş mevcut görselleri al (removedImageIds içinde olmayanlar)
            // Create sayfasında existingImages boş olacak, bu yüzden bu kısım çalışmayacak
            const keptExistingImages = existingImages.filter(img => {
                const imgId = img.Id || img.id;
                const isRemoved = imgId && removedImageIds.includes(imgId);
                console.log('Image check:', { imgId, isRemoved, img });
                return imgId && !isRemoved;
            });
            
            console.log('keptExistingImages:', keptExistingImages);
            
            // Mevcut görsellerin URL'lerini al (ImageUrl veya imageUrl property'si olabilir)
            const existingUrls = keptExistingImages.map(img => {
                const url = img.ImageUrl || img.imageUrl || '';
                console.log('Image URL:', { img, url });
                return url;
            }).filter(url => url && url.trim() !== '');
            
            // Yeni görsellerin URL'lerini al
            const newUrls = newImages.map(img => img.url || '').filter(url => url && url.trim() !== '');
            
            // Tüm URL'leri birleştir
            const allUrls = [...existingUrls, ...newUrls];
            
            imageUrlsJsonInput.value = JSON.stringify(allUrls);
            
            // Primary image index'i güncelle
            const primaryImageIndexInput = document.getElementById('primaryImageIndex');
            if (primaryImageIndexInput) {
                // Create sayfasında sadece newImages var, Edit sayfasında existingImages + newImages
                if (existingImages.length === 0) {
                    // Create sayfası: sadece newImages içinde primary'yi bul
                    const primaryIndex = newImages.findIndex(img => img.isPrimary);
                    primaryImageIndexInput.value = primaryIndex >= 0 ? primaryIndex.toString() : '0';
                } else {
                    // Edit sayfası: existingImages + newImages içinde primary'yi bul
                    const keptCount = existingUrls.length;
                    const primaryInNew = newImages.findIndex(img => img.isPrimary);
                    if (primaryInNew >= 0) {
                        primaryImageIndexInput.value = (keptCount + primaryInNew).toString();
                    } else {
                        // Primary existingImages içinde
                        const primaryInExisting = keptExistingImages.findIndex(img => img.IsPrimary || img.isPrimary);
                        primaryImageIndexInput.value = primaryInExisting >= 0 ? primaryInExisting.toString() : '0';
                    }
                }
            }
            
            console.log('Hidden inputs güncellendi:', {
                mevcutToplam: existingImages.length,
                silinen: removedImageIds.length,
                korunan: existingUrls.length,
                yeni: newUrls.length,
                toplam: allUrls.length,
                urls: allUrls,
                imageUrlsJsonValue: imageUrlsJsonInput.value,
                primaryImageIndex: primaryImageIndexInput?.value
            });
        } else {
            console.error('imageUrlsJsonInput bulunamadı!');
        }
    }
    
    // Form submit flag (sonsuz döngüyü önlemek için)
    let isSubmitting = false;
    
    // Form submit handler
    const submitHandler = async function(e) {
        const formId = form.id || 'unknown';
        console.log(`Form submit event tetiklendi (${formId})`, { isSubmitting });
        
        // Eğer zaten submit ediliyorsa, tekrar tetikleme
        if (isSubmitting) {
            console.log('Form zaten submit ediliyor, yeni submit engellendi');
            e.preventDefault();
            return;
        }
        
        // Form validation kontrolü
        if (!form.checkValidity()) {
            console.error('Form validation hatası var');
            form.reportValidity();
            return;
        }
        
        // ÖNEMLİ: Form submit edilmeden önce Ingredients ve Steps textarea'larını güncelle
        // Quill editöründen içeriği al
        if (typeof ingredientsQuill !== 'undefined' && ingredientsQuill) {
            const ingredientsTextarea = document.getElementById('Ingredients');
            if (ingredientsTextarea) {
                const htmlData = ingredientsQuill.root.innerHTML;
                if (typeof convertHtmlToPlainText === 'function') {
                    const plain = convertHtmlToPlainText(htmlData);
                    ingredientsTextarea.value = plain;
                    console.log('Ingredients güncellendi:', plain.substring(0, 50) + '...');
                } else {
                    // Fallback: HTML'den text çıkar
                    const tempDiv = document.createElement('div');
                    tempDiv.innerHTML = htmlData;
                    ingredientsTextarea.value = tempDiv.textContent || tempDiv.innerText || '';
                    console.log('Ingredients güncellendi (fallback)');
                }
            } else {
                console.error('Ingredients textarea bulunamadı!');
            }
        } else {
            console.warn('ingredientsQuill tanımlı değil');
        }
        
        // Steps textarea'sını güncelle
        if (typeof updateStepsTextarea === 'function') {
            updateStepsTextarea();
            const stepsTextarea = document.getElementById('Steps');
            if (stepsTextarea) {
                console.log('Steps güncellendi:', stepsTextarea.value.substring(0, 50) + '...');
            }
        } else {
            // Fallback: Manuel olarak güncelle
            const stepsList = document.getElementById('stepsList');
            const stepsTextarea = document.getElementById('Steps');
            if (stepsList && stepsTextarea) {
                const items = stepsList.querySelectorAll('.item-input');
                const values = Array.from(items).map(input => input.value.trim()).filter(v => v);
                stepsTextarea.value = values.join('\n');
                console.log('Steps güncellendi (fallback):', stepsTextarea.value.substring(0, 50) + '...');
            } else {
                console.error('Steps textarea veya list bulunamadı!');
            }
        }
        
        // Hidden input'ları güncelle
        updateHiddenInputs();
        
        // Yeni görselleri S3'e yükle (eğer varsa)
        const filesToUpload = newImages.filter(img => img.isFile && img.file);
        
        if (filesToUpload.length > 0) {
            console.log('Yeni görseller S3\'e yükleniyor...', filesToUpload.length, 'dosya');
            isSubmitting = true;
            e.preventDefault(); // Önce S3'e yükleyelim
            
            try {
                const uploadPromises = filesToUpload.map(async (imgData) => {
                    try {
                        const formData = new FormData();
                        formData.append('file', imgData.file);
                        
                        // Token'ı al
                        let token = '';
                        if (typeof getAuthToken === 'function') {
                            token = getAuthToken();
                        } else if (typeof localStorage !== 'undefined') {
                            token = localStorage.getItem('authToken') || '';
                        }
                        
                        const headers = {};
                        if (token) {
                            headers['Authorization'] = `Bearer ${token}`;
                        }
                        
                        // Backend API URL'ini al
                        const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7016';
                        const uploadUrl = `${apiBaseUrl}/api/upload/image`;
                        
                        const response = await fetch(uploadUrl, {
                            method: 'POST',
                            headers: headers,
                            body: formData
                        });
                        
                        if (response.ok) {
                            const result = await response.json();
                            if (result.url) {
                                // Base64 URL'yi S3 URL'i ile değiştir
                                const oldIndex = newImages.findIndex(img => img === imgData);
                                if (oldIndex !== -1) {
                                    newImages[oldIndex].url = result.url;
                                    newImages[oldIndex].isFile = false; // Artık URL
                                }
                                return result.url;
                            }
                        } else {
                            const error = await response.text();
                            console.error('S3 upload failed:', error);
                            throw new Error(`Upload failed: ${error}`);
                        }
                    } catch (error) {
                        console.error('Error uploading to S3:', error);
                        throw error;
                    }
                });
                
                await Promise.all(uploadPromises);
                console.log('Tüm görseller S3\'e yüklendi');
                
                // Hidden input'ları tekrar güncelle (S3 URL'leri ile)
                updateHiddenInputs();
                
                // imageFiles input'unu temizle
                if (imageFilesInput) {
                    imageFilesInput.value = '';
                }
                
                // Form submit et
                console.log('Form submit ediliyor (görseller yüklendikten sonra)...');
                setTimeout(() => {
                    // isSubmitting flag'ini false yap (yeni submit'e izin ver)
                    isSubmitting = false;
                    // Event listener'ı kaldır
                    form.removeEventListener('submit', submitHandler);
                    // Form'u doğrudan submit et (programatik submit event'leri tetiklemez)
                    console.log('Form submit ediliyor (programatik)...');
                    form.submit();
                }, 500);
            } catch (error) {
                alert('Görseller yüklenirken hata oluştu: ' + error.message);
                console.error('Upload error:', error);
                isSubmitting = false; // Hata durumunda flag'i sıfırla
            }
        } else {
            // Yüklenecek dosya yok, direkt submit et
            console.log('Yeni görsel yok, form direkt submit ediliyor...');
            isSubmitting = true; // Flag set et (tekrar tetiklenmeyi önle)
            
            // Hidden input'ları son kez güncelle
            updateHiddenInputs();
            
            // Form'un normal submit olmasına izin ver (e.preventDefault() çağırma)
            // Event listener zaten çalıştı ve hidden input'ları güncelledi
            // Form normal submit olacak ve backend'e gidecek
            console.log('Form normal submit edilecek (yeni görsel yok)...');
        }
    };
    
    form.addEventListener('submit', submitHandler);
}

// Sayfa yüklendiğinde başlat
document.addEventListener('DOMContentLoaded', () => {
    // Edit sayfası mı kontrol et
    const isEditMode = document.querySelector('input[name="Id"]') !== null;
    
    // Quill editörünü başlat
    initQuillEditor(isEditMode);
    
    // Adımları yükle
    loadExistingSteps();
    
    // EditRecipe ve Create sayfaları için görsel yönetimini başlat
    // Create sayfasında da çoklu görsel yükleme yapılabilmeli
    initEditRecipeImageManagement();
});

